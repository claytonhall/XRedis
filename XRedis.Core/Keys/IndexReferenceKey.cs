using System;
using System.Text.RegularExpressions;
using StackExchange.Redis;
using Index = XRedis.Core.Fields.Indexes.Index;

namespace XRedis.Core.Keys
{
    public class IndexReferenceKey
    {
        public IndexKey IndexKey { get; set; }

        public VersionedRecordKey VersionedRecordKey { get; set; }

        public static IndexReferenceKey Parse(string str)
        {
            var match = Regex.Match(str, $@"{Keys.IndexReference}:(?<IndexKey>.*);(?<VersionedRecordKey>.*)");
            var indexKeyStr = match.Groups["IndexKey"].Value;
            var vkeyStr = match.Groups["VersionedRecordKey"].Value;
            var indexKey = IndexKey.Parse(indexKeyStr);
            var vkey = VersionedRecordKey.Parse(vkeyStr);
            return new IndexReferenceKey(indexKey, vkey);
        }


        public IndexReferenceKey(IndexKey indexKey, VersionedRecordKey versionedRecordKey)
        {
            IndexKey = indexKey;
            VersionedRecordKey = versionedRecordKey;
        }

        public IndexReferenceKey(Type tableType, VersionedRecordKey versionedRecordKey, params Index[] indexes)
            : this(new IndexKey(tableType, indexes), versionedRecordKey)
        {
        }

        public override string ToString()
        {
            return $"{Keys.IndexReference}:{IndexKey};{VersionedRecordKey}";
        }

        public static implicit operator RedisKey(IndexReferenceKey key) => key.ToString();
    }
}