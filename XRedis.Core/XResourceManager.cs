using System;
using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Transactions;
using Castle.Core.Internal;
using Castle.DynamicProxy.Generators;
using XRedis.Core.Extensions;
using StackExchange.Redis;
using XRedis.Core.Fields;
using XRedis.Core.Interception;
using XRedis.Core.Keys;
using Index = XRedis.Core.Fields.Indexes.Index;

namespace XRedis.Core
{
    public class XResourceManager : IEnlistmentNotification
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IProxyFactory _proxyFactory;
        private readonly ISchemaHelper _schemaHelper;
        private readonly ILogger _logger;

        private ITransaction _rollbackTransaction;
        private ITransaction _commitTransaction;

        private TransactionCache _transactionCache = new TransactionCache();

        private long _transactionVersion;

        public XResourceManager(IConnectionMultiplexer connectionMultiplexer, IProxyFactory proxyFactory, ILogger logger, ISchemaHelper schemaHelper)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _proxyFactory = proxyFactory;
            _logger = logger;
            _schemaHelper = schemaHelper;
        }

        public bool IsAutoCommit { get; set; }

        public void StartTransaction()
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();
            var transactionVersion = db.StringIncrement(new TableIncrementKey(Keys.Keys.Version));

            ITransaction tran = db.CreateTransaction();
            tran.StringSetAsync($"TRANSACTION:{TransactionId}", transactionVersion);
            SetTransactionState(transactionVersion, "EXECUTE", tran);
            tran.Execute();
            
            _logger.Log($"Transaction:{TransactionVersion} Begin");

            _rollbackTransaction = db.CreateTransaction();
            SetTransactionState(transactionVersion, "ROLLBACK", _rollbackTransaction);

            _commitTransaction = db.CreateTransaction();
            SetTransactionState(transactionVersion, "COMMIT", _commitTransaction);

            _rollbackTransaction.KeyDeleteAsync($"TRANSACTIONSTATE:{transactionVersion}");
            _rollbackTransaction.KeyDeleteAsync($"TRANSACTION:{TransactionId}");
            _commitTransaction.KeyDeleteAsync($"TRANSACTIONSTATE:{transactionVersion}");
            _commitTransaction.KeyDeleteAsync($"TRANSACTION:{TransactionId}");

            Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
        }

        public long TransactionVersion
        {
            get
            {
                if (_transactionVersion == 0)
                {
                    IDatabase db = _connectionMultiplexer.GetDatabase();
                    _transactionVersion = long.Parse(db.StringGet($"TRANSACTION:{TransactionId}"));
                }
                return _transactionVersion;
            }
        }

        public string GetTransactionState(long transactionVersion)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();
            return db.StringGet($"TRANSACTIONSTATE:{transactionVersion}");
        }

        private void SetTransactionState(long transactionVersion, string state, ITransaction tran)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();
            tran.StringSetAsync($"TRANSACTIONSTATE:{transactionVersion}", state);
        }

        public string TransactionId { get; set; }

        public object GetValue(IRecord record, PropertyInfo propertyInfo)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();
            var recordKey = GetReadKey(record.GetRecordKey());
            
            var value = _transactionCache[recordKey][propertyInfo.Name] ?? db.HashGet(recordKey, propertyInfo.Name);

            var convertedValue = ConvertRedisValue(value, propertyInfo.PropertyType);
            return convertedValue;
        }

        public void SetValue(IRecord record, PropertyInfo propertyInfo, Id id, object value)
        {
            _logger.Log($"Transaction:{TransactionVersion} Begin SetValue");
            IDatabase db = _connectionMultiplexer.GetDatabase();

            //bool firstField = false;
            var tran = db.CreateTransaction();

            RecordKey recordKey = default(RecordKey);
            VersionedRecordKey versionedRecordKey = default(VersionedRecordKey);

            if(id.Value.GetType().GetDefaultValue().Equals(id.Value))
            {
                //firstField = true;
                id = new Id(db.StringIncrement(record.GetIncrementKey()));
                recordKey = record.GetRecordKey(id);
                versionedRecordKey = SetRecordVersion(recordKey);
                record.SetID(id);

                //track its version!
                tran.SortedSetAddAsync(versionedRecordKey.RecordKey.GetVersionTrackerKey(), versionedRecordKey.ToSortableString(), 0);

                //for rollback - delete the new internal key!
                _rollbackTransaction.KeyDeleteAsync(versionedRecordKey);
                //and the version tracking!
                _rollbackTransaction.SortedSetRemoveAsync(versionedRecordKey.RecordKey.GetVersionTrackerKey(), versionedRecordKey.ToSortableString());
            }
            else
            {
                recordKey = record.GetRecordKey(id);

                var timeout = DateTime.Now.AddMilliseconds(100);
                bool retry = true;
                while (retry)
                {
                    retry = false;

                    WriteKeyResult writeKeyResult = GetWriteKey(recordKey, tran);
                    _logger.Log($"Transaction:{TransactionVersion} - {writeKeyResult.Message}");

                    if (writeKeyResult.State == WriteKeyState.CREATE)
                    {
                        CreateNewVersion(record, writeKeyResult.CurrentKey, writeKeyResult.NewKey);

                        //delete this key on rollback!
                        _rollbackTransaction.KeyDeleteAsync(writeKeyResult.NewKey);

                        //use the new key!
                        versionedRecordKey = writeKeyResult.NewKey;

                    }
                    else if (writeKeyResult.State == WriteKeyState.CURRENT)
                    {
                        //use the old key!
                        versionedRecordKey = writeKeyResult.CurrentKey;
                    }
                    else if (writeKeyResult.State == WriteKeyState.FAILURE)
                    {
                        if (DateTime.Now < timeout)
                        {
                            retry = true;
                        }
                        else
                        {
                            //rollback!
                            Transaction.Current.Rollback();
                            throw new ApplicationException($"Transaction Cancelled - Can not obtain write key");
                        }
                    }
                }
            }

            _logger.Log($"Transaction:{TransactionVersion} - WriteKey - {versionedRecordKey}");

            _transactionCache[versionedRecordKey][propertyInfo.Name] = value.ToString();
            tran.HashSetAsync(versionedRecordKey, propertyInfo.Name, value.ToString());


            //this only needs to happen on commit!
            _commitTransaction.ListRemoveAsync("QUEUE", record.GetRecordKey());
            _commitTransaction.ListLeftPushAsync("QUEUE", record.GetRecordKey());

            UpdateIndexes(record, versionedRecordKey, tran);

            var committed = tran.Execute();
            if (!committed)
            {
                throw new ApplicationException("Transaction did not commit!");
            }
            _logger.Log($"Transaction:{TransactionVersion} - End SetValue");
        }

        public TNavigationRecord GetNavigationRecord<TNavigationRecord>(IRecord record)
        where TNavigationRecord : class, IRecord
        {
            var navigationRecordType = typeof(TNavigationRecord);

            TNavigationRecord retVal = default(TNavigationRecord);
            IDatabase db = _connectionMultiplexer.GetDatabase();
            var fkValue = record.ForeignKey(navigationRecordType).GetValue(record);

            //if (fkValue != default(Id))
            if (!fkValue.Value.GetType().GetDefaultValue().Equals(fkValue.Value))
            {
                //now, lets find that on the propertype record!
                var recordKey = navigationRecordType.GetRecordKey(fkValue);
                
                //get the current read version!
                var readKey = GetReadKey(recordKey);
                if (db.KeyExists(readKey))
                {
                    var navObj = _proxyFactory.CreateClassProxy<TNavigationRecord>();
                    navObj.SetID(fkValue);
                    retVal = navObj;
                }
            }
            return retVal;
        }

        //this is called externally....
        public IndexValue GetIndexValue(IndexKey indexKey, string minValue, string maxValue = null, long skip = 0)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();

            //indexKey is the List of index keys!
            IndexValue retVal = null;
            IndexValue[] indexValues;
            do
            {
                maxValue ??= default(RedisKey);

                indexValues = db.SortedSetRangeByValue(indexKey, min: minValue, max: maxValue, skip: skip, take: 1)
                    .Where(iv=>iv.HasValue)
                    .ToList()
                    .ConvertAll(k => (IndexValue)k)
                    .ToArray();

                if (indexValues.Length != 0)
                {
                    var indexValue = indexValues[0];

                    //if this is the rowversion currently in scope for me...
                    //if (GetReadKey(indexValue.RecordKey).Version <= indexValue.VersionedRecordKey.Version)
                    if (GetReadKey(indexValue.RecordKey).Version >= indexValue.VersionedRecordKey.Version)
                    {
                        //if yes, return it!
                        retVal = indexValue;
                    }
                    else
                    {
                        //skip 1, starting with this key!
                        skip = 1;
                        minValue = indexValue.ToString();
                    }
                }
            } while (indexValues.Length > 0 && retVal == null);
            return retVal;
        }

        public void Delete(IRecord record)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();
            var hashKeys = db.HashKeys(record.GetRecordKey());
            var tran = db.CreateTransaction();
            foreach (var hashKey in hashKeys)
            {
                tran.AddCondition(Condition.HashExists(record.GetRecordKey(), hashKey));
            }
            tran.HashDeleteAsync(record.GetRecordKey(), hashKeys);

            //add code to delete related indexes!


            if (!tran.Execute())
            {
                throw new ApplicationException("Delete failed.");
            }
        }


        private VersionedRecordKey SetRecordVersion(RecordKey key)
        {
            return key.GetVersionedRecordKey(TransactionVersion);
        }

        private VersionedRecordKey GetReadKey(RecordKey key)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();

            var versionReferenceLookupTable = key.GetVersionTrackerKey().ToString();
            var unversionedKeyName = key.ToSortableString();
            var currentVersionKeyName = key.GetVersionedRecordKey(TransactionVersion).ToSortableString();

            var internalKeyValue = db.SortedSetRangeByValue(versionReferenceLookupTable, unversionedKeyName, currentVersionKeyName).LastOrDefault();
            if (!internalKeyValue.HasValue)
            {
                var internalKey = SetRecordVersion(key);
                db.SortedSetAdd(versionReferenceLookupTable, internalKey.ToSortableString(), 0);
                internalKeyValue = internalKey;
                _rollbackTransaction.SortedSetRemoveAsync(key.GetVersionTrackerKey(), internalKey.ToSortableString());
            }

            _logger.Log($"{TransactionVersion} - GetReadKey - {internalKeyValue}");

            return VersionedRecordKey.Parse(internalKeyValue);
        }

        private WriteKeyResult GetWriteKey(RecordKey key, ITransaction tran)
        {
            WriteKeyResult result = new WriteKeyResult();
            IDatabase db = _connectionMultiplexer.GetDatabase();

            var versionTrackerKey = key.GetVersionTrackerKey();

            var keyFromDbString = db.SortedSetRangeByValue(versionTrackerKey, key.ToSortableString(), key.ToSortableString().NextGreaterValue()).LastOrDefault();
            if (keyFromDbString.HasValue)
            {
                var keyFromDb = VersionedRecordKey.Parse(keyFromDbString);
                result.CurrentKey = keyFromDb;
            }
            else
            {
                result.CurrentKey = SetRecordVersion(key);
                tran.SortedSetAddAsync(versionTrackerKey, result.CurrentKey.ToSortableString(), 0);

                _rollbackTransaction.SortedSetRemoveAsync(versionTrackerKey, result.CurrentKey.ToString());
            }
            _logger.Log($"Transaction:{TransactionVersion} -  {nameof(GetWriteKey)} - CurrentKey {result.CurrentKey}");

            if (result.CurrentKey.Version == TransactionVersion)
            {
                //use CurrentKey!
                result.Message = $"Current WriteKey. {key} is version {result.CurrentKey.Version} same as transaction {TransactionVersion}";
                result.State = WriteKeyState.CURRENT;
            }
            else if (result.CurrentKey.Version > TransactionVersion)
            {
                //can't write!
                result.Message = $"Failed to obtain WriteKey. {key} is version {result.CurrentKey.Version} greater than this transaction is {TransactionVersion}";
                result.State = WriteKeyState.FAILURE;
            }
            else if (result.CurrentKey.Version < TransactionVersion)
            {
                var tranState = GetTransactionState(result.CurrentKey.Version);
                if (tranState != "EXECUTE")
                {
                    result.State = WriteKeyState.CREATE;

                    //make a copy
                    result.NewKey = SetRecordVersion(key);
                    result.Message = $"Create WriteKey. {key} is version {result.CurrentKey.Version}. Create key with version {result.NewKey.Version}";
                }
                else
                {
                    //can't write!
                    result.Message = $"Failed to obtain WriteKey. {key} is version {result.CurrentKey.Version} less than this transaction is {TransactionVersion} but TransactionState is {tranState}";
                    result.State = WriteKeyState.FAILURE;
                }
            }

            _logger.Log($"{nameof(GetWriteKey)} - CurrentKey {result.CurrentKey} NewKey {result.NewKey}");

            return result;
        }

        
        private void CreateNewVersion(IRecord record, VersionedRecordKey oldKey, VersionedRecordKey newKey)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();

            var tran = db.CreateTransaction();
            var newVals = db.HashGetAll(oldKey);
            foreach (var newVal in newVals)
            {
                _transactionCache[newKey].Add(newVal.Name, newVal.Value);
            }
            tran.HashSetAsync(newKey, newVals);

            //now add indexes!
            UpdateIndexes(record, newKey, tran);

            //track its version!
            tran.SortedSetAddAsync(newKey.RecordKey.GetVersionTrackerKey(), newKey.ToSortableString(), 0);

            if (!tran.Execute())
            {
                throw new ApplicationException("Transaction Failed!");
            }

            //for rollback - delete the new internal key!
            _rollbackTransaction.KeyDeleteAsync(newKey);
            //and the version tracking!
            _rollbackTransaction.SortedSetRemoveAsync(newKey.RecordKey.GetVersionTrackerKey(), newKey.ToString());

            //for commit - delete the old internal key!
            //not sure... could another transaction still be using this?
            //delete indexes!
            DeleteIndexes(record, oldKey, tran);
            _commitTransaction.KeyDeleteAsync(oldKey);
            _commitTransaction.SortedSetRemoveAsync(oldKey.RecordKey.GetVersionTrackerKey(), oldKey.ToString());
        }

        private void DeleteIndexes(IRecord record, VersionedRecordKey versionedRecordKey, ITransaction tran)
        {
            var pkIndex = _schemaHelper.PkIndex(_schemaHelper.PrimaryKey(record.GetType()));
            DeleteIndex(pkIndex, record, tran, versionedRecordKey);

            var fkIndexes = _schemaHelper.Indexes(record.GetType()).Where(i => i.IndexableField is ForeignKey).ToList();
            fkIndexes.ForEach(i => DeleteIndex(i, record, tran, versionedRecordKey));

            var other = _schemaHelper.Indexes(record.GetType()).Where(i => !(i.IndexableField is PrimaryKey) && !(i.IndexableField is ForeignKey)).ToList();
            other.ForEach(i => DeleteIndex(i, record, tran, versionedRecordKey));
        }

        private void DeleteIndex(Index index, IRecord record, ITransaction tran, VersionedRecordKey versionedRecordKey)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();
            
            var indexRefKey = record.GetIndexReferenceKey(versionedRecordKey, index);
            var indexValue = db.StringGet(indexRefKey);
            _commitTransaction.SortedSetRemoveAsync(indexRefKey.IndexKey, indexValue);
            _commitTransaction.KeyDeleteAsync(indexRefKey);
        }




        private void UpdateIndexes(IRecord record, VersionedRecordKey versionedRecordKey, ITransaction tran)
        {
            var pkIndex = _schemaHelper.PkIndex(_schemaHelper.PrimaryKey(record.GetType()));
            AddIndex(pkIndex, record, tran, versionedRecordKey);

            var fkIndexes = _schemaHelper.Indexes(record.GetType()).Where(i => i.IndexableField is ForeignKey).ToList();
            fkIndexes.ForEach(i=>AddIndex(i, record, tran, versionedRecordKey));

            var other = _schemaHelper.Indexes(record.GetType()).Where(i => !(i.IndexableField is PrimaryKey) && !(i.IndexableField is ForeignKey)).ToList();
            other.ForEach(i => AddIndex(i, record, tran, versionedRecordKey));
        }

        private void AddIndex(Index index, IRecord record, ITransaction tran, VersionedRecordKey versionedRecordKey)
        {
            IDatabase db = _connectionMultiplexer.GetDatabase();
            var indexValue = record.GetIndexValue(index, versionedRecordKey);
            var indexRefKey = record.GetIndexReferenceKey(versionedRecordKey, index);

            var oldIndexValue = db.StringGet(indexRefKey);
            if (oldIndexValue.HasValue)
            {
                tran.AddCondition(Condition.StringEqual(indexRefKey, oldIndexValue));
                tran.SortedSetRemoveAsync(index.Key, oldIndexValue);
            }
            else
            {
                tran.AddCondition(Condition.KeyNotExists(indexRefKey));
            }

            tran.SortedSetAddAsync(index.Key, indexValue, 0);
            tran.StringSetAsync(indexRefKey, indexValue);
            _logger.Log($"Transaction:{TransactionVersion} - Added index - {index.Tag} {index.Key} {indexValue}");

            //for rollback!
            _rollbackTransaction.SortedSetRemoveAsync(index.Key, indexValue);
            _rollbackTransaction.KeyDeleteAsync(indexRefKey);
        }

        

        

        private object ConvertRedisValue(RedisValue value, Type type)
        {
            object retVal = null;
            if (!value.IsNull)
            {
                if (type == typeof(DateTime?))
                {
                    retVal = DateTime.Parse(value.ToString());
                }
                else
                {
                    retVal = Convert.ChangeType(value.ToString(), type);
                }
            }
            else
            {
                retVal = GetDefault(type);
            }
            return retVal;
        }

        private object GetDefault(Type t)
        {
            return this.GetType()
                        .GetMethod(nameof(GetDefaultGeneric))
                        ?.MakeGenericMethod(t)
                        .Invoke(this, null);
        }

        public T GetDefaultGeneric<T>()
        {
            return default(T);
        }



        public void Commit(Enlistment enlistment)
        {
            _logger.Log($"Transaction:{TransactionVersion} Commit");
            var success = _commitTransaction.Execute();
            if (!success)
            {
                _logger.Log($"Transaction:{TransactionVersion} Error on commit");
                System.Diagnostics.Debug.WriteLine("Error on commit!");
            }
        }

        public void InDoubt(Enlistment enlistment)
        {
            //throw new NotImplementedException();
        }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            preparingEnlistment.Prepared();
        }

        public void Rollback(Enlistment enlistment)
        {
            _logger.Log($"Transaction:{TransactionVersion} Rollback");
            var success = _rollbackTransaction.Execute();
            if (!success)
            {
                _logger.Log($"Transaction:{TransactionVersion} Error on rollback");
                System.Diagnostics.Debug.WriteLine("Error on rollback!");
            }
        }

        private class TransactionCache : Dictionary<VersionedRecordKey, RecordCache>
        {
            public new RecordCache this[VersionedRecordKey key]
            {
                get
                {
                    if(!ContainsKey(key))
                    {
                        base.Add(key, new RecordCache());
                    }
                    return base[key];
                }
                set => base[key] = value;
            }

        }

        private class RecordCache : Dictionary<string, RedisValue?>
        {
            public new RedisValue? this[string key]
            {
                get => !TryGetValue(key, out var val) ? null : val;
                set => base[key] = value;
            }
        }


    }
}