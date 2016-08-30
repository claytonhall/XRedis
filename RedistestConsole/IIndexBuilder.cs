using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public interface IIndexBuilder<T>
    {
        IEnumerable<Func<T, string>> Expressions { get; set; }
        string Name { get; set; }

        IIndexBuilder<T> On(IEnumerable<Func<T, string>> expression);
        IIndexBuilder<T> Tag(string tagName);
    }

    public static class IndexExtensions
    {
        //public static void Index<T>(this Table<T> table, IIndexBuilder<T> builder)
        //{
        //}
    } 
}
