using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using XRedis.Core.Extensions;

namespace XRedis.Core.Fields
{
    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ForeignKey : System.Attribute, IKeyField
    {
        public ForeignKey(Type parentRecordType, Type recordType, [CallerMemberName] string fkPropertyName = "")
        {
            ParentRecordType = parentRecordType;
            RecordType = recordType;
            PropertyInfo = recordType.GetProperty(fkPropertyName);
        }

        public ForeignKey(Type parentRecordType, Type recordType, PropertyInfo pkPropertyInfo)
        {
            ParentRecordType = parentRecordType;
            RecordType = recordType;
            PropertyInfo = pkPropertyInfo;
        }

        public Type ParentRecordType { get; }
        public Type RecordType { get; }
        public PropertyInfo PropertyInfo { get;}
        public string Name => PropertyInfo.Name;

        public IId GetValue<TRecord, TKey>(TRecord record)
            where TRecord : class, IRecord<TKey>
        {
            return record.GetIDValue<TRecord, TKey>(this);
            //return new Id(record.GetType().GetProperty(PropertyInfo.Name)?.GetValue(record)); 
            //(IId?)record.GetType().GetProperty(PropertyInfo.Name)?.GetValue(record) ?? default(IId);
        }

        public void SetValue(IRecord record, IId id)
        {
            record.SetIDValue(this, id);
            //record.GetType().GetProperty(PropertyInfo.Name)?.SetValue(record, (long)(Id)id);
        }
    }
}