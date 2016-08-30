using RedistestConsole.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public class TestContext : XContext
    {
        public TestContext(string connectionString) : base(connectionString)
        {
        }

        public override void CreateIndexes()
        {
            Persons
                .CreateIndex()
                .On(new Func<Person, string>[] {
                    ((p) => p.LastName ),
                    ((p) => p.FirstName )})
                .Tag("Name");
            Persons
                .CreateIndex()
                .On(new Func<Person, string>[] {
                    ((p)=>p.Social)})
                .Tag("SSN");
            base.CreateIndexes();
        }


        Table<Person> Persons { get; set; }



    }
}
