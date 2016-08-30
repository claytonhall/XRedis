using RedistestConsole.Models;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static RedistestConsole.Commands;
using static RedistestConsole.XContext;

namespace RedistestConsole
{
    public static class Commands
    {
        static ISerializer serializer = new NewtonsoftSerializer();
        static ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");
        static ICacheClient cacheClient = new StackExchangeRedisCacheClient(connectionMultiplexer, serializer);

        public static Table<T> Use<T>(Table<T> table)
            where T : class, new()
        {
            var newTable = new Table<T>();
            newTable.TableName = table.TableName;
            foreach (var index in table.Indexes)
            {
                newTable.Indexes.Add(index);
            }

            return newTable;
        }

        public static Table<T> In<T>(this Table<T> table, string workAreaName)
        {
            table.Alias = workAreaName;
            return table;
        }

        public static Table<T> Tag<T>(this Table<T> table, string tagName)
        {
            table.SelectedIndex = table.Indexes.Single(i => i.Tag == tagName);
            return table;
        }

        public static Index<T> Index<T>(this Table<T> table)
        {
            Index<T> index = new Index<T>();
            table.Indexes.Add(index);
            return index;
            //table.SelectedIndex = table.Indexes.Single(i => i.Tag == tagName);
            //return table;
        }

        public static Index<T> On<T>(this Index<T> index, Func<T, string> expression)
        {
            index.Expressions.Add(expression);
            return index;
            //table.SelectedIndex = table.Indexes.Single(i => i.Tag == tagName);
            //return table;
        }

        public static Index<T> Tag<T>(this Index<T> index, string tagName)
        {
            index.Tag = tagName;
            return index;
        }

        public static void Select<T>(Table<T> table, Action<Table<T>> action)
        {
            action.Invoke(table);
        }


        public static void Seek<T>(this Table<T> table, string expression)
        {
            var db = connectionMultiplexer.GetDatabase();
            var values = db.SortedSetRangeByValue( String.Format("{0}:{1}:{2}", table.TableName, table.SelectedIndex.Tag, expression ));
            if (values.Length > 0)
            {
                var pieces = values[0].ToString().Split(':');
                var key = pieces[pieces.Length - 1];

                table.Found = true;
                table.CurrentRecord = cacheClient.Get<T>(String.Format("{0}:{1}", table.TableName, key));
            }
            else
            {
                table.Found = false;
            }
        }

        public static void AppendBlank<T>(this Table<T> table)
            where T : new()
        {
            var record = new T();
            var db = connectionMultiplexer.GetDatabase();
            var id = table.IncrementKey();
            record.SetID(id);
            db.HashSet(String.Format("{0}:{1}", table.TableName, id), record.ToHashEntries());

            table.CurrentRecord = record;
        }

        public static void Gather<T>(this Table<T> table, T record)
            where T : new()
        {
            var db = connectionMultiplexer.GetDatabase();

            long id = table.CurrentRecord.GetID();
            if (id == 0)
            {
                id = table.IncrementKey();
                record.SetID(id);
            }

            db.HashSet(String.Format("{0}:{1}", table.TableName, id), record.ToHashEntries());
            table.UpdateIndex(record);
        }


        private static long IncrementKey<T>(this Table<T> table)
        {
            var db = connectionMultiplexer.GetDatabase();
            return db.StringIncrement(String.Format("Table:{0}", table.TableName));
        }


        private static void UpdateIndex<T>(this Table<T> table, T record)
        {
            var db = connectionMultiplexer.GetDatabase();
            foreach (var index in table.Indexes)
            {
                string value = "";
                foreach (var exp in index.Expressions)
                {
                    //TODO: think about concatenation... padding...
                    if (!String.IsNullOrWhiteSpace(value))
                    {
                        value += ":";
                    }
                    value += exp.Invoke(record);
                }
                value += ":" + record.GetID().ToString();
                string indexKey = String.Format("{0}:{1}", table.TableName, index.Tag);
                db.SortedSetAdd(indexKey, value, 0);
            }
        }




        public static T Scatter<T>(this Table<T> table)
        {
            return table.ScatterInternal();
        }

             

        static HashEntry[] ToHashEntries<T>(this T record)
        {
            var props = record.GetType().GetProperties();
            List<HashEntry> hashEntries = new List<HashEntry>(props.Length);
            foreach (var propInfo in props)
            {
                hashEntries.Add(new HashEntry(propInfo.Name, propInfo.GetValue(record) == null ? "" : propInfo.GetValue(record).ToString()));
            }
            return hashEntries.ToArray();
        }

