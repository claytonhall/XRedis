using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public class XContext
    {
        public static XContext _xContext;
        
        public static XContext Instance
        {
            get
            {
                if (_xContext == null)
                {
                    _xContext = new XContext("localhost:6379");
                }
                return _xContext;
            }
        }

        public string ConnectionString { get; set; }

        IConnectionMultiplexer _connectionMultiplexer;
        ICacheClient _cacheClient;
        ISerializer _serializer = new NewtonsoftSerializer();

        public IConnectionMultiplexer ConnectionMultiplexer { get { return _connectionMultiplexer; } }
        public ICacheClient CacheClient { get { return _cacheClient; } }

        public XContext(string connectionString)
        {
            this.ConnectionString = connectionString;
            _connectionMultiplexer = ConnectionMultiplexer.Connect(this.ConnectionString);
            _cacheClient = new StackExchangeRedisCacheClient((ConnectionMultiplexer)_connectionMultiplexer, _serializer);
        }


    }
}
