﻿using XRedis.Core;

namespace XRedis.Data
{


    public class TestContext : XRedisContext
    {
        public TestContext(IXRedisConnection xredisConnection) : base(xredisConnection)
        {
            this.Companies = this.CreateRecordSet<Company, long>();
            this.Associates = this.CreateRecordSet<Associate, long>();
            //this.Orders = this.CreateRecordSet<Order>();
            //this.OrderDetails = this.CreateRecordSet<OrderDetail>();
            //this.Products = this.CreateRecordSet<Product>();
            //this.ShippingMethods = this.CreateRecordSet<ShippingMethod>();
            //this.ShippingRates = this.CreateRecordSet<ShippingRate>();
        }

        public virtual IRecordSet<Company, long> Companies { get; set; }

        public virtual IRecordSet<Associate, long> Associates { get; set; }
        //public virtual IRecordSet<Order> Orders { get; set; }
        //public virtual IRecordSet<OrderDetail> OrderDetails { get; set; }
        //public virtual IRecordSet<Product> Products { get; set; }
        //public virtual IRecordSet<ShippingMethod> ShippingMethods { get; set; }
        //public virtual IRecordSet<ShippingRate> ShippingRates { get; set; }
    }
}
