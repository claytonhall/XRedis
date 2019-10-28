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

    public interface IRecordSet<TRecord, TKey, TParent, TParentKey> : IRecordSet<TRecord, TKey>, IRecordSetWithParent<TParent>
    {
        //Index ParentRelationIndex { get; }
    }


    public interface IRecordSet<TRecord, TKey> : IEnumerable<TRecord>, IRecordSet
    {
        TRecord New(Action<TRecord> setter = null);

        TRecord Add(TRecord record);

        TRecord Seek(object value);

        void Delete(TRecord objKey);
        
        IRecordSet<TRecord, TKey> OrderBy(string indexTag);

        TRecord CurrentRecord { get; }
        void SkipX(int i = 1);

        IRecordSetEnumerator<TRecord, TKey> Enumerator { get; }
    }
}
