using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XRedis.Core.Extensions
{
    public static class IndexExtensions
    {
        public static string FormatIndexValues(this object[] indexVals)
        {
            return string.Join("+", FormatIndexValue(indexVals));
        }

        public static string FormatIndexValue(this object indexVal)
        {
            string retVal = null;
            if (indexVal is long)
            {
                retVal = indexVal.ToString().PadLeft(long.MaxValue.ToString().Length, '0');
            }
            else if (indexVal is short)
            {
                retVal = indexVal.ToString().PadLeft(short.MaxValue.ToString().Length, '0');
            }
            else if (indexVal is int)
            {
                retVal = indexVal.ToString().PadLeft(int.MaxValue.ToString().Length, '0');
            }
            else if (indexVal is decimal)
            {
                retVal = indexVal.ToString().PadLeft(decimal.MaxValue.ToString().Length, '0');
            }
            else if (indexVal is float)
            {
                retVal = indexVal.ToString().PadLeft(float.MaxValue.ToString().Length, '0');
            }
            else
            {
                retVal = indexVal.ToString();
            }
            return retVal;
        }

        //public static string FormatIndexMin(this object indexVal)
        //{
        //    return $"{FormatIndexValue(indexVal)}{KeyElement.SplitChar}";
        //}

        public static string FormatIndexMax(this object indexVal)
        {
            return FormatIndexValue(indexVal).PadRight(50,'~');
        }
    }
}
