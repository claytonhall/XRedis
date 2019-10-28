namespace XRedis.Core
{
    public class XRedisConnection : IXRedisConnection
    {
        public string ConnectionString { get; }

        public XRedisConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}