using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using cor64.Mips.JitCommon;
using DYNMETHOD = System.Action<cor64.Mips.JitCommon.IDynamicMips, cor64.Mips.R4300I.JitIL.MipsILExecutableBlock>;

namespace cor64.Mips.R4300I.JitIL
{
    public class MipsILExecutableBlock : MipsExecutableBlock
    {
        private DynamicMethod m_DynamicMethod;
        private Action<IDynamicMips, MipsILExecutableBlock> m_CallableMethod;

        public MipsILExecutableBlock(ulong address, int size, RuntimeBasicBlock shadow) : base(address, size, shadow)
        {
        }

        public ILGenerator Begin()
        {
            m_DynamicMethod = new DynamicMethod(
                    "MipsILMethod_" + Address.ToString("X8"),
                    typeof(void),
                    new Type[] { typeof(IDynamicMips), typeof(MipsILExecutableBlock) },
                    typeof(Object),
                    true);

            return m_DynamicMethod.GetILGenerator();
        }

        public void End()
        {
            try
            {
                m_CallableMethod = (DYNMETHOD)m_DynamicMethod.CreateDelegate(typeof(DYNMETHOD));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public override void Execute(IDynamicMips owner)
        {
            m_CallableMethod(owner, this);
        }
    }
}
