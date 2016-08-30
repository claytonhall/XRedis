using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public interface ITable
    {
        string TableName { get; set; }
    }

    public interface ITable<T> : ITable
    {
        List<Index<T>> Indexes { get; set; }
    }
}
