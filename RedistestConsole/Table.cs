using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public class Table<T> : ITable<T>
    {
        List<Index<T>> _indexes = new List<Index<T>>();

        public string TableName { get; set; }

        public Table() { }

        public Table(string tableName)
        {
            this.TableName = tableName;
        }
        
        public List<Index<T>> Indexes { get { return _indexes; } set { _indexes = value; } }
    }
}
