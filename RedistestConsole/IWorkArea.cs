using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public interface IWorkArea
    {
        Type Type { get; }

        string Alias { get; set; }
    }
}
