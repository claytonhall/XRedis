using System.Text.RegularExpressions;
using StackExchange.Redis;

namespace XRedis.Core.Keys
{
    public class IndexValue
    {
        public static IndexValue Parse(string str)
        {
            var match = Regex.Match(str, $@"(?<Value>.*?):(?<VersionedRecordKey>.*)");
            var value = match.Groups["Value"].Value;
            var vkey = match.Groups["VersionedRecordKey"].Value;
            return new IndexValue(VersionedRecordKey.Parse(vkey), value);
        }

        public override string ToString()
        {
            return $"{Value}:{VersionedRecordKey}";
        }

        public string Value { get; }

        public VersionedRecordKey VersionedRecordKey { get; set; }

        public RecordKey RecordKey
        {
            get { return VersionedRecordKey.RecordKey; }
        }

        public IndexValue(VersionedRecordKey versionedRecordKey, string value)
        {
            //Values = values;
            Value = value;
            VersionedRecordKey = versionedRecordKey;
        }


        public static implicit operator RedisValue(IndexValue indexValue) => indexValue.ToString();
        public static implicit operator IndexValue(RedisValue value) => IndexValue.Parse(value);
    }
}