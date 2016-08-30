using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    interface IWorkArea
    {
        string Alias { get; set; }
        bool Found { get; }
        bool Eof { get; }
        bool Bof { get; }
    }

    interface IWorkArea<T> : ITable<T>, IWorkArea
    {
        Index<T> SelectedIndex { get; set; }
        T CurrentRecord { get; }

        void Skip();

        void Skip(int i);

      
    }
}
