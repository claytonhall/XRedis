using Castle.DynamicProxy;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XRedis.Core;
using XRedis.Data;
using System.Transactions;

namespace XRedis.Demo
{
    class Program
    {
        static TestContext _context;

        static void Main(string[] args)
        {
            _context = new TestContext(new XRedisConnection("localhost:6379,allowadmin=true"));
            _context.FlushDb();

            for (var i = 0; i < 1; i++)
            {
                _context.Companies.New(c =>
                {
                    c.Name = $"{i} company";
                    c.TaxId = i+100000;
                    c.CreatedDate = DateTime.Now.AddDays(-i);
                });
            }

            using (var scope = new TransactionScope())
            {
                var company = _context.Companies.New(c => { c.Name = "A company"; });
                company.Associates.New(a =>
                {
                    a.FirstName = "Clayton";
                    a.LastName = "Hall";
                });
                company.Associates.New(a =>
                {
                    a.FirstName = "Mandie";
                    a.LastName = "Hall";
                });
                company.Associates.New(a =>
                {
                    a.FirstName = "Carson";
                    a.LastName = "Hall";
                });
                company.Associates.New(a =>
                {
                    a.FirstName = "Morgan";
                    a.LastName = "Hall";
                });
                company.Associates.New(a =>
                {
                    a.FirstName = "Madison";
                    a.LastName = "Hall";
                });
                company.Associates.New(a =>
                {
                    a.FirstName = "Mallory";
                    a.LastName = "Hall";
                });
                company.Associates.New(a =>
                {
                    a.FirstName = "Cohen";
                    a.LastName = "Hall";
                });


                var company2 = _context.Companies.New(c => { c.Name = "B company"; });
                company2.Associates.New(a =>
                {
                    a.FirstName = "James";
                    a.LastName = "Allen";
                });
                company2.Associates.New(a =>
                {
                    a.FirstName = "Laura";
                    a.LastName = "Allen";
                });
                company2.Associates.New(a =>
                {
                    a.FirstName = "Riley";
                    a.LastName = "Allen";
                });
                company2.Associates.New(a =>
                {
                    a.FirstName = "Jayden";
                    a.LastName = "Allen";
                });
                company2.Associates.New(a =>
                {
                    a.FirstName = "Gage";
                    a.LastName = "Allen";
                });
                company2.Associates.New(a =>
                {
                    a.FirstName = "Katie";
                    a.LastName = "Allen";
                });
                company2.Associates.New(a =>
                {
                    a.FirstName = "Sophie";
                    a.LastName = "Allen";
                });
                company2.Associates.New(a =>
                {
                    a.FirstName = "Reid";
                    a.LastName = "Allen";
                });

                _context.Associates.OrderBy("FirstName");
                var record = _context.Associates.Seek("M");
                Console.WriteLine(record.FirstName);

                var recordSet = _context.Companies;
                foreach (var company1 in recordSet)
                {
                    Console.WriteLine($"{company1.Name}");
                    var associates = company1.Associates.OrderBy("FirstName");
                    associates.Seek("COH");
                    foreach (var associate in associates)
                    {
                        Console.WriteLine($"{associate.Company.Name} {associate.FirstName} {associate.LastName}");
                    }

                    Console.WriteLine("Now by LastName");
                    foreach (var associate in company1.Associates.OrderBy("LastName"))
                    {
                        Console.WriteLine($"{associate.Company.Name} {associate.FirstName} {associate.LastName}");
                    }
                }

                scope.Complete();
            }

        }
    }
}
