﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace XRedis.Core
{
    public struct Id
    {
        public object Value { get; }

        public Id(object value)
        {
            this.Value = value;
        }

        public static implicit operator long(Id id)
        {
            return (long?) id.Value ?? default;
        }

        public static implicit operator int(Id id)
        {
            return (int?) id.Value ?? default;
        }

        public override string ToString() => Value.ToString();

        public string ToSortableString()
        {
            //handle other types!
            return Value.ToString().PadLeft(long.MaxValue.ToString().Length, '0');
        }
            
    }



    //public struct Id<T> : IId
    //{
    //    public T Value { get; }

    //    public Id(T value)
    //    {
    //        this.Value = value;
    //    }

    //    public static explicit operator T(Id<T> id)
    //    {
    //        return id.Value;
    //    }

    //    public static explicit operator Id<T>(T value)
    //    {
    //        return new Id<T>(value);
    //    }

    //    public static Id<T> Parse(string str)
    //    {
    //        var converter = TypeDescriptor.GetConverter(typeof(T));
    //        var id= (T)converter.ConvertFrom(str);
    //        return new Id<T>(id);
    //    }

    //    public override string ToString() => Value.ToString();
    //}
}
