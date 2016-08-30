using RedistestConsole.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public static class XContext
    {
        public static Table<Person> Persons = new Table<Person>("Persons");

        static XContext()
        {
            Persons.Index().On((Person p) => { return p.LastName.ToUpper(); }).Tag("LastName");
            Persons.Index().On((Person p) => { return p.Social; }).Tag("SSN");
        }
    }
}
