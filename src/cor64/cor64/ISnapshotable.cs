using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64
{
    public interface ISnapshotable
    {
        IDictionary<string, string> SnapSave();

        void SnapLoad(IDictionary<string, string> dictionary);
    }
}
