using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Antlr.Runtime.Tree;
using Castle.Core.Internal;
using StackExchange.Redis;

namespace RedisFirst.Core.Interception
{
    public class TransactionDatabase
    {
        private IDatabase _baseDb;

        private IsolationLevel _isolationLevel;

        private Guid _transactionId;

        long RowVersion(RedisKey key)
        {
            return Convert.ToInt64(key.ToString().Substring(key.ToString().LastIndexOf("VERSION:") + "VERSION:".Length));
        }

        RedisKey WithoutVersion(RedisKey key)
        {
            var str = key.ToString();
            if (str.LastIndexOf("VERSION:") > -1)
            {
                return str.Substring(0, str.LastIndexOf("VERSION:") + "VERSION:".Length);
            }
            else
            {
                return key;
            }
        }

        RedisKey NotExists(RedisKey key)
        {
            return $"{key}:VERSION:0";
        }

        RedisKey NextVersion(RedisKey key)
        {
            var nextVersion = _baseDb.StringIncrement($"_VERSION");
            return $"{WithoutVersion(key)}:VERSION:{nextVersion}";
        }


        RedisKey GetCommittedKey(RedisKey key)
        {
            var val = _baseDb.StringGet($"COMMITTED:{key}");
            if (val.IsNullOrEmpty)
            {
                return NotExists(key);
            }
            else
            {
                return (RedisKey) val.ToString();
            }
        }

        RedisKey GetTransactionKey(RedisKey key)
        {
            var val = _baseDb.StringGet($"TRANSACTION:{_transactionId}:{key}");
            if (val.IsNullOrEmpty)
            {
                return new RedisKey();
            }
            else
            {
                return (RedisKey)val.ToString();
            }
        }

        RedisKey GetCurrentKey(RedisKey key)
        {
            var val = _baseDb.StringGet($"CURRENT:{key}");
            if (val.IsNullOrEmpty)
            {
                return new RedisKey();
            }
            else
            {
                return (RedisKey)val.ToString();
            }
        }

        RedisKey GetKey(RedisKey key)
        {
            var retVal = key;
            if (_isolationLevel == IsolationLevel.ReadUncommitted)
            {
                var tranKey = GetTransactionKey(key);
                var currentKey = GetCurrentKey(key);
                if (RowVersion(currentKey) > RowVersion(tranKey))
                {
                    retVal = currentKey;
                }
                else
                {
                    retVal = tranKey;
                }
            }
            else if (_isolationLevel == IsolationLevel.ReadCommitted)
            {
                var tranKey = GetTransactionKey(key);
                var committedKey = GetCommittedKey(key);
                if (RowVersion(committedKey) > RowVersion(tranKey))
                {
                    retVal = committedKey;
                }
                else
                {
                    retVal = tranKey;
                }
            }
            else if (_isolationLevel == IsolationLevel.RepeatableRead)
            {
                var tranKey = GetTransactionKey(key);
                if (string.IsNullOrWhiteSpace(tranKey))
                {
                    //repeatable read means that this needs to be added to the transaction!
                    retVal = NotExists(key);
                    _baseDb.StringSet($"TRANSACTION:{_transactionId}:{key}", retVal.ToString());

                    //also... need to writelock the key?
                }
                else
                {
                    retVal = tranKey;
                }
            }
            return retVal;
        }

       

        void SetKey(RedisKey key, RedisKey internalKey)
        {
            if (_isolationLevel == IsolationLevel.ReadUncommitted)
            {
                throw new ApplicationException("ReadUncommitted should be readonly.");
            }
            else if(_isolationLevel == IsolationLevel.ReadCommitted)
            {
                _baseDb.StringSet($"TRANSACTION:{_transactionId}:{key}", internalKey.ToString());
                _baseDb.StringSet($"CURRENT:{key}", internalKey.ToString());
            }
            else if(_isolationLevel == IsolationLevel.RepeatableRead)
            {
                _baseDb.StringSet($"TRANSACTION:{_transactionId}:{key}", internalKey.ToString());
                _baseDb.StringSet($"CURRENT:{key}", internalKey.ToString());
            }
        }





