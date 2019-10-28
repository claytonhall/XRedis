using XRedis.Core.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using XRedis.Core;
using XRedis.Core.Fields;
using XRedis.Core.Fields.Indexes;
using Index = XRedis.Core.Fields.Indexes.Index;

namespace XRedis.Core
{
    public interface ISchemaHelper
    {
        void Load(IEnumerable<Type> types);

        List<PrimaryKey> PrimaryKeys();
        PrimaryKey PrimaryKey<T>(T record);
        PrimaryKey PrimaryKey(Type recordType);

        List<ForeignKey> ForeignKeys();
        List<ForeignKey> ForeignKeys<T>(T record);
        List<ForeignKey> ForeignKeys(Type type);
        ForeignKey ForeignKey<T>(T record, Type parentRecordType);
        ForeignKey ForeignKey(Type childRecordType, Type parentRecordType);
        
        List<Index> Indexes();
        Index PkIndex(PrimaryKey primaryKey);
        List<Index> Indexes<T>(T record);
        List<Index> Indexes(Type recordType);
        Index PkIndex<T>(T record);
        Index PkIndex(Type recordType);
        Index FkIndex(Type recordType, Type parentRecordType);
        Index FkIndex(ForeignKey foreignKey);
        Index FkIndex<T>(T record, Type parentRecordType);
        Index CompoundIndex(params Index[] indexes);
        Index Index<T>(T record, string tag);
        Index Index(Type recordType, string tag);
    }

    public class SchemaHelper : ISchemaHelper
    {
        private List<PrimaryKey> _primaryKeys;
        private List<ForeignKey> _foreignKeys;
        private List<Index> _indexes;

        public List<PrimaryKey> PrimaryKeys() => _primaryKeys.ToList();
        public PrimaryKey PrimaryKey<T>(T record) => PrimaryKey(record.GetType());
        public PrimaryKey PrimaryKey(Type recordType) => PrimaryKeys().Single(pk => pk.RecordType == recordType.GetUnproxiedType());

        public List<ForeignKey> ForeignKeys() => _foreignKeys.ToList();
        public List<ForeignKey> ForeignKeys<T>(T record) => ForeignKeys(record.GetType());
        public List<ForeignKey> ForeignKeys(Type type) => _foreignKeys.Where(fk => fk.RecordType == type.GetUnproxiedType()).ToList();
        public ForeignKey ForeignKey<T>(T record, Type parentRecordType) => ForeignKey(record.GetType(), parentRecordType);
        public ForeignKey ForeignKey(Type childRecordType, Type parentRecordType) => ForeignKeys(childRecordType).Single(fk => fk.ParentRecordType == parentRecordType.GetUnproxiedType());
        
        public List<Index> Indexes() => _indexes.ToList();
        public List<Index> Indexes<T>(T record) => Indexes(record.GetType());
        public List<Index> Indexes(Type recordType) => Indexes().Where(i => i.RecordType == recordType.GetUnproxiedType()).ToList();
        public Index Index<T>(T record, string tag) => Index(record.GetType(), tag);
        public Index Index(Type recordType, string tag) => Indexes(recordType).Single(i => i.Tag == tag);
        public Index CompoundIndex(params Index[] indexes) => Indexes().Single(i => i is CompoundIndex compoundIndex && !compoundIndex.Indexes.Except(indexes).Any());
        //public Index CompoundIndex(params Index[] indexes) => Indexes().Single(i => i.Tag == string.Join("+",indexes.Select(ix=>ix.Tag)));


        public Index PkIndex<T>(T record) => PkIndex(PrimaryKey<T>(record));
        public Index PkIndex(Type recordType) => PkIndex(PrimaryKey(recordType));
        public Index PkIndex(PrimaryKey primaryKey) => Indexes().Single(i => i.RecordType == primaryKey.RecordType && i.IndexableField == primaryKey);

        public Index FkIndex<T>(T record, Type parentRecordType) => FkIndex(ForeignKey<T>(record, parentRecordType));
        public Index FkIndex(Type recordType, Type parentRecordType) => FkIndex(ForeignKey(recordType, parentRecordType));
        public Index FkIndex(ForeignKey foreignKey) => Indexes().Single(i => i.RecordType == foreignKey.RecordType && i.IndexableField == foreignKey);


