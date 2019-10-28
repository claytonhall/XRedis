namespace XRedis.Core
{
    public interface IResourceManagerFactory
    {
        XResourceManager GetInstance();
    }
}