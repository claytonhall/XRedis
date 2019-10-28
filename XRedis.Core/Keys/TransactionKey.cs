//using System.Runtime.Remoting.Messaging;

namespace XRedis.Core.Keys
{
    public class TransactionKey
    {
        public long TransactionId { get; set; }

        public TransactionKey(long transactionId)
        {
            TransactionId = transactionId;
        }

        public override string ToString()
        {
            return $"{Keys.Transaction}:{TransactionId}";
        }
    }
}
