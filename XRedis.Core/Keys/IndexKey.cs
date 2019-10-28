using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using StackExchange.Redis;
using Index = XRedis.Core.Fields.Indexes.Index;

namespace XRedis.Core.Keys
{
    public class IndexKey
    {
        public string Tabletype { get; set; }

        public List<string> IndexTags { get; set; }

        public static IndexKey Parse(string str)
        {
            var match = Regex.Match(str, $@"{Keys.Index}:(?<TableName>.*):(?<IndexTags>.*)");
            var tableName = match.Groups["TableName"].Value;
            var indexTags = match.Groups["indexTags"].Value.Split("+");
            return new IndexKey(tableName, indexTags);
        }
        public override string ToString()
        {
            return $"{Keys.Index}:{Tabletype}:{string.Join("+",IndexTags)}";
        }

        public IndexKey(string tableType, params string[] indexTags)
        {
            IndexTags = indexTags.ToList();
            Tabletype = tableType;
        }

        public IndexKey(Type tableType, params Index[] indexes) : this(tableType.Name, indexes.Select(i=>i.Tag).ToArray()) { }

        public IndexKey(params Index[] indexes) : this(indexes.Select(i=>i.RecordType).First().Name, indexes.Select(i => i.Tag).ToArray()) { }

        public static implicit operator RedisKey(IndexKey key) => key.ToString();
    }
}