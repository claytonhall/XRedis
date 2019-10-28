using AutoMapper;
using XRedis.Core;
using System;
using System.Linq;
using XRedis.Core.Fields;

namespace XRedis.Data
{
    public class Order : IRecord
    {
        public long OrderID { get; set; }

        [ForeignKey(typeof(Associate), typeof(Order))]
        public virtual long AssociateID { get; set; }

        public virtual long ShippingMethodID { get; set; }

        public virtual DateTime? OrderDate { get; set; }

        public virtual DateTime? ShipDate { get; set; }

        public virtual decimal ShippingFee { get; set; }

        public virtual decimal Tax { get; set; }

        public virtual DateTime? DeliveredDate { get; set; }

        public virtual string ShippingAddress1 { get; set; }

        public virtual string ShippingAddress2 { get; set; }

        public virtual string ShippingCity { get; set; }

        public virtual string ShippingState { get; set; }

        public virtual string ShippingZipcode { get; set; }

        //public decimal Total
        //{
        //    get
        //    {
        //        return this.OrderDetails.Select(d => d.Total).Sum();
        //    }
        //}

        [IgnoreMapAttribute]
        public virtual Associate Associate { get; set; }

        //[IgnoreMapAttribute]
        //public virtual ShippingMethod ShippingMethod { get; set; }

        //[IgnoreMapAttribute]
        //public virtual IRecordSet<OrderDetail> OrderDetails { get; set; }
    }
}
