using StackExchange.Redis;
using System;
using System.Linq;
using Castle.DynamicProxy;
using XRedis.Interception;
using SimpleInjector;
using XRedis.Core.Interception;

namespace XRedis.Core
{
    public interface IXRedisConnection
    {
        string ConnectionString { get; }
    }


    public class XRedisContext
    {
        readonly IResolver _resolver;

        public XRedisContext(IXRedisConnection connection)
        {
            Container container = new Container();

            container.RegisterConditional(typeof(ILogger),
                context =>
                {
                    if (context.Consumer != null)
                    {
                        return typeof(Log4NetProxy<>).MakeGenericType(context.Consumer.ImplementationType);
                    }
                    return typeof(Log4NetProxy<>).MakeGenericType(typeof(ILogger));
                },
                 Lifestyle.Singleton, context => true);
            container.Register<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(connection.ConnectionString), Lifestyle.Singleton);
            container.Register(typeof(IRecordInterceptor<,>), typeof(RecordInterceptor<,>), Lifestyle.Singleton);
            container.Register(typeof(IRecordSet<,>), typeof(RecordSet<,>));
            container.Register(typeof(IRecordSet<,,,>), typeof(RecordSet<,,,>));
            container.Register(typeof(IRecordSetEnumerator<,>), typeof(RecordSetEnumerator<,>));
            container.Register<IResolver>(() => new Resolver(container), Lifestyle.Singleton);
            container.Register<IProxyFactory, ProxyFactory>(Lifestyle.Singleton);
            container.Register<IQueueListener, QueueListener>();
            container.Register<ISchemaHelper, SchemaHelper>(Lifestyle.Singleton);
            container.Register<IProxyGenerationHook, RedisProxyGenerationHook>();
            container.Register<IResourceManagerFactory, ResourceManagerFactory>(Lifestyle.Singleton);
         
            _resolver = container.GetInstance<IResolver>();
        }

        public IRecordSet<TRecord, TKey> CreateRecordSet<TRecord, TKey>()
            where TRecord : class, IRecord<TKey>
        {
            return _resolver.GetInstance<IRecordSet<TRecord, TKey>>();
        }

        public IResolver Resolver
        {
            get { return _resolver; }
        }

        public void FlushDb()
        {
            var redis = _resolver.GetInstance<IConnectionMultiplexer>();
            redis.GetServer(redis.GetEndPoints()[0]).FlushDatabase();
        }
    }
}
