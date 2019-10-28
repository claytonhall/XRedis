using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XRedis.Core.Extensions;
using StackExchange.Redis;
using XRedis.Core.Interception;

namespace XRedis.Core
{
    public interface IQueueListener
    {
        //void Listen(Action<IRecord> action);
    }

    public class QueueListener : IQueueListener
    {
        //IConnectionMultiplexer _redis;
        //IProxyFactory _proxyFactory;

        //Dictionary<string, Type> _recordTypes;

        //public QueueListener(IConnectionMultiplexer redis, IProxyFactory proxyFactory)
        //{
        //    _recordTypes = AppDomain.CurrentDomain.GetAssemblies()
        //        .Where(a => !a.IsDynamic)
        //        .SelectMany(a => a.GetTypes())
        //        .Where(t => typeof(IRecord).IsAssignableFrom(t))
        //        .ToDictionary(t=>t.Name);
        //    _redis = redis;
        //    _proxyFactory = proxyFactory;
        //}

        //public void Listen(Action<IRecord> action)
        //{
        //    var db = _redis.GetDatabase();
        //    while (true)
        //    {
        //        try
        //        {
        //            var key = db.ListRightPop("QUEUE").ToString();

        //            if (key != null)
        //            {
        //                int firstColon = key.IndexOf(":");
        //                int secondColon = key.IndexOf(":", firstColon + 1);

        //                string tableName = key.Substring(firstColon + 1, secondColon - firstColon - 1);
        //                var id = Id<long>.Parse(key.Substring(secondColon + 1));

        //                //now get the type of the table!
        //                if (_recordTypes.ContainsKey(tableName))
        //                {
        //                    Type recordType = _recordTypes[tableName];
        //                    var proxyObj = (IRecord)_proxyFactory.CreateClassProxy(recordType);
        //                    proxyObj.SetID(id);
        //                    action(proxyObj);
        //                }
        //                else
        //                {
        //                    throw new ApplicationException($"Unknown table {tableName}");
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex);
        //        }
        //    }
        //}
    }
}
