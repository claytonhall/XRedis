//using ExpressionEvaluator;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using XRedis.Core.Fields;
using XRedis.Core.Keys;
using Index = XRedis.Core.Fields.Indexes.Index;

namespace XRedis.Core.Extensions
{
    public static class RecordExtensions
    {
        private static ISchemaHelper _schemaHelper => Resolver.Instance.GetInstance<ISchemaHelper>();

        public static PrimaryKey PrimaryKey<T>(this T record) => _schemaHelper.PrimaryKey<T>(record);
        public static IEnumerable<ForeignKey> ForeignKeys<T>(this T record) => _schemaHelper.ForeignKeys<T>(record);
        public static IEnumerable<Index> Indexes<T>(this T record) => _schemaHelper.Indexes<T>(record);

        public static Id GetID<T>(this T record)
            where T : IRecord
        {
            return record.PrimaryKey().GetValue(record);
        }

        public static void SetID<T>(this T record, Id id)
            where T : IRecord
        {
            record.PrimaryKey().SetValue(record, id);
        }

        public static Id GetIDValue<T>(this T record, IKeyField keyField)
        {
            return new Id(record.GetType().GetProperty(keyField.PropertyInfo.Name)?.GetValue(record));
        }

        public static void SetIDValue<T>(this T record, IKeyField keyField, Id id)
        {
            var property = record.GetType().GetProperty(keyField.PropertyInfo.Name);
            var idValue = id.Value;
            if (id.Value.GetType() != keyField.PropertyInfo.PropertyType)
            {
                var converter = TypeDescriptor.GetConverter(keyField.PropertyInfo.PropertyType);
                idValue = converter.ConvertFrom(id.Value);
            }
            property?.SetValue(record, idValue);
        }


        public static ForeignKey ForeignKey<T>(this T record, IRecord parentRecord)
        {
            return record.ForeignKey(parentRecord.GetType());
        }

        public static ForeignKey ForeignKey<T>(this T record, Type parentRecordType)
        {
            return record.ForeignKeys<T>().Single(fk => fk.ParentRecordType == parentRecordType.GetUnproxiedType());
        }

        public static void Delete<T>(this T record)
        {
        }

        public static Type GetUnproxiedType(this Type type)
        {
            if (type.Namespace == "Castle.Proxies")
            {
                type = type.BaseType;
            }
            return type;
        }

        public static IndexValue GetIndexValue(this IRecord record, Index index, VersionedRecordKey versionedRecordKey)
        {
            var indexValue = index.Format(record);
            return new IndexValue(versionedRecordKey, indexValue);
        }
    }
}
