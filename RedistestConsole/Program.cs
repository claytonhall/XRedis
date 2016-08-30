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

        public static WorkArea<T> Use<T>(Table<T> table)
            where T : class, new()
        {
            return new WorkArea<T>(table);
        }

        public static WorkArea<T> In<T>(this WorkArea<T> workArea, string workAreaName)
            where T : new()
        {
            workArea.Alias = workAreaName;
            return workArea;
        }

        public static WorkArea<T> Tag<T>(this WorkArea<T> workArea, string tagName)
            where T : new()
        {
            workArea.SelectedIndex = workArea.Indexes.Single(i => i.Tag == tagName);
            return workArea;
        }

        public static WorkArea<T> SetOrderTo<T>(this WorkArea<T> workArea, string tagName)
            where T : new()
        {
            workArea.SelectedIndex = workArea.Indexes.Single(i => i.Tag == tagName);
            return workArea;
        }

        public static Index<T> Index<T>(this Table<T> table)
        {
            Index<T> index = new Index<T>();
            table.Indexes.Add(index);
            return index;
        }

        public static Index<T> On<T>(this Index<T> index, Func<T, string> expression)
        {
            index.Expressions.Add(expression);
            return index;
        }

        public static Index<T> Tag<T>(this Index<T> index, string tagName)
        {
            index.Tag = tagName;
            return index;
        }

        public static void Select<T>(WorkArea<T> workArea, Action<WorkArea<T>> action)
            where T : new()
        {
            action.Invoke(workArea);
        }


        public static void Seek<T>(this WorkArea<T> workArea, string expression)
            where T : new()
        {
            var db = connectionMultiplexer.GetDatabase();
            RedisValue[] values = db.SortedSetRangeByValue( String.Format("{0}:{1}", workArea.TableName, workArea.SelectedIndex.Tag ), min:expression, take:1);
            if (values.Length > 0)
            {
                var pieces = values[0].ToString().Split(':');
                var key = pieces[pieces.Length - 1];

                workArea.Found = true;
                workArea.CurrentRecord = db.HashGetAll(String.Format("{0}:{1}", workArea.TableName, key)).ToRecord<T>();
            }
            else
            {
                workArea.Found = false;
                workArea.CurrentRecord = new T();
            }
        }

        public static void AppendBlank<T>(this WorkArea<T> workArea)
            where T : new()
        {
            var record = new T();
            var db = connectionMultiplexer.GetDatabase();
            var id = workArea.Table.IncrementKey();
            record.SetID(id);
            db.HashSet(String.Format("{0}:{1}", workArea.TableName, id), record.ToHashEntries());

            workArea.CurrentRecord = record;
        }

        public static void Gather<T>(this WorkArea<T> workArea, T record)
            where T : new()
        {
            var db = connectionMultiplexer.GetDatabase();

            long id = workArea.CurrentRecord.GetID();
            if (id == 0)
            {
                id = workArea.Table.IncrementKey();
            }
            record.SetID(id);

            db.HashSet(String.Format("{0}:{1}", workArea.TableName, id), record.ToHashEntries());
            workArea.Table.UpdateIndex(record);
        }


        private static long IncrementKey<T>(this Table<T> table)
        {
            var db = connectionMultiplexer.GetDatabase();
            return db.StringIncrement(String.Format("Table:{0}", table.TableName));
        }


        private static void UpdateIndex<T>(this Table<T> table, T record)
        {
            long recordID = record.GetID();
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
                value += ":" + recordID.ToString();

                string indexKey = String.Format("{0}:{1}", table.TableName, index.Tag);
                string indexToIndexKey = String.Format("{0}:{1}:{2}", table.TableName, index.Tag, recordID);

                string previousIndexValue = db.StringGet(indexToIndexKey);

                if (previousIndexValue != null && previousIndexValue != value)
                {
                    db.SortedSetRemove(indexKey, previousIndexValue);
                }

                db.StringSet(indexToIndexKey, value);
                db.SortedSetAdd(indexKey, value, 0);
            }
        }


        public static void Scan<T>(this WorkArea<T> workArea, Action<WorkArea<T>> action)
            where T : new()
        {
            action.Invoke(workArea);
        }

        public static ScanBuilder<T> Scan<T>(this WorkArea<T> workArea)
            where T : new()
        {
            return new ScanBuilder<T>(workArea);
        }

        public static void For<T>(this ScanBuilder<T> builder, 
            Func<WorkArea<T>, bool> func,
            Action<WorkArea<T>> workArea)
            where T : new()
        {
            while (!workArea.Eof())
            {
                if (func(builder.WorkArea))
                {
                    builder.Invoke();
                }
                workArea.Skip();
            }
        }


        public static T Scatter<T>(this WorkArea<T> workArea)
            where T : new()
        {
            return workArea.ScatterInternal();
        }

        static T ToRecord<T>(this HashEntry[] hashEntries)
            where T : new()
        {
            var record = new T();
            foreach (var hashEntry in hashEntries)
            {
                var propertyInfo = typeof(T).GetProperty(hashEntry.Name);
                propertyInfo.SetValue(record, Convert.ChangeType(hashEntry.Value, propertyInfo.PropertyType));
            }
            return record;
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


        public static string CurrentRecordKey<T>(this WorkArea<T> workArea)
            where T : new()
        {
            long id = workArea.CurrentRecord.GetID();
            return String.Format("{0}:{1}", workArea.TableName, id);
        }

        public static string NextRecordKey<T>(this WorkArea<T> workArea)
            where T : new()
        {
            long id = workArea.CurrentRecord.GetID();
            id++;
            return String.Format("{0}:{1}", workArea.TableName, id);
        }


        public static void Skip<T>(this WorkArea<T> workArea)
            where T : new()
        {
            var db = connectionMultiplexer.GetDatabase();
            if (workArea.SelectedIndex == null)
            {
                var propertyName = workArea.CurrentRecord.GetIDProperty().Name;
                var key = workArea.NextRecordKey();
                var maxID = db.StringIncrement(String.Format("Table:{0}", workArea.TableName));
                bool eof = true;
                while (workArea.CurrentRecord.GetID() <= maxID)
                {
                    workArea.NextRecordKey();
                    if (db.HashExists(key, propertyName))
                    {
                        eof = false;
                        break;
                    }
                    else
                    {
                        key = workArea.NextRecordKey();
                        //am i eof?
                    }
                }
                if (!eof)
                {
                    workArea.CurrentRecord = db.HashGetAll(key).ToRecord<T>();
                }
                workArea.Eof = eof;
            }
            else
            {
            }
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            var lines = File.ReadAllLines(@"c:\temp\testdata.txt");


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

            persons.SetOrderTo("SSN");

            persons.Seek("33");
            if (persons.Found)
            {
                var p = persons.Scatter();
                p.LastName = "Test";
                persons.Gather(p);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
