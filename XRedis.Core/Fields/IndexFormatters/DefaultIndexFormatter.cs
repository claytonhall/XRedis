using System;

namespace XRedis.Core.Fields.IndexFormatters
{
    public class DefaultIndexFormatter : IIndexFormatter
    {
        public string Format(object val)
        {
            string retVal = null;
            if (val is long)
            {
                retVal = val.ToString().PadLeft(long.MaxValue.ToString().Length, '0');
            }
            else if (val is short)
            {
                retVal = val.ToString().PadLeft(short.MaxValue.ToString().Length, '0');
            }
            else if (val is int)
            {
                retVal = val.ToString().PadLeft(int.MaxValue.ToString().Length, '0');
            }
            else if (val is decimal)
            {
                retVal = val.ToString().PadLeft(decimal.MaxValue.ToString().Length, '0');
            }
            else if (val is double)
            {
                retVal = val.ToString().PadLeft(double.MaxValue.ToString().Length, '0');
            }
            else if (val is float)
            {
                retVal = val.ToString().PadLeft(float.MaxValue.ToString().Length, '0');
            }
            else if (val is byte)
            {
                retVal = val.ToString().PadLeft(byte.MaxValue.ToString().Length, '0');
            }
            else if (val is DateTime dateTime)
            {
                retVal = dateTime.ToString("u");
            }
            else
            {
                retVal = val?.ToString();
            }

            return retVal;
        }
    }




}