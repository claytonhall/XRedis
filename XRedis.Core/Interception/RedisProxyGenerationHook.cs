using XRedis.Core;
using XRedis.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace XRedis.Interception
{
    public class RedisProxyGenerationHook : IProxyGenerationHook
    {
        private readonly ISchemaHelper _schemaHelper;
        public RedisProxyGenerationHook(ISchemaHelper schemaHelper)
        {
            _schemaHelper = schemaHelper;
        }

        public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            bool intercept = true;

            if (!methodInfo.IsVirtual)
            {
                intercept = false;
            }
            else if (!methodInfo.IsGetter() && !methodInfo.IsSetter())
            {
                intercept = false;
            }
            else if (IsIDPropertyMethod(methodInfo, methodInfo.DeclaringType))
            {
                intercept = false;
            }
            //else if (Attribute.IsDefined(type.GetProperty(methodInfo.PropertyName()), typeof(ComputedColumn)))
            //{
            //    intercept = false;
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

        private bool IsIDPropertyMethod(MethodInfo methodInfo, Type type)
        {
            if (methodInfo.IsGetter() || methodInfo.IsSetter())
            {
                var property = type.GetProperty(methodInfo.ToProperty().Name);
                var pkProperty = _schemaHelper.PrimaryKey(type);
                if (property.Name == pkProperty.Name)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
