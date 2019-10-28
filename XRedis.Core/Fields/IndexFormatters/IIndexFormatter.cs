namespace XRedis.Core.Fields.IndexFormatters
{
    public interface IIndexFormatter
    {
        string Format(object val);
    }
}