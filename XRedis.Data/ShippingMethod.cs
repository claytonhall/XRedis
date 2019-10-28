using XRedis.Core;
using System;

namespace XRedis.Data
{
    public class ShippingMethod : Record
    {
        public virtual long ShippingMethodID { get; set; }

        public virtual String Name { get; set; }

    }
}
