using System;
using System.Text.RegularExpressions;
using StackExchange.Redis;
using XRedis.Core.Extensions;

namespace XRedis.Core.Keys
{
    public class RecordKey
    {
        public static RecordKey Parse(string str)
        {
            var match = Regex.Match(str, $@"{Keys.Table}:(?<TableName>.*):(?<Id>.*)");
            var tableName = match.Groups["TableName"].Value;
            var id = match.Groups["Id"].Value.TrimStart('0');   //trim leading zeros
            return new RecordKey(tableName, id);
        }

        public override string ToString()
        {
            return $"{Keys.Table}:{TableType}:{Id}";
        }

        public string ToSortableString()
        {
            return $"{Keys.Table}:{TableType}:{Id.ToSortableString()}";
            
        }

        public string TableType { get; set; }

        public Id Id { get; set; }

        public RecordKey(string type, string id)
        {
            TableType = type;
            Id = new Id(id);  //Id<long>.Parse(id);
        }

        public RecordKey(Type tableType, Id id) : this(tableType.GetUnproxiedType().Name, id.ToString())
        {
        }

        public RecordKey(IRecord record, Id id) :this(record.GetType(), id) { }

        public RecordKey(IRecord record) : this(record, record.GetID()){ }

       

        public static implicit operator RedisKey(RecordKey key) => key.ToString();
        public static implicit operator RecordKey(RedisKey key) => RecordKey.Parse(key);
        public static implicit operator RedisValue(RecordKey key) => key.ToString();
    }
}