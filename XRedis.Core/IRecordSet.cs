using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Index = XRedis.Core.Fields.Indexes.Index;

namespace XRedis.Core
{
    public interface IRecordSet
    {
        Index Index { get; set; }
    }

    public interface IRecordSetWithParent<TParent>
    {
        TParent ParentRecord { get; set; }
    }

    public interface IRecordSet<TRecord, TParent> : IRecordSet<TRecord>, IRecordSetWithParent<TParent>
    {
        //Index ParentRelationIndex { get; }
    }


    public interface IRecordSet<T> : IEnumerable<T>, IRecordSet
    {
        T New(Action<T> setter = null);

        T Add(T record);

        T Seek(object value);

        void Delete(T objKey);
        
        IRecordSet<T> OrderBy(string indexTag);

        T CurrentRecord { get; }
        void SkipX(int i = 1);

        IRecordSetEnumerator<T> Enumerator { get; }
    }
}
