using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace XRedis.Core.Extensions
{
    public static class TypeExtensions
    {
        //a thread-safe way to hold default instances created at run-time
        private static ConcurrentDictionary<Type, object> typeDefaults =
            new ConcurrentDictionary<Type, object>();

        public static object GetDefaultValue(this Type type)
        {
            return type.IsValueType
                ? typeDefaults.GetOrAdd(type, Activator.CreateInstance)
                : null;
        }

        //public static bool IsDefaultValue<T>(this T obj)
        //{
        //    return obj.GetType().GetDefaultValue() == obj;
        //}
    }
}
