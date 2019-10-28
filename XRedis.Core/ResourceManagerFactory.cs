using System;
using System.Collections.Generic;
using System.Transactions;
using StackExchange.Redis;
using XRedis.Core.Interception;

namespace XRedis.Core
{
    public class ResourceManagerFactory : IResourceManagerFactory
    {
        private readonly Dictionary<string, XResourceManager> _resourceManagers = new Dictionary<string, XResourceManager>();
        private readonly IResolver _resolver;
        public ResourceManagerFactory(IResolver resolver)
        {
            _resolver = resolver;
        }

        public XResourceManager GetInstance()
        {
            var identifier = Transaction.Current.TransactionInformation.LocalIdentifier;
            if (!_resourceManagers.ContainsKey(identifier))
            {
                var rm = new XResourceManager(_resolver.GetInstance<IConnectionMultiplexer>(), _resolver.GetInstance<IProxyFactory>(), _resolver.GetInstance<ILogger>(), _resolver.GetInstance<ISchemaHelper>());
                rm.TransactionId = identifier;
                _resourceManagers.Add(identifier, rm);

                Transaction.Current.TransactionCompleted += (sender, args) => _resourceManagers.Remove(identifier);
                rm.StartTransaction();
            }
            return _resourceManagers[Transaction.Current.TransactionInformation.LocalIdentifier];
        }
    }
}