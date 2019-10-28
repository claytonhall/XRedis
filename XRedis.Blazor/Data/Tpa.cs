using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XRedis.Core;
using XRedis.Core.Fields.Indexes;

namespace XRedis.Blazor.Data
{
    public class DemoContext : XRedisContext
    {
        public DemoContext(IXRedisConnection connection) : base(connection)
        {
            Tpas = CreateRecordSet<Tpa, long>();
            Employers = CreateRecordSet<Employer, long>();
        }

        public virtual IRecordSet<Tpa, long> Tpas { get; set; }
        public virtual IRecordSet<Employer, long> Employers { get; set; }

    }

    public class Tpa : IRecord<long>
    {
        public virtual long TpaId { get; set; }

        [Index("Name", typeof(Tpa))]
        public virtual string Name { get; set; }

        public virtual IRecordSet<Employer, Tpa> Employers { get; set; }
    }

    public class Employer : IRecord<long>
    {
        public virtual long EmployerId { get; set; }

        public virtual string Name { get; set; }

        public virtual Tpa Tpa { get; set; }
    }
}
