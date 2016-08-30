using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public class WorkArea<T> : IWorkArea<T>
        where T : new()
    {
        Table<T> _table;

        public WorkArea()
        {
        }

        public WorkArea(Table<T> table)
        {
            _table = table;
        }

        public Table<T> Table
        {
            get
            {
                return _table;
            }
        }

        public string Alias { get; set; }
        public Index<T> SelectedIndex { get; set; }
        public T CurrentRecord { get; internal set; }
        public bool Found { get; internal set; }

        public bool Eof { get; internal set; }

        public bool Bof { get; internal set; }

        public List<Index<T>> Indexes
        {
            get
            {
                return _table.Indexes;
            }

            set
            {
                _table.Indexes = value;
            }
        }

        public string TableName
        {
            get
            {
                return _table.TableName;
            }

            set
            {
                _table.TableName = value;
            }
        }

        internal T ScatterInternal()
        {
            var serialzied = Newtonsoft.Json.JsonConvert.SerializeObject(this.CurrentRecord);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serialzied);
        }

        public void Skip()
        {
            this.Skip(1);
        }

        public void Skip(int i)
        {
            if(this.SelectedIndex != null)
            {

            }
        }
    }
}
