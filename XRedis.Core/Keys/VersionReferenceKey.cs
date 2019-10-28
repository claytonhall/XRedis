using System.Text.RegularExpressions;
using StackExchange.Redis;

namespace XRedis.Core.Keys
{
    public class VersionReferenceKey
    {
        public static VersionReferenceKey Parse(string str)
        {
            var match = Regex.Match(str, $@"{Keys.VersionReference}:(?<RecordKey>.*)");
            var recordKey = RecordKey.Parse(match.Groups["RecordKey"].Value);
            return new VersionReferenceKey(recordKey);
        }

        public RecordKey RecordKey { get; set; }

        public VersionReferenceKey(RecordKey key)
        {
            RecordKey = key;
        }

        public override string ToString()
        {
            return $"{Keys.VersionReference}:{RecordKey}";
        }

        public static implicit operator RedisKey(VersionReferenceKey key) => key.ToString();

        
    }
}