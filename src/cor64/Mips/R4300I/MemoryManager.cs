using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cor64.Mips.R4300I.TLB;

namespace cor64.Mips.R4300I
{
    public class MemoryManager
    {
        private readonly SystemController m_Cop0;
        private readonly TLBCache m_TranslationBuffer;
        private bool m_Disabled;

        public MemoryManager(SystemController controller) {
            m_Cop0 = controller;
            m_TranslationBuffer = new TLBCache(m_Cop0);
        }

        public void Disable(bool disable) {
            m_Disabled = disable;
        }

        public bool IsDisabled => m_Disabled;

        public TLBCache TLB => m_TranslationBuffer;

        public void Init() {
            m_TranslationBuffer.Initialize();
        }

        public bool VirtualToPhysical(bool isWrite, long virtualAddress, out long physicalAddress)
        {
            if (m_Disabled) {
                physicalAddress = virtualAddress;
                return true;
            }

            /* Force 32-bit addresses */
            virtualAddress &= 0xFFFFFFFF;

            if (m_Cop0.IsSupervisorMode)
            {
                throw new NotSupportedException("Supervisor vmap not yet supported");
            }
            else if (m_Cop0.IsUserMode)
            {
                throw new NotSupportedException("User vmap not yet supported");
            }
            else
            {
                /* Kernel 32-bit memory addressing */
                byte index = (byte)((uint)virtualAddress >> 28);

                physicalAddress = index switch
                {
                    0x0 or 0x1 or 0x2 or 0x3 or 0x4 or 0x5 or 0x6 or 0x7 => m_TranslationBuffer.Translate(virtualAddress, isWrite),
                    0x8 or 0x9 => virtualAddress - 0x80000000,
                    0xA => virtualAddress & 0x1FFFFFFF,
                    0xB => virtualAddress & 0x1FFFFFFF,
                    0xC or 0xD or 0xE or 0xF => m_TranslationBuffer.Translate(virtualAddress, isWrite),
                    _ => throw new ArgumentException("Invalid virtual address provided"),
                };
            }

            /* Check if memory access has thrown a non-maskable exception */
            if (m_Cop0.IsNonMaskablePending) {
                return false;
            }

            return true;
        }
    }
}