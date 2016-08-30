using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public interface IUseBuilder<T>
    {
        IUseBuilder<T> Use();

        IUseBuilder<T> In(int workArea);

        IUseBuilder<T> In(string alias);

        IUseBuilder<T> Tag(string tagName);

        IUseBuilder<T> Alias(string alias);

        Table<T> Invoke();
    }
}
