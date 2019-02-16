using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace cor64.MSIL
{
    public class ILBasicBlock
    {
        private ulong m_Address;
        private ulong m_NextPc;
        private DynamicMethod m_DMethod;
        private Object m_Bridge;
        private Object m_Owner;

        public ILBasicBlock(ulong address, Object owner, Object bridge)
        {
            m_Address = address;
            m_NextPc = address;
            m_Owner = owner;
            m_Bridge = bridge;

            m_DMethod =
                new DynamicMethod(
                    "CodeFunc_" + address.ToString("X8"),
                    typeof(void),
                    new Type[] { typeof(Object), typeof(Object) },
                    owner.GetType(),
                    true);
        }

        public void IncrementPC()
        {
            m_NextPc += 4;
        }

        public void Execute()
        {
            m_NextPc = m_Address;

            //m_DMethod.Invoke(m_Owner, new object[] { m_Bridge, this });
            ((Action<object, object>)m_DMethod.CreateDelegate(typeof(Action<object, object>)))(m_Bridge, this);
        }

        public ILGenerator GetCodeGenerator()
        {
            return m_DMethod.GetILGenerator();
        }

        public void LinkPC(ulong pc)
        {
            m_NextPc = pc;
        }

        public ulong NextPC => m_NextPc;

        public ulong Address => m_Address;

        internal Object Bridge => m_Bridge;
    }
}
