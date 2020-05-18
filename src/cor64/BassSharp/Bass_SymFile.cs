using System;
using System.Collections.Generic;
using System.Linq;

namespace cor64.BassSharp
{
    public abstract partial class Bass
    {
        void AppendSymFile(String label, long data)
        {
            if (IsWritePhase) {
                String scopedName = label;

                if (m_ScopeStack.Count > 0) {
                    scopedName = String.Format("{0}.{1}", m_ScopeStack.Merge("."), label);
                }

                //String entry = String.Format("{0:8X} {1}\n", data, scopedName);

                m_Symbols.Add(data, scopedName);

                Log.Debug("Symbol Added: {0}:{1}", scopedName, data);
            }
        }
    }
}