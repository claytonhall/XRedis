using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using XRedis.Core.Keys;
using Index = XRedis.Core.Fields.Indexes.Index;

namespace XRedis.Core.Extensions
{
    public static class KeyExtensions
    {
        public static TableIncrementKey GetIncrementKey(this IRecord record)
        {
            var key = new TableIncrementKey(record.GetType());
            return key;
        }

        //public static TableIncrementKey GetIncrementKey(this Type recordType)
        //{
        //    var key = new TableIncrementKey(recordType.GetUnproxiedType());
        //    return key;
        //}

        public static RecordKey GetRecordKey(this IRecord record, IId id)
        {
            var key = new RecordKey(record, id);
            return key;
        }

        public static RecordKey GetRecordKey<TRecord, TKey>(this TRecord record)
            where TRecord : class, IRecord<TKey>
        {
            var key = RecordKey.Create<TRecord, TKey>(record);
            //new RecordKey(record);
            return key;
        }

        public static RecordKey GetRecordKey(this Type type, IId id)
        {
            var key = new RecordKey(type, id);
            return key;
        }

        public static VersionReferenceKey GetVersionReferenceKey(this RecordKey recordKey)
        {
            var key = new VersionReferenceKey(recordKey);
            return key;
        }

        public static VersionedRecordKey GetVersionedRecordKey(this RecordKey recordKey, long version)
        {
            var key = new VersionedRecordKey(recordKey, version);
            return key;
        }

        public static IndexReferenceKey GetIndexReferenceKey(this IRecord record, VersionedRecordKey vkey,
            params Index[] indexes)
        {
            return record.GetType().GetUnproxiedType().GetIndexReferenceKey(vkey, indexes);
        }

        public static IndexReferenceKey GetIndexReferenceKey(this Type tableType, VersionedRecordKey vkey, params Index[] indexes)
        {
            return new IndexReferenceKey(tableType, vkey, indexes);
        }
    }
}
