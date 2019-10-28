using System;
using System.Text.RegularExpressions;
using StackExchange.Redis;
using XRedis.Core.Extensions;

namespace XRedis.Core.Keys
{
    public class VersionedRecordKey
    {
        public static VersionedRecordKey Parse(string str)
        {
            var match = Regex.Match(str, $@"(?<RecordKey>.*):{Keys.Version}:(?<Version>.*)");
            var recordKeyStr = match.Groups["RecordKey"].Value;
            var version = long.Parse(match.Groups["Version"].Value);
            var recordKey = RecordKey.Parse(recordKeyStr);
            return new VersionedRecordKey(recordKey, version);
        }

        public override string ToString()
        {
            return $"{RecordKey}:{Keys.Version}:{Version}";
        }

        public string ToSortableString()
        {
            
            return $"{RecordKey.ToSortableString()}:{Keys.Version}:{Version.ToString().PadLeft(long.MaxValue.ToString().Length, '0')}";
        }

        public VersionedRecordKey(RecordKey recordKey, long version)
        {
            RecordKey = recordKey;
            Version = version;
        }

        public VersionedRecordKey(Type tableType, IId id, long version) : this(new RecordKey(tableType,id), version) { }

        public VersionedRecordKey(IRecord record, IId id, long version) : this(record.GetType().GetUnproxiedType(), id, version) { }

        //public VersionedRecordKey(IRecord record, long version) : this(record, record.GetID(), version) { }


        public long Version { get; set; }

        public RecordKey RecordKey { get; set; }

        public string TableType { get { return RecordKey.TableType; } }

        public IId Id { get { return RecordKey.Id; } }


        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, this)) return true;

            return string.Equals(obj?.ToString(), ToString(), StringComparison.CurrentCulture);
        }

        public static implicit operator VersionedRecordKey(RedisKey key) => VersionedRecordKey.Parse(key);
        public static implicit operator VersionedRecordKey(RedisValue key) => VersionedRecordKey.Parse(key);
        public static implicit operator RedisKey(VersionedRecordKey key) => key.ToString();
        public static implicit operator RedisValue(VersionedRecordKey key) => key.ToString();
    }
}