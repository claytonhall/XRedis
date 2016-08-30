using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public interface IWorkAreaBuilder<T>
    {
        IWorkAreaBuilder<T> Use();
        IWorkAreaBuilder<T> In(int workAreaNumber);
        IWorkAreaBuilder<T> In(string workAreaAlias);
        IWorkAreaBuilder<T> Tag(string tagName);
        IWorkAreaBuilder<T> Alias(string tagName);
    }

    public static class WorkAreaExtensions
    {
        public static WorkArea<T> Use<T>(this XContext context)
            where T : new()
        {
            return context.Use<T>();
        }
    }
}
