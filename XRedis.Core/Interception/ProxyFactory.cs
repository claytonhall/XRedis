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

        //public object CreateClassProxy(Type type)
        //{
        //    if (_recordInterceptor == null)
        //    {
        //        _recordInterceptor = _resolver.GetInstance<IRecordInterceptor>();
        //    }

        //    return _generator.CreateClassProxy(type, _options, _recordInterceptor);
        //}

        public T CreateClassProxy<T>()
            where T : class, IRecord
        {
            //if (_recordInterceptor == null)
            //{
            //    _recordInterceptor = _resolver.GetInstance<IRecordInterceptor<T>>();
            //}
            var recordInterceptor = _resolver.GetInstance<IRecordInterceptor<T>>();
            return _generator.CreateClassProxy<T>(_options, recordInterceptor);
        }
    }
}
