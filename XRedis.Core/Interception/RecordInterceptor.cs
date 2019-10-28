using System.Collections;
using System.Reflection;
using System.Transactions;
using Castle.DynamicProxy;
using StackExchange.Redis;
using XRedis.Core.Extensions;

namespace XRedis.Core.Interception
{
    public interface IRecordInterceptor<TRecord> : IInterceptor
        where TRecord : class, IRecord
    {
    }

    public class RecordInterceptor<TRecord> : IRecordInterceptor<TRecord>
        where TRecord : class, IRecord
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IProxyFactory _proxyFactory;
        private readonly IResolver _resolver;
        private readonly IResourceManagerFactory _resourceManagerFactory;
        
        public RecordInterceptor(
            IConnectionMultiplexer connectionMultiplexer, 
            IProxyFactory proxyFactory, 
            IResolver resolver,
            IResourceManagerFactory resourceManagerFactory)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _proxyFactory = proxyFactory;
            _resolver = resolver;
            _resourceManagerFactory = resourceManagerFactory;
        }

        public void Intercept(IInvocation invocation)
        {
            if (Transaction.Current == null)
            {
                using (var scope = new TransactionScope())
                {
                    InterceptInternal(invocation);
                    scope.Complete();
                }
            }
            else
            {
                InterceptInternal(invocation);
            }
        }


        private void InterceptInternal(IInvocation invocation)
        {
            MethodInfo methodInfo = invocation.GetConcreteMethod();
            var record = (TRecord)invocation.InvocationTarget;

            var rm = _resourceManagerFactory.GetInstance();
            var id = record.GetID();
            if (methodInfo.IsGetter())
            {
                if (IsRecordProperty(methodInfo))
                {
                    invocation.ReturnValue = rm.GetType()
                        .GetMethod(nameof(rm.GetNavigationRecord))
                        ?.MakeGenericMethod(methodInfo.ReturnType)
                        .Invoke(rm, new object[]{record});
                }
                else if (IsRecordSetProperty(methodInfo))
                {
                    invocation.ReturnValue = GetRecordSet(methodInfo, record);
                }
                else
                {
                    invocation.ReturnValue = rm.GetValue(record, methodInfo.ToProperty());
                }
            }
            else if (methodInfo.IsSetter())
            {
                rm.SetValue(record, methodInfo.ToProperty(), id, invocation.Arguments[0]);
            }
            else if (IsDeleteMethod(methodInfo))
            {
                rm.Delete(record);
            }
        }

        private IRecordSetWithParent<TRecord> GetRecordSet(MethodInfo methodInfo, TRecord record)
        {
            var typeArgs = methodInfo.ReturnType.GenericTypeArguments;
            var genType = typeof(IRecordSet<,>).MakeGenericType(typeArgs);
            var recordSet = (IRecordSetWithParent<TRecord>)_resolver.GetInstance(genType);
            recordSet.ParentRecord = record;
            return recordSet;
        }

        private bool IsRecordSetProperty(MethodInfo methodInfo)
        {
            return methodInfo.ReturnType != typeof(string) 
                   && typeof(IEnumerable).IsAssignableFrom(methodInfo.ReturnType);
        }

        private bool IsRecordProperty(MethodInfo methodInfo)
        {
            return typeof(IRecord).IsAssignableFrom(methodInfo.ReturnType);
        }

        private bool IsDeleteMethod(MethodInfo methodInfo)
        {
            return methodInfo.Name.ToUpper() == "DELETE";
        }
    }
}
