using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XRedis.Core.Keys;

namespace XRedis.Core.Interception
{
    public interface IProxyFactory
    {
        TRecord CreateClassProxy<TRecord, TKey>()
            where TRecord: class, IRecord<TKey>;
    }
}
