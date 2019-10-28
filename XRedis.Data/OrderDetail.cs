using AutoMapper;
using XRedis.Core;

namespace XRedis.Data
{
    public class OrderDetail : Record
    {
        public long OrderDetailID { get; set; }

        public virtual long OrderID { get; set; }

        public virtual long ProductID { get; set; }

        public virtual int Quantity { get; set; }

        public virtual decimal ItemCost { get; set; }

        public virtual decimal Discount { get; set; }

        public virtual decimal Total { get; set; }

        [IgnoreMap]
        public virtual Order Order { get; set; }

        [IgnoreMap]
        public virtual Product Product { get; set; }
    }
}
