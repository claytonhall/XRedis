using System;
using System.Collections.Generic;
using System.Linq;

namespace XRedis.Core.Fields.Indexes
{
    public class CompoundIndex : Index
    {
        public List<Index> Indexes = new List<Index>();
        private readonly Type _recordType;

        public override Type RecordType => _recordType;
        public override string Tag => string.Join("+", Indexes.Select(i => i.Tag));

        public CompoundIndex(Type recordType, IEnumerable<Index> indexes)
        {
            _recordType = recordType;
            Indexes.AddRange(indexes);
        }

        public override string Format(IRecord record)
        {
            return string.Join("+", Indexes.Select(i => i.Format(record)));
        }
    }
}