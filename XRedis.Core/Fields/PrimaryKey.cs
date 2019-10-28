using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using XRedis.Core.Extensions;

namespace XRedis.Core.Fields
{
    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PrimaryKey : System.Attribute, IKeyField
    {
        public PrimaryKey(Type recordType, [CallerMemberName] string pkPropertyName = "")
        {
            RecordType = recordType;
            PropertyInfo = recordType.GetProperty(pkPropertyName);
        }

        public PrimaryKey(Type recordType, PropertyInfo pkPropertyInfo)
        {
            RecordType = recordType;
            PropertyInfo = pkPropertyInfo;
        }

        public Type RecordType { get; }

        public PropertyInfo PropertyInfo { get; }

        public string Name => PropertyInfo.Name;

        public IId GetValue<TRecord, TKey>(TRecord record)
            where TRecord : class, IRecord<TKey>
        {
            return record.GetIDValue<TRecord, TKey>(this);
            //return new Id(record.GetType().GetProperty(PropertyInfo.Name)?.GetValue(record));
            //return record.GetIDValue(this);
        }

        public void SetValue(IRecord record, IId id)
        {
            record.SetIDValue(this, id);
        }
    }
}