        static void SetID<T>(this T record, long id)
        {
            var property = record.GetIDProperty();

            if (property != null)
            {
                property.SetValue(record, id);
            }
            else
            {
                throw new ApplicationException(String.Format("Could not find ID for {0}", record.GetType().Name));
            }
        }

        static long GetID<T>(this T record)
        {
            var property = record.GetIDProperty();

            if (property != null)
            {
                return (long)property.GetValue(record);
            }
            else
            {
                throw new ApplicationException(String.Format("Could not find ID for {0}", record.GetType().Name));
            }
        }

        static PropertyInfo GetIDProperty<T>(this T record)
        {
            PropertyInfo property = null;
            var bindingFlags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

            List<string> possibleKeys = new List<string>(new string[] { record.GetType().Name + "ID", record.GetType().Name + "_ID", "ID" });
            foreach (var keyName in possibleKeys)
            {
                property = record.GetType().GetProperty(keyName, bindingFlags);
                if (property != null)
                {
                    break;
                }
            }
            return property;
        }


    }

    public static class XContext
    {
        public static Table<Person> Persons = new Table<Person>("Persons");

        static XContext()
        {
            Func<Person, string> func = new Func<Person, string>((Person p) => { return p.LastName.ToUpper(); });
            Persons.Index().On(func).Tag("LastName");
        }
    }

    public class Table<T> : ITable<T>
    {
        List<Index<T>> _indexes = new List<Index<T>>();

        public string TableName { get; set; }

        public Table() { }

        public Table(string tableName)
        {
            this.TableName = tableName;
        }

        public string Alias { get; set; }

        public List<Index<T>> Indexes { get { return _indexes; } set { _indexes = value; } }
        public Index<T> SelectedIndex { get; set; }
        public T CurrentRecord { get; internal set; }
        public bool Found { get; internal set; }


        internal T ScatterInternal()
        {
            return (T)this.MemberwiseClone();
        }

    }

    public interface ITable
    {
        string Alias { get; set; }
        string TableName { get; set; }
    }

    public interface ITable<T> : ITable
    {
        List<Index<T>> Indexes { get; set; }
        Index<T> SelectedIndex { get; set; }
        T CurrentRecord { get; }
    }

    public class Index<T> : IIndex
    {
        List<Func<T, string>> _expressions = new List<Func<T, string>>();

        public string Tag { get; set; }

        public List<Func<T, string>> Expressions { get { return _expressions; } set { _expressions = value; } }
    }

    public interface IIndex
    {
        string Tag { get; set; }

        //List<Func<object, string>> Expressions { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadAllLines(@"c:\temp\testnames.txt");


            var persons = Use(Persons).In("myAlias");

            foreach (var line in lines)
            {
                var fields = line.Split(',');

                var person = new Person();
                person.FirstName = fields[0];
                person.LastName = fields[1];
                person.Social = fields[2];


                persons.AppendBlank();
                persons.Gather(person);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();


            //Select(persons, p=>
            //{
            //    p.Seek("Hall");
            //    if (p.Found)
            //    {
            //        Console.WriteLine("dsfsadf");
            //    }
            //});

            //var person = Use(Persons.In(0).Tag("LastName"));
            //var person = Use<Person>();
            //person.SetOrderTo("LastName");

            //person.Seek("Hall");
            ////person.Seek()

            //var serializer = new NewtonsoftSerializer();
            //var connectionMultiplexer = ConnectionMultiplexer.Connect("localhost:6379");
            //var cacheClient = new StackExchangeRedisCacheClient(connectionMultiplexer, serializer);

            //var person = new Person()
            //{
            //    ID = 1,
            //    FirstName = "Clayton",
            //    LastName = "Hall",
            //    Social = "333-33-3333"
            //};

            //cacheClient.Add(person.ID.ToString(), person);

            //var personReturend = cacheClient.Get<Person>(person.ID.ToString());

            //Console.WriteLine("{0} {1} {2} {3}", personReturend.ID, personReturend.FirstName, personReturend.LastName, personReturend.Social);

            //var db = connectionMultiplexer.GetDatabase();
            //var indexValue = String.Format(person.FirstName + ":" + person.ID.ToString());
            ////var x = new SortedSetEntry(, 0);
            //db.SortedSetAdd("PersonFirstNameIndex", indexValue, 0, CommandFlags.FireAndForget);

            ////db.SortedSetRangeByScore("PersonFirstNameIndex", );

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();


            //var context = new XContext("localhost:6379");
            //context.Select(0);
            //context.Use<Person>()
        }
    }

    
    








}
