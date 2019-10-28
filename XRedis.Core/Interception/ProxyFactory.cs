using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using XRedis.Interception;

namespace XRedis.Core.Interception
{
    public class ProxyFactory : IProxyFactory
    {
        //IRecordInterceptor _recordInterceptor;
        IResolver _resolver;
        ProxyGenerator _generator = new ProxyGenerator();
        ProxyGenerationOptions _options;

        public ProxyFactory(IResolver resolver)
        {
            _resolver = resolver;
            _options = new ProxyGenerationOptions(_resolver.GetInstance<IProxyGenerationHook>());
        }

        public TRecord CreateClassProxy<TRecord, TKey>()
            where TRecord : class, IRecord<TKey>
        {
            var recordInterceptor = _resolver.GetInstance<IRecordInterceptor<TRecord, TKey>>();
            return _generator.CreateClassProxy<TRecord>(_options, recordInterceptor);
        }
    }
}
