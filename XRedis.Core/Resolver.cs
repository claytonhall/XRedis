using SimpleInjector;
using System;

namespace XRedis.Core
{
    public interface IResolver
    {
        T GetInstance<T>()
            where T : class;

        object GetInstance(Type type);
        
    }


    public class Resolver : IResolver
    {
        public static Resolver Instance { get; private set; }

        private static Container _container;

        public Resolver(Container container)
        {
            _container = container;
            Instance = this;
        }

        public T GetInstance<T>()
            where T : class
        {
            return _container.GetInstance<T>();
        }

        public object GetInstance(Type type)
        {
            return _container.GetInstance(type);
        }
    }
}
