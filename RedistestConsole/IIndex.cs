using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public interface IIndex
    {
        string Tag { get; set; }

        //List<Func<object, string>> Expressions { get; set; }
    }
}
