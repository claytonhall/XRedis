using AutoMapper;
using XRedis.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using XRedis.Core.Fields;
using XRedis.Core.Fields.IndexFormatters;
using Index = XRedis.Core.Fields.Indexes.Index;

namespace XRedis.Data
{
    public class Company : IRecord<long>
    {
        public virtual long CompanyId { get; set; }

        [Index("UPPERNAME", typeof(Company), typeof(UpperIndexFormatter))]
        public virtual string Name { get; set; }

        [Index("TaxID", typeof(Company))]
        public virtual int TaxId { get; set; }

        [Index("CreatedDate", typeof(Company))]
        public virtual DateTime CreatedDate { get; set; }

        public virtual IRecordSet<Associate, long, Company, long> Associates { get; set; }
    } 


    public class Associate : IRecord<long>
    {
        [PrimaryKey(typeof(Associate))]
        public virtual long AssociateId { get; set; }

        public virtual long CompanyId { get; set; }

        [Index("FirstName", typeof(Associate), typeof(UpperIndexFormatter))]
        public virtual string FirstName { get; set; }

        [Index("LastName", typeof(Associate), typeof(UpperIndexFormatter))]
        public virtual string LastName { get; set; }

        public virtual Company Company { get; set; }
    }
}
