using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XRedis.Core.Keys;

namespace XRedis.Core
{
    public enum WriteKeyState
    {
        FAILURE, CURRENT, CREATE
    }

    public class WriteKeyResult
    {
        public WriteKeyState State { get; set; }

        public VersionedRecordKey CurrentKey { get; set; }

        public VersionedRecordKey NewKey { get; set; }

        public string Message { get; set; }
    }
}
