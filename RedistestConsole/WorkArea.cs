using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public class WorkArea<T> : IWorkAreaBuilder<T>
        where T : new()
    {
        Table<T> _table = new Table<T>();
        Table<T> Table { get { return _table; } }
        Index<T> SelectedIndex { get; set; }

        public IWorkAreaBuilder<T> Alias(string aliasName)
        {
            throw new NotImplementedException();
        }

        public IWorkAreaBuilder<T> In(string aliasName)
        {
            XContext.Instance.Select(aliasName);
            return this;
        }

        public IWorkAreaBuilder<T> In(int workAreaNumber)
        {
            XContext.Instance.Select(workAreaNumber);
            return this;
        }

        public IWorkAreaBuilder<T> Tag(string tagName)
        {
            SelectedIndex = this.Table.Indexes.Single(i => i.Name.ToUpper() == tagName.ToUpper());
            return this;
        }

        public IWorkAreaBuilder<T> Use()
        {
            return new WorkArea<T>();    
        }
    }
}
