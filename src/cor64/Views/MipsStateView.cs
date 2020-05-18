using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Views
{
    public class MipsStateView
    {
        private Mips.BaseInterpreter m_Target;

        public MipsStateView(Mips.BaseInterpreter target)
        {
            m_Target = target;
        }

        public ulong ReadValueU64(String name)
        {
            switch (name)
            {
                default: throw new ArgumentException("key doesn't exist");

                /* GPR lookup by numeric */

            }
        }
    }
}
