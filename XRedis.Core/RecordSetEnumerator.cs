using System;
using System.Collections;
using System.Collections.Generic;
using XRedis.Core.Extensions;
using StackExchange.Redis;
using XRedis.Core.Fields.Indexes;
using XRedis.Core.Interception;
using XRedis.Core.Keys;
using Index = XRedis.Core.Fields.Indexes.Index;

namespace XRedis.Core
{
    public interface IRecordSetEnumerator<TRecord, TKey> : IEnumerator<TRecord>
    {
        Index Index { get; set; }
        void SetFilter(RecordSetFilter filter, Index index);
        void Seek(object value);
    }

    public class RecordSetEnumerator<TRecord, TKey> : IRecordSetEnumerator<TRecord, TKey>
        where TRecord : class, IRecord<TKey>
    {
        private readonly IProxyFactory _proxyFactory;
        private readonly IResourceManagerFactory _resourceManagerFactory;
        private readonly ISchemaHelper _schemaHelper;

        private RecordSetFilter Filter { get; set; }
        public Index Index { get; set; }
        private IndexValue _currentIndexValue = null;

        public RecordSetEnumerator(
            IProxyFactory proxyFactory, 
            IResourceManagerFactory resourceManagerFactory,
            ISchemaHelper schemaHelper)
        {
            _proxyFactory = proxyFactory;
            _resourceManagerFactory = resourceManagerFactory;
            _schemaHelper = schemaHelper;
            
            //default index to primary key
            Index = _schemaHelper.PkIndex(_schemaHelper.PrimaryKey(typeof(TRecord)));
        }

        public void SetFilter(RecordSetFilter filter, Index index)
        {
            this.Filter = filter;
            this.Index = index;
        }

        public TRecord Current
        {
            get
            {
                if (_currentIndexValue == null) return null;

                IId id = _currentIndexValue.VersionedRecordKey.Id;
                var record = _proxyFactory.CreateClassProxy<TRecord, TKey>();
                record.SetID(id);
                return record;
            }
        }
        
        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        private bool _isSought = false;
        public void Seek(object value)
        {
            var minValue = (Filter?.MinValue ?? "") + value.ToString().FormatIndexValue();
            var maxValue = String.IsNullOrEmpty(minValue) ? char.MaxValue.ToString() : minValue.NextGreaterValue();

            var rm = _resourceManagerFactory.GetInstance();
            _currentIndexValue = rm.GetIndexValue(Index.Key, minValue, maxValue);
            _isSought = _currentIndexValue != null;
        }

        public bool MoveNext()
        {
            var skip = (_isSought || _currentIndexValue == null) ? 0 : 1;
            _isSought = false;
            var minValue = _currentIndexValue?.ToString() ?? Filter?.MinValue ?? "";
            var maxValue = Filter?.MaxValue ?? char.MaxValue.ToString();

            //get the next item in the index!
            var rm = _resourceManagerFactory.GetInstance();
            _currentIndexValue = rm.GetIndexValue(Index.Key, minValue, maxValue, skip);
            return _currentIndexValue != null;
        }

        public void Reset()
        {
            _currentIndexValue = null;
        }
    }
}