using System;
using System.Collections.Generic;
using StackExchange.Redis;
using System.Collections;
using System.Linq;
using XRedis.Core.Extensions;
using AutoMapper;
using XRedis.Core.Fields;
using XRedis.Core.Fields.Indexes;
using XRedis.Core.Interception;
using XRedis.Core.Keys;
using Index= XRedis.Core.Fields.Indexes.Index;

namespace XRedis.Core
{
    public class RecordSet<TRecord, TParent> : RecordSet<TRecord>, IRecordSet<TRecord,TParent>
        where TRecord : class, IRecord
        where TParent : class, IRecord
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IProxyFactory _proxyFactory;
        private readonly IResolver _resolver;
        private readonly IResourceManagerFactory _resourceManagerFactory;
        private readonly ILogger _logger;
        private readonly ISchemaHelper _schemaHelper;
        public RecordSet(IConnectionMultiplexer redis, IResolver resolver, IProxyFactory proxyFactory, IResourceManagerFactory resourceManagerFactory, ISchemaHelper schemaHelper, ILogger logger) : base(redis, resolver, proxyFactory, resourceManagerFactory, schemaHelper, logger)
        {
            _redis = redis;
            _proxyFactory = proxyFactory;
            _resolver = resolver;
            _resourceManagerFactory = resourceManagerFactory;
            _logger = logger;
            _schemaHelper = schemaHelper;
        }

        public TParent ParentRecord { get; set; } = null;

        public override TRecord Add(TRecord record)
        {
            var recordProxy = base.Add(record);
            Id parentId = ParentRecord.PrimaryKey().GetValue(ParentRecord);
            recordProxy.ForeignKey(ParentRecord).SetValue(recordProxy, parentId);
            return recordProxy;
        }

        public override TRecord New(Action<TRecord> setter = null)
        {
            var recordProxy = base.New(setter);
            var parentId = ParentRecord.PrimaryKey().GetValue(ParentRecord);
            recordProxy.ForeignKey(ParentRecord).SetValue(recordProxy, parentId);
            return recordProxy;
        }

        public override IRecordSetEnumerator<TRecord> Enumerator
        {
            get
            {
                var enumerator = base.Enumerator;
                if (Index == null)
                {
                    var fkIndex = _schemaHelper.FkIndex(typeof(TRecord), ParentRecord.GetType());
                    var index = _schemaHelper.PkIndex(typeof(TRecord));
                    Index = _schemaHelper.CompoundIndex(fkIndex, Index);
                }

                var pkValue = ParentRecord.GetIDValue(ParentRecord.PrimaryKey()).Value;
                var filter = new RecordSetFilter();
                filter.MinValue = ((CompoundIndex) Index).Indexes[0].Formatter.Format(pkValue) + "+";
                filter.MaxValue = filter.MinValue.NextGreaterValue();

                enumerator.SetFilter(filter, Index);
                return enumerator;
            }
        }

        public override IRecordSet<TRecord> OrderBy(string indexTag)
        {
            var fkIndex = _schemaHelper.FkIndex(typeof(TRecord), ParentRecord.GetType());
            var index = _schemaHelper.Index(typeof(TRecord), indexTag);
            Index = _schemaHelper.CompoundIndex(fkIndex, index);
            return this;
        }
    }

    public class RecordSet<TRecord> : IRecordSet<TRecord>
            where TRecord : class, IRecord
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IProxyFactory _proxyFactory;
        private readonly IResolver _resolver;
        private readonly IResourceManagerFactory _resourceManagerFactory;
        private readonly ILogger _logger;
        private readonly ISchemaHelper _schemaHelper;

        private IRecordSetEnumerator<TRecord> _enumerator;
        public virtual IRecordSetEnumerator<TRecord> Enumerator 
        {
            get
            {
                if (_enumerator == null || !Equals(_enumerator.Index, Index))
                {
                    _enumerator = _resolver.GetInstance<IRecordSetEnumerator<TRecord>>();
                    Index ??= _schemaHelper.PkIndex(typeof(TRecord));
                    _enumerator.SetFilter(null, Index);
                }
                return _enumerator;
            }
        }

        public Index Index { get; set; }

        public RecordSet(IConnectionMultiplexer redis, 
            IResolver resolver, 
            IProxyFactory proxyFactory, 
            IResourceManagerFactory resourceManagerFactory, 
            ISchemaHelper schemaHelper,
            ILogger logger)
        {
            _redis = redis;
            _proxyFactory = proxyFactory;
            _resolver = resolver;
            _resourceManagerFactory = resourceManagerFactory;
            _logger = logger;
            _schemaHelper = schemaHelper;
        }

        public virtual IRecordSet<TRecord> OrderBy(string indexTag)
        {
            Index = _schemaHelper.Index(typeof(TRecord), indexTag);
            return this;
        }


        public virtual TRecord New(Action<TRecord> setter = null)
        {
            var proxy = _proxyFactory.CreateClassProxy<TRecord>();
            setter?.Invoke(proxy);
            return proxy;
        }

        public void Delete(TRecord record)
        {
            record.Delete();
        }

        public virtual TRecord Add(TRecord record)
        {
            var proxyObj = _proxyFactory.CreateClassProxy<TRecord>();

            var config = new MapperConfiguration(cfg => cfg.CreateMap<TRecord, TRecord>());
            var mapper = config.CreateMapper();
            mapper.Map(record, proxyObj);
            return proxyObj;
        }

        public TRecord Seek(object value)
        {
            Enumerator.Seek(value);
            return Enumerator.Current;
        }

        public IEnumerator<TRecord> GetEnumerator() => Enumerator;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TRecord CurrentRecord => ((IRecordSetEnumerator<TRecord>)GetEnumerator()).Current;

        public void SkipX(int i = 1)
        {
            Enumerator.MoveNext();
        }
    }
}
