using System;
using System.Reflection;

namespace XRedis.Core.Fields
{
    public class IndexableField : IIndexableField
    {
        public IndexableField(Type recordType, PropertyInfo pkPropertyInfo)
        {
            RecordType = recordType;
            PropertyInfo = pkPropertyInfo;
        }

        public Type RecordType { get; }
        public PropertyInfo PropertyInfo { get; }
    }
}