        string GetIndexValue(string indexKey, string minValue, string maxValue = null, long skip = 0)
        {
            IDatabase db = null;
            
            //indexKey is the List of index keys!
            string retVal = null;
            RedisValue[] keys;
            do
            {
                keys = db.SortedSetRangeByValue(indexKey, min: minValue, max: maxValue ?? default(RedisKey), skip: skip, take: 1);

                if (keys.Length != 0)
                {
                    var key = (RedisKey)keys[0].ToString();

                    //if this is the rowversion currently in scope for me...
                    if (RowVersion(GetKey(key)) == RowVersion(key))
                    {
                        //if yes, return it!
                        retVal = key;
                    }
                    else
                    {
                        //skip 1, starting with this key!
                        skip = 1;
                        minValue = key;
                    }
                }
            } while (keys.Length>0 && retVal == null);
            return retVal;
        }


//        public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = default(TimeSpan?),
//            When when = When.Always, CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = NextVersion(GetKey(key));
//            SetKey(key, internalKey);
//            return _baseDb.StringSet(internalKey, value, expiry, when, flags);
//        }

//        public RedisValue StringGet(RedisKey key, CommandFlags flags = CommandFlags.None)
//        {
//            return _baseDb.StringGet(GetKey(key), flags);
//        }

//        public RedisValue[] StringGet(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
//        {
//            RedisKey[] internalKeys = keys.ConvertAll(k => GetKey(k));
//            return _baseDb.StringGet(internalKeys, flags);
//        }

//        public bool StringSet(KeyValuePair<RedisKey, RedisValue>[] values, When when = When.Always, CommandFlags flags = CommandFlags.None)
//        {
//            var internalValues = values.ConvertAll(k => new KeyValuePair<RedisKey, RedisValue>(GetKey(k.Key), k.Value));
//            return _baseDb.StringSet(internalValues, when, flags);
//        }

//        public double HashDecrement(RedisKey key, RedisValue hashField, double value,
//            CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = NextVersion(GetKey(key));
//            SetKey(key, internalKey);
//            return _baseDb.HashDecrement(internalKey, hashField, value, flags);
//        }

//        public long HashDecrement(RedisKey key, RedisValue hashField, long value = 1,
//            CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = NextVersion(GetKey(key));
//            SetKey(key, internalKey);
//            return _baseDb.HashDecrement(internalKey, hashField, value, flags);
//        }

//        public long HashDelete(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
//        {
//            var oldInternalKey = GetKey(key);
//            var internalKey = NextVersion(key);

//            //copy the record
//            _baseDb.HashSet(internalKey, HashGetAll(oldInternalKey));
//            SetKey(key, internalKey);
//            return _baseDb.HashDelete(internalKey, hashFields, flags);
//        }

//        public bool HashDelete(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
//        {
//            var oldInternalKey = GetKey(key);
//            var internalKey = NextVersion(key);

//            //copy the record
//            _baseDb.HashSet(internalKey, HashGetAll(oldInternalKey));
//            SetKey(key, internalKey);
//            return _baseDb.HashDelete(internalKey, hashField, flags);
//        }

//        public bool HashExists(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = GetKey(key);
//            return _baseDb.HashExists(internalKey, hashField, flags);
//        }

//        public RedisValue[] HashGet(RedisKey key, RedisValue[] hashFields, CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = GetKey(key);
//            return _baseDb.HashGet(internalKey, hashFields, flags);
//        }

//        public RedisValue HashGet(RedisKey key, RedisValue hashField, CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = GetKey(key);
//            return _baseDb.HashGet(internalKey, hashField, flags);
//        }

//        public HashEntry[] HashGetAll(RedisKey key, CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = GetKey(key);
//            return _baseDb.HashGetAll(internalKey, flags);
//        }


//        public double HashIncrement(RedisKey key, RedisValue hashField, double value,
//            CommandFlags flags = CommandFlags.None)
//        {
//            var oldInternalKey = GetKey(key);
//            var internalKey = NextVersion(key);

//            //copy the record
//            _baseDb.HashSet(internalKey, HashGetAll(oldInternalKey));
//            SetKey(key, internalKey);
//            return _baseDb.HashIncrement(internalKey, hashField, value, flags);
//        }

//        public long HashIncrement(RedisKey key, RedisValue hashField, long value = 1,
//            CommandFlags flags = CommandFlags.None)
//        {
//            var oldInternalKey = GetKey(key);
//            var internalKey = NextVersion(key);

//            //copy the record
//            _baseDb.HashSet(internalKey, HashGetAll(oldInternalKey));
//            SetKey(key, internalKey);
//            return _baseDb.HashIncrement(internalKey, hashField, value = 1, flags);
//}


//        public RedisValue[] HashKeys(RedisKey key, CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = GetKey(key);
//            return _baseDb.HashKeys(internalKey, flags);
//        }


//        public long HashLength(RedisKey key, CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = GetKey(key);
//            return _baseDb.HashLength(internalKey, flags);
//        }   

//        public void HashSet(RedisKey key, HashEntry[] hashFields, CommandFlags flags = CommandFlags.None)
//        {
//            var oldInternalKey = GetKey(key);
//            var internalKey = NextVersion(key);

//            //copy the record
//            _baseDb.HashSet(internalKey, HashGetAll(oldInternalKey));
//            SetKey(key, internalKey);
//            _baseDb.HashSet(internalKey, hashFields, flags);
//        }

//        public bool HashSet(RedisKey key, RedisValue hashField, RedisValue value, When when = When.Always,
//            CommandFlags flags = CommandFlags.None)
//        {
//            var oldInternalKey = GetKey(key);
//            var internalKey = NextVersion(key);

//            //copy the record
//            _baseDb.HashSet(internalKey, HashGetAll(oldInternalKey));
//            SetKey(key, internalKey);
//            return _baseDb.HashSet(key, hashField, value, when, flags);
//        }

//        public RedisValue[] HashValues(RedisKey key, CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = GetKey(key);
//            return _baseDb.HashValues(internalKey, flags);
//        }

//        public long KeyDelete(RedisKey[] keys, CommandFlags flags = CommandFlags.None)
//        {
//            int count = 0;
//            keys.ForEach(key =>
//                {
//                    if (KeyDelete(key, flags))
//                    {
//                        count++;
//                    }
//                });
            
//            return count;
//        }

//        public bool KeyDelete(RedisKey key, CommandFlags flags = CommandFlags.None)
//        {
//            bool retVal = false;
//            var oldInternalKey = GetKey(key);
//            if (_baseDb.KeyExists(oldInternalKey))
//            {
//                var internalKey = NextVersion(key);
//                SetKey(key, internalKey);

//                retVal = true;
//            }
//            return retVal;
//        }


//        public bool KeyExists(RedisKey key, CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = GetKey(key);
//            return _baseDb.KeyExists(internalKey, flags);
//        }


//        public RedisValue ListLeftPop(RedisKey key, CommandFlags flags = CommandFlags.None)
//        {
//            return _baseDb.ListLeftPop(key, flags);
//        }


//        public long ListLeftPush(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
//        {
//            return _baseDb.ListLeftPush(key, values, flags);
//        }

//        public long ListLeftPush(RedisKey key, RedisValue value, When when = When.Always,
//            CommandFlags flags = CommandFlags.None)
//        {
//            return _baseDb.ListLeftPush(key, value, when, flags);
//        }

//        public long ListLength(RedisKey key, CommandFlags flags = CommandFlags.None)
//        {
//            return _baseDb.ListLength(key, flags);
//        }

//        public RedisValue ListRightPop(RedisKey key, CommandFlags flags = CommandFlags.None)
//        {
//            return _baseDb.ListRightPop(key, flags);
//        }

//        public long ListRightPush(RedisKey key, RedisValue[] values, CommandFlags flags = CommandFlags.None)
//        {
//            return _baseDb.ListRightPush(key, values, flags);
//        }

//        public long ListRightPush(RedisKey key, RedisValue value, When when = When.Always, CommandFlags flags = CommandFlags.None)
//        {
//            return _baseDb.ListRightPush(key, value, when, flags);
//        }

//        public long SortedSetAdd(RedisKey key, SortedSetEntry[] values, CommandFlags flags = CommandFlags.None)
//        {
//            return _baseDb.SortedSetAdd(key, values, flags);
//        }

//        public bool SortedSetAdd(RedisKey key, RedisValue member, double score, CommandFlags flags = CommandFlags.None)
//        {
//            var oldInternalKey = GetKey(key);
//            var internalKey = NextVersion(key);

//            //this doesn't seem good... copying the whole set...
//            _baseDb.SortedSetCombineAndStore(SetOperation.Union, internalKey, oldInternalKey, new RedisKey());
//            SetKey(key, internalKey);
//            return _baseDb.SortedSetAdd(internalKey, member, score, flags);
//        }

//        public RedisValue[] SortedSetRangeByValue(RedisKey key, RedisValue min = default(RedisValue),
//            RedisValue max = default(RedisValue), Exclude exclude = Exclude.None, long skip = 0, long take = -1,
//            CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = NextVersion(GetKey(key));
//            SetKey(key, internalKey);
//            return _baseDb.SortedSetRangeByValue(key, min, max, exclude, skip, take, flags);
//        }

//        public long SortedSetRemove(RedisKey key, RedisValue[] members, CommandFlags flags = CommandFlags.None)
//        {
//            var internalKey = NextVersion(GetKey(key));
//            SetKey(key, internalKey);
//            return _baseDb.SortedSetRemove(key, members, flags);
//        }

//        public bool SortedSetRemove(RedisKey key, RedisValue member, CommandFlags flags = CommandFlags.None)
        //{
        //    var internalKey = NextVersion(GetKey(key));
        //    SetKey(key, internalKey);
        //    return _baseDb.SortedSetRemove(key, member, flags);
        //}
      
    //}
}