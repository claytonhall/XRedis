namespace XRedis.Core.Keys
{
    public class VersionKey
    {
        public VersionKey()
        {
        }

        public override string ToString()
        {
            return $"{Keys.Version}";
        }
    }
}