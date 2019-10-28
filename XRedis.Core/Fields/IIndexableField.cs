using System;
using System.Reflection;

namespace XRedis.Core.Fields
{
    public interface IIndexableField
    {
        Type RecordType { get; }

        PropertyInfo PropertyInfo { get; }
    }

    public interface IKeyField : IIndexableField
    {

    }
}