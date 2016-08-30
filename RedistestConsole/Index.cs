using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public class Index<T> : IIndex
    {
        List<Func<T, string>> _expressions = new List<Func<T, string>>();

        public string Tag { get; set; }

        public List<Func<T, string>> Expressions { get { return _expressions; } set { _expressions = value; } }
    }
}
