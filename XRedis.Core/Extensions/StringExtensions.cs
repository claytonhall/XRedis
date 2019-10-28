using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XRedis.Core.Extensions
{
    public static class StringExtensions
    {
        public static string NextGreaterValue(this string s, int incrementValue = 1)
        {
            var lastChar = s.ToCharArray().Last();
            if (String.IsNullOrEmpty(s))
            {
                return char.MinValue.ToString();
            }
            else if (lastChar == char.MaxValue)
            {
                return s + char.MinValue;
            }

            return s[0..^1] + (char) (s[^1] + 1);
            
        }
    }
}
