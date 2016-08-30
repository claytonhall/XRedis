using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedistestConsole
{
    public class Index<T> : IIndexBuilder<T>
    {
        public Index()
        {
        }

        public Index(IIndexBuilder<T> builder)
        {
            this.Name = builder.Name;
            this.Expressions = builder.Expressions;
        }
        
        public string Name { get; set; }

        public IEnumerable<Func<T, string>> Expressions { get; set; }

        public IIndexBuilder<T> CreateIndex()
        {
            return this;
        }

        public IIndexBuilder<T> On(IEnumerable<Func<T, string>> expression)
        {
            this.Expressions = expression;
            return this;
        }

        internal void Seek(string indexKey)
        {
        }

        public IIndexBuilder<T> Tag(string tagName)
        {
            this.Name = tagName;
            return this;
        }

        internal void Skip(int step)
        {
        }
    }
}
