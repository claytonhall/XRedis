using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public class Table<T>
    {
        public List<Index<T>> Indexes = new List<Index<T>>();
        Index<T> _selectedIndex { get; set; }
        public int _workArea { get; private set; }

        T _currentRecord;
        private string _alias;

        public Table()
        {
            
        }


        public void SetOrderTo(string tagName)
        {
            _selectedIndex = this.Indexes.Single(i => i.Name.ToUpper() == tagName.ToUpper());
        }

        public void Seek(string indexKey)
        {
            if (_selectedIndex == null)
            {
                throw new ApplicationException("No Index Order Set");
            }
            else
            {
                _selectedIndex.Seek(indexKey);
            }
        }

        public void ScanWhile(Func<T, bool> expression, Action<T> action)
        {
            while(expression(_currentRecord))
            {
                action(_currentRecord);
                Skip();
            }
        }

        public void Skip()
        {
            Skip(1);
        }

        public void Skip(int step)
        {
            if (_selectedIndex == null)
            {
            }
            else
            {
                _selectedIndex.Skip(step);
            }
        }

        //UseBuilder!
        public IUseBuilder<T> In(int workArea)
        {
            this._workArea = workArea;
            return this;
        }

        public IUseBuilder<T> In(string alias)
        {
            this._alias = alias;
            return this;
        }

        public IUseBuilder<T> Tag(string tagName)
        {
            this._selectedIndex = this.Indexes.Single(t => t.Name == tagName);
            return this;
        }

        public IUseBuilder<T> Alias(string alias)
        {
            this._alias = alias;
            return this;
        }

        public IUseBuilder<T> Use()
        {
            return this;
        }

        public Table<T> Invoke()
        {
            Table<T> table = new Table<T>();
        }
    }
}
