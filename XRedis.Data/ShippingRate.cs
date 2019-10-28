using AutoMapper;
using XRedis.Core;

namespace XRedis.Data
{
    public class ShippingRate : Record
    {
        public virtual long ShippingRateID { get; set; }

        public virtual long ShippingMethodID { get; set; }

        public virtual decimal FlateRate { get; set; }

        public virtual decimal MaxWeight { get; set; }

        public virtual decimal RatePerUnit { get; set; }

        [IgnoreMap]
        public virtual ShippingMethod ShippingMethod { get; set; }

        [IgnoreMap]
        public virtual IRecordSet<Order> Orders { get; set; }

    }
}
