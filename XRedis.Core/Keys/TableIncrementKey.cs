using System;
using StackExchange.Redis;
using XRedis.Core.Extensions;

namespace XRedis.Core.Keys
{
    public class TableIncrementKey
    {
        public string TableName { get; set; }

        public TableIncrementKey(string tableName)
        {
            TableName = tableName;
        }

        public TableIncrementKey(Type tableType) : this(tableType.GetUnproxiedType().Name)
        {
        }

        //public TableIncrementKey(IRecord record) :this(record.GetType()) { }
        //public string Value { get; set; }

        public override string ToString()
        {
            return $"{Keys.Identity}:{TableName}";
        }

        public static implicit operator RedisKey(TableIncrementKey key) => key.ToString();
    }
}