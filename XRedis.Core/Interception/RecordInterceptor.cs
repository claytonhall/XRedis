using System.Collections;
using System.Linq;
using System.Reflection;
using System.Transactions;
using Castle.DynamicProxy;
using StackExchange.Redis;
using XRedis.Core.Extensions;

namespace XRedis.Core.Interception
{
    public interface IRecordInterceptor<TRecord, TKey> : IInterceptor
        where TRecord : class, IRecord<TKey>
    {
    }

    public class RecordInterceptor<TRecord, TKey> : IRecordInterceptor<TRecord, TKey>
        where TRecord : class, IRecord<TKey>
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
            var id = record.GetID<TRecord, TKey>();
            if (methodInfo.IsGetter())
            {
                if (IsRecordProperty(methodInfo))
                {
                    var keyType = methodInfo.ReturnType.GetInterfaces()
                        .Single(i => i.IsGenericType)
                        .GetGenericArguments()[0];

                    //var keyType = methodInfo.ReturnType.GetGenericArguments()[0];

                    invocation.ReturnValue = rm.GetType()
                        .GetMethod(nameof(rm.GetNavigationRecord))
                        ?.MakeGenericMethod(typeof(TRecord), typeof(TKey), methodInfo.ReturnType, keyType)
                        .Invoke(rm, new object[]{record});
                }
                else if (IsRecordSetProperty(methodInfo))
                {
                    invocation.ReturnValue = GetRecordSet(methodInfo, record);
                }
                else
                {
                    //var recordType = methodInfo.ReturnType;
                    //var keyType = methodInfo.ReturnType.GetGenericArguments()[0];
                    //var propertyInfo = methodInfo.ToProperty();

                    //invocation.ReturnValue = rm.GetType()
                    //    .GetMethod(nameof(rm.GetValue))
                    //    ?.MakeGenericMethod(recordType, keyType)
                    //    .Invoke(rm, new object[] { record, propertyInfo });
                    invocation.ReturnValue = rm.GetValue<TRecord, TKey>(record, methodInfo.ToProperty());
                }
            }
            else if (methodInfo.IsSetter())
            {
                rm.SetValue<TRecord, TKey>(record, methodInfo.ToProperty(), id, invocation.Arguments[0]);
            }
            else if (IsDeleteMethod(methodInfo))
            {
                //var recordType = methodInfo.ReturnType;
                //var keyType = methodInfo.ReturnType.GetGenericArguments()[0];
                //invocation.ReturnValue = rm.GetType()
                //    .GetMethod(nameof(rm.Delete))
                //    ?.MakeGenericMethod(recordType, keyType)
                //    .Invoke(rm, new object[] { record });
                rm.Delete<TRecord, TKey>(record);
            }
        }

        private IRecordSetWithParent<TRecord> GetRecordSet(MethodInfo methodInfo, TRecord record)
        {
            var typeArgs = methodInfo.ReturnType.GenericTypeArguments;
            var genType = typeof(IRecordSet<,,,>).MakeGenericType(typeArgs);
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
