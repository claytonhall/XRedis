using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XRedis.Core.Interception
{
    public interface IProxyFactory
    {
        //object CreateClassProxy(Type type);

        T CreateClassProxy<T>()
            where T : class, IRecord;
    }
}
