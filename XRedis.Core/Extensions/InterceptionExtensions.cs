using XRedis.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XRedis.Core.Extensions
{
    public static class InterceptionExtensions
    {
        //public static bool IsIDPropertyMethod<T>(this MethodInfo methodInfo)
        //{
        //    return methodInfo.IsIDPropertyMethod(typeof(T));
        //}

        //public static bool IsIDPropertyMethod(this MethodInfo methodInfo, Type type)
        //{
        //    if (methodInfo.IsGetter() || methodInfo.IsSetter())
        //    {
        //        var property = type.GetProperty(methodInfo.ToProperty().Name);
        //        var pkProperty = SchemaHelper.Instance.GetPrimaryKey(type);
        //        if (property.Name == pkProperty.Name)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        public static string PropertyName(this MethodBase methodInfo)
        {
            //remove get_/set_
            return methodInfo.Name.Substring(4);
        }

        public static PropertyInfo ToProperty(this MethodBase methodInfo)
        {
            var propertyName = Regex.Replace(methodInfo.Name, @"^[gs]et_(.*)$", "$1");
            return methodInfo.DeclaringType.GetUnproxiedType().GetProperty(propertyName);
        }

        public static bool IsGetter(this MethodBase methodInfo)
        {
            return methodInfo.Name.StartsWith("get_", StringComparison.Ordinal);
        }

        public static bool IsSetter(this MethodBase methodInfo)
        {
            return methodInfo.Name.StartsWith("set_", StringComparison.Ordinal);
        }
    }
}
