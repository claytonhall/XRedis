using XRedis.Core;
using AutoMapper;

namespace XRedis.Data
{
    public class Product : Record
    {
        public virtual long ProductID { get;set;}

        public virtual string Name { get; set; }

        public virtual decimal Price { get; set; }

        public virtual decimal Weight { get; set; }

        [IgnoreMap]
        public virtual IRecordSet<OrderDetail> OrderDetails { get; set; }
    }
}
