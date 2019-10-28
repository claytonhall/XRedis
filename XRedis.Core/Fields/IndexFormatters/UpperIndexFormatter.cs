namespace XRedis.Core.Fields.IndexFormatters
{
    public class UpperIndexFormatter : IIndexFormatter
    {
        public string Format(object val)
        {
            return val?.ToString().ToUpper();
        }
    }
}