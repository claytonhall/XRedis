using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using StackExchange.Redis;
using System.Transactions;

namespace RedisFirst.Core.Interception
{
    public interface IConnectionMultiplexerInterceptor : IInterceptor
    {

    }

    public class ConnectionMultiplexerInterceptor : IConnectionMultiplexerInterceptor
    {
        private IDatabase _database = null;

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.Name == "GetDatabase")
            {
                if (_database == null)
                {
                    var generator = new ProxyGenerator();
                    ProxyGenerationOptions options = new ProxyGenerationOptions(new DatabaseGenerationHook());
                    _database = generator.CreateInterfaceProxyWithTarget<IDatabase>(
                        ((IConnectionMultiplexer)invocation.InvocationTarget).GetDatabase(), options,
                        new DatabaseInterceptor());
                }
                invocation.ReturnValue = _database;
            }
            else
            {
                invocation.Proceed();
            }
        }
    }

    public class ConnectionMultiplexerGenerationHook : IProxyGenerationHook
    {
        public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            bool intercept = false;

            //if (Transaction.Current != null)
            //{
                if (methodInfo.Name == "GetDatabase")
                {
                    intercept = true;
                }
            //}
            return intercept;
        }

        public void MethodsInspected()
        {
        }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
            //throw new NotImplementedException();
            System.Diagnostics.Debug.WriteLine($"Non proxyable member: {type.Name}.{memberInfo.Name}");
        }
    }

    public interface IDatabaseInterceptor : IInterceptor
    {
    }

    public class DatabaseInterceptor : IDatabaseInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            IDatabase originalDatabase = (IDatabase)invocation.InvocationTarget;

            MethodInfo methodInfo = invocation.GetConcreteMethod();
            IDatabase record = (IDatabase)invocation.InvocationTarget;

            var redisKeyParamIndex = Array.FindIndex(methodInfo.GetParameters(), p=> p.ParameterType == typeof(RedisKey));
            if (redisKeyParamIndex != -1)
            {
                string transactionid = "1";

                RedisKey originalArgValue = (RedisKey)invocation.GetArgumentValue(redisKeyParamIndex);
                var newArgValue = $"Transaction:{transactionid}:" + originalArgValue.ToString();
                //invocation.SetArgumentValue(redisKeyParamIndex, "Transaction:transactionid:"+originalArgValue.ToString());
                System.Diagnostics.Debug.WriteLine($"method {methodInfo.Name}");
                System.Diagnostics.Debug.WriteLine($"original {originalArgValue}");
                System.Diagnostics.Debug.WriteLine($"new {newArgValue}");


                if (methodInfo.IsSignature("HashSet", typeof(void), typeof(RedisKey), typeof(RedisValue), typeof(When), typeof(CommandFlags)))
                {
                    if (originalDatabase.KeyExists((RedisKey)invocation.GetArgumentValue(0)))
                    {
                        var hashVals = originalDatabase.HashGetAll((RedisKey) invocation.GetArgumentValue(0));
                        originalDatabase.HashSet((RedisKey) invocation.GetArgumentValue(0), hashVals, (CommandFlags) invocation.GetArgumentValue(3));
                    }
                    H
                }
                else if(methodInfo.IsSignature("HashSet", typeof(void), typeof(RedisKey), typeof(HashEntry[]), typeof(CommandFlags)))
                {

                }
            


                //check to see if the record is locked...

                    //write using the TransactionKey!
                    invocation.SetArgumentValue(redisKeyParamIndex, newArgValue);
                    invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);

                    //mark the key as dirty... so no other transactions can reference it
                    originalDatabase.StringSet($"LOCK:{originalArgValue.ToString()}", $"Transaction:{transactionid}");

                    //need to ... block... not fail...
                }
                else if (methodInfo.Name == "ListRemove")
                {
                    originalDatabase.ListR
                    invocation.SetArgumentValue(redisKeyParamIndex, newArgValue);
                    invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);
                }
                else if (methodInfo.Name == "ListLeftPush")
                {
                    invocation.SetArgumentValue(redisKeyParamIndex, newArgValue);
                    invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);
                }
                else if (methodInfo.Name == "ListLeftPop")
                {
                    invocation.SetArgumentValue(redisKeyParamIndex, newArgValue);
                    invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);
                }
                else if (methodInfo.Name == "ListRightPush")
                {
                    invocation.SetArgumentValue(redisKeyParamIndex, newArgValue);
                    invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);
                }
                else if (methodInfo.Name == "ListRightPop")
                {
                    invocation.SetArgumentValue(redisKeyParamIndex, newArgValue);
                    invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);
                }
                else if (methodInfo.Name == "StringGet")
                {
                    //get the original value!
                    var obj = (RedisValue)invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);
                    
                    //update the transaction table!
                    originalDatabase.StringSet(newArgValue, obj);
                    originalDatabase.StringSet($"LOCK:{originalArgValue.ToString()}", $"Transaction:{transactionid}");
                }
                else if (methodInfo.Name == "StringSet")
                {
                    //update the transaction table!
                    invocation.SetArgumentValue(redisKeyParamIndex, newArgValue);
                    invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);
                    originalDatabase.StringSet($"LOCK:{originalArgValue.ToString()}", $"Transaction:{transactionid}");
                }
                else if (methodInfo.Name == "SortedSetAdd")
                {
                    invocation.SetArgumentValue(redisKeyParamIndex, newArgValue);
                    invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);

                    //mark the key as dirty... so no other transactions can reference it
                    originalDatabase.StringSet($"LOCK:{originalArgValue.ToString()}", $"Transaction:{transactionid}");
                }
                else if (methodInfo.Name == "SortedSetRemove")
                {
                    //remove from tran table!
                    invocation.SetArgumentValue(redisKeyParamIndex, newArgValue);
                    invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);

                    originalDatabase.StringSet($"LOCK:{originalArgValue.ToString()}", $"Transaction:{transactionid}");

                    //add to remove queue!
                    //originalDatabase.ListLeftPush(newArgValue)
                }
                else if (methodInfo.Name == "KeyExists")
                {
                    invocation.SetArgumentValue(redisKeyParamIndex, newArgValue);
                    var exists = (bool)invocation.MethodInvocationTarget.Invoke(invocation.InvocationTarget, invocation.Arguments);

                    originalDatabase.StringSet($"LOCK:{originalArgValue.ToString()}", $"Transaction:{transactionid}");
                }
                else if (methodInfo.Name == "HashGet")
                {
                    if (!originalDatabase.KeyExists(newArgValue))
                    {
                        if (originalDatabase.KeyExists(originalArgValue))
                        {
                            var hashes = originalDatabase.HashGetAll(originalArgValue);
                            originalDatabase.HashSet(newArgValue, hashes);
                        }
                    }
                }
                else if (methodInfo.Name == "SortedSetRangeByValue")
                {

                }
            }
            invocation.Proceed();
        }
    }

    public class DatabaseGenerationHook : IProxyGenerationHook
    {
        private bool HasRedisKeyArgs(MethodInfo methodInfo)
        {
            var index = Array.FindIndex(methodInfo.GetParameters(), p=> p.ParameterType == typeof(RedisKey));
            return index != -1;
        }


        public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            bool intercept = false;

            if (HasRedisKeyArgs(methodInfo))
            {
                intercept = true;
            }

            return intercept;
        }

        public void MethodsInspected()
        {
        }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
            //throw new NotImplementedException();
            System.Diagnostics.Debug.WriteLine($"Non proxyable member: {type.Name}.{memberInfo.Name}");
        }
    }

    public static class DbInterceptorExtensions
    {
        public static bool IsSignature(this MethodInfo methodInfo, string name, Type returnType, params Type[] parameters)
        {
            bool retVal = false;
            if (methodInfo.Name == name && methodInfo.ReturnType == returnType)
            {
                var miParams = methodInfo.GetParameters();

                if (miParams.Length == parameters.Length)
                {
                    for (int i = 0; i < miParams.Length; i++)
                    {
                        if (miParams[i].ParameterType != parameters[i])
                        {
                            break;
                        }
                    }
                    retVal = true;
                }
            }
            return retVal;
        }
    }
}
