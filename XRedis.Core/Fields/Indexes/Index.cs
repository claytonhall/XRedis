using System;
using System.IO;
using System.Runtime.CompilerServices;
using XRedis.Core.Fields.IndexFormatters;
using XRedis.Core.Keys;

namespace XRedis.Core.Fields.Indexes
{
    [System.AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class Index : System.Attribute
    {
        private string _tag = "";
        private IIndexableField _indexableField;
        private int _order;
        private Type _recordType;
        private IIndexFormatter _formatter;

        public IIndexableField IndexableField => _indexableField;

        public virtual Type RecordType => _recordType;

        public virtual string Tag => _tag;

        public IIndexFormatter Formatter => _formatter;

        public int Order => _order;

        public Index()
        {
            
        }

        public Index(string tag, Type recordType, Type indexFormatterType = null, [CallerMemberName] string indexableFieldProperty = "", int order = 0)
        {
            indexFormatterType ??= typeof(DefaultIndexFormatter);
            var formatter = (IIndexFormatter)Activator.CreateInstance(indexFormatterType);

            var prop = recordType.GetProperty(indexableFieldProperty);
            var indexableField = new IndexableField(recordType, prop);
            Setup(tag, recordType, indexableField, formatter, order);
        }

        public Index(string tag, Type recordType, IIndexableField indexableField, Type indexFormatterType = null, int order = 0)
        {
            indexFormatterType ??= typeof(DefaultIndexFormatter);
            var formatter = (IIndexFormatter)Activator.CreateInstance(indexFormatterType);
            Setup(tag, recordType, indexableField, formatter, order);
        }

        public Index(string tag, Type recordType, IIndexableField indexableField, IIndexFormatter formatter, int order = 0)
        {
            Setup(tag, recordType, indexableField, formatter, order);
        }

        private void Setup(string tag, Type recordType, IIndexableField indexableField, IIndexFormatter formatter, int order = 0)
        {
            _indexableField = indexableField;
            _recordType = recordType;
            _formatter = formatter;
            _tag = tag;
            _order = order;
        }
       
        public virtual string Format(IRecord record)
        {
            var property = record.GetType().GetProperty(IndexableField.PropertyInfo.Name);
            var value = property.GetValue(record);
            return Formatter.Format(value);
        }

        public IndexKey Key => new IndexKey(this);


    }
}