        static object _instanceLockObj = new object();
        static SchemaHelper _instance;
        public static SchemaHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLockObj)
                    {
                        if (_instance == null)
                        {
                            _instance = new SchemaHelper();
                        }
                    }
                }
                return _instance;
            }
        }

        public SchemaHelper()
        {
            var iRecords = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IRecord).IsAssignableFrom(t) && t != typeof(IRecord));
            Load(iRecords);
            _instance = this;
        }

        

        public void Load(IEnumerable<Type> types)
        {
            LoadPrimaryKeys(types);
            LoadForeignKeys(types);
            LoadIndexes(types);
        }

        private void LoadPrimaryKeys(IEnumerable<Type> types)
        {
            var pkAttributes = types.SelectMany(t =>
                    t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                )
                .Where(property => Attribute.IsDefined(property, typeof(PrimaryKey)))
                .Select(property => property.GetCustomAttribute<PrimaryKey>());

            var regex = new Regex(@"(\w*)_?ID", RegexOptions.IgnoreCase);
            var pkConvention = types.SelectMany(t =>
                    t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                )
                .Where(property => !Attribute.IsDefined(property, typeof(PrimaryKey)))
                .Where(property => regex.IsMatch(property.Name))
                .Where(p => p.DeclaringType.Name == regex.Replace(p.Name, @"$1"))
                .Select(x => new PrimaryKey(x.DeclaringType, x));

            _primaryKeys = pkAttributes.Union(pkConvention).ToList();
        }

        private void LoadForeignKeys(IEnumerable<Type> types)
        {
            var fkAttributes = types.SelectMany(t =>
                    t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                )
                .Where(property => Attribute.IsDefined(property, typeof(ForeignKey)))
                .Select(property => property.GetCustomAttribute<ForeignKey>());

            var regex = new Regex(@"(\w*)_?ID", RegexOptions.IgnoreCase);
            var fkConvention = types.SelectMany(t =>
                    t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                )
                .Where(property => regex.IsMatch(property.Name))
                .Where(property => types.Any(t => t != property.DeclaringType && t.Name == regex.Replace(property.Name, @"$1")))
                .Select(p => new {PropertyInfo = p, ParentType = types.First(t => t != p.DeclaringType && t.Name == regex.Replace(p.Name, @"$1"))})
                .Select(x => new ForeignKey(x.ParentType, x.PropertyInfo.DeclaringType, x.PropertyInfo))
                .ToList();

            _foreignKeys = fkAttributes.Union(fkConvention).ToList();
        }

        private void LoadIndexes(IEnumerable<Type> types)
        {
            var attributeIndexGroups = types.SelectMany(t =>
                    t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                )
                .Where(property => !Attribute.IsDefined(property, typeof(ForeignKey)))
                .Where(property => !Attribute.IsDefined(property, typeof(PrimaryKey)))
                .Where(p => Attribute.IsDefined(p, typeof(Index)))
                .SelectMany(p => p.GetCustomAttributes<Index>()
                    .Select(a => new {PropertyInfo = p, Index = a}))
                .Select(x => new Index(
                    x.Index.Tag,
                    x.PropertyInfo.DeclaringType,
                    new IndexableField(x.PropertyInfo.DeclaringType, x.PropertyInfo),
                    x.Index.Formatter))
                .GroupBy(i => new {i.RecordType, i.Tag})
                .ToList();

            var compoundAttributeIndexes = attributeIndexGroups
                .Where(g => g.Count() > 1)
                .Select(g => new CompoundIndex(g.Key.RecordType, g.OrderBy(i=>i.Order)))
                .ToList();

            var attributeIndexes = attributeIndexGroups
                .Where(g => g.Count() == 1)
                .Select(g => g.First())
                .ToList();


            var pkIndexes = _primaryKeys
                .Select(pk => new Index(pk.Name, pk.RecordType, pk))
                .ToList();

            var fkIndexes = _foreignKeys
                .Select(fk => new Index(fk.Name, fk.RecordType, fk))
                .ToList();

            //foreignKeys + attributeIndexes
            var fkPlusCompoundAttributeIndexes = fkIndexes.SelectMany(fk => compoundAttributeIndexes.Where(cai=>cai.RecordType == fk.RecordType),
                    (fk, cai) => new CompoundIndex(fk.RecordType, new[] {fk, cai}))
                .ToList();
            var fkPlusAttributeIndexes = fkIndexes.SelectMany(fk => attributeIndexes.Where(ai => ai.RecordType == fk.RecordType),
                    (fk, ai) => new CompoundIndex(fk.RecordType, new[] { fk, ai }))
                .ToList();

            _indexes = attributeIndexes
                .Union(pkIndexes)
                .Union(fkIndexes)
                .Union(fkPlusCompoundAttributeIndexes)
                .Union(fkPlusAttributeIndexes)
                .ToList();
        }
    }
}
