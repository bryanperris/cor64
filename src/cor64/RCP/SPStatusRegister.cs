using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.IO;

/*            (W): [0]  clear halt          (R): [0]  halt
                 [1]  set halt                 [1]  broke
                 [2]  clear broke              [2]  dma busy
                 [3]  clear intr               [3]  dma full
                 [4]  set intr                 [4]  io full
                 [5]  clear sstep              [5]  single step
                 [6]  set sstep                [6]  interrupt on break
                 [7]  clear intr on break      [7]  signal 0 set
                 [8]  set intr on break        [8]  signal 1 set
                 [9]  clear signal 0           [9]  signal 2 set
                 [10] set signal 0             [10] signal 3 set
                 [11] clear signal 1           [11] signal 4 set
                 [12] set signal 1             [12] signal 5 set
                 [13] clear signal 2           [13] signal 6 set
                 [14] set signal 2             [14] signal 7 set
                 [15] clear signal 3
                 [16] set signal 3
                 [17] clear signal 4
                 [18] set signal 4
                 [19] clear signal 5
                 [20] set signal 5
                 [21] clear signal 6
                 [22] set signal 6
                 [23] clear signal 7
                 [24] set signal 7
*/

/* Notes:
* The SDK manages task swtiching for the RSP.  In order for the 
RSP to halt its current task, SPSTATUS_SET_SIGNAL0 is set to yield the RSP.
*/

namespace cor64.RCP
{
    [Flags]
    public enum StatusCmdFlags : uint {
        None                  = 0b0,
        ClearHalt             = 0b1,
        SetHalt               = 0b10,
        ClearBroke            = 0b100,
        ClearIntterupt        = 0b1000,
        SetInterrupt          = 0b10000,
        ClearSingleStep       = 0b100000,
        SetSingleStep         = 0b1000000,
        ClearInterruptOnBreak = 0b10000000,
        SetInterruptOnBreak   = 0b100000000,
        ClearSignal0          = 0b1000000000,
        SetSignal0            = 0b10000000000,
        ClearSignal1          = 0b100000000000,
        SetSignal1            = 0b1000000000000,
        ClearSignal2          = 0b10000000000000,
        SetSignal2            = 0b100000000000000,
        ClearSignal3          = 0b1000000000000000,
        SetSignal3            = 0b10000000000000000,
        ClearSignal4          = 0b100000000000000000,
        SetSignal4            = 0b1000000000000000000,
        ClearSignal5          = 0b10000000000000000000,
        SetSignal5            = 0b100000000000000000000,
        ClearSignal6          = 0b1000000000000000000000,
        SetSignal6            = 0b10000000000000000000000,
        ClearSignal7          = 0b100000000000000000000000,
        SetSignal7            = 0b1000000000000000000000000
    }

    [Flags]
    public enum StatusFlags : uint {
        None             = 0b0,
        Halt             = 0b1,
        Broke            = 0b10,
        DmaBusy          = 0b100,
        DmaFull          = 0b1000,
        IoFull           = 0b10000,
        SingleStep       = 0b100000,
        InterruptOnBreak = 0b1000000,
        Signal0Set       = 0b10000000,
        Signal1Set       = 0b100000000,
        Signal2Set       = 0b1000000000,
        Signal3Set       = 0b10000000000,
        Signal4Set       = 0b100000000000,
        Signal5Set       = 0b1000000000000,
        Signal6Set       = 0b10000000000000,
        Signal7Set       = 0b100000000000000
    }

    public class SPStatusRegister
    {
            private MemMappedBuffer m_Register;

            public event Action Change;

            public SPStatusRegister(MemMappedBuffer rawReg) {
                m_Register = rawReg;
                m_Register.Write += OnChange;
            }

            public uint Cmd { 
                get => m_Register.RegisterValue;
                private set => m_Register.RegisterValue = value;
            }

            private uint Status {
                get => m_Register.ReadonlyRegisterValue;
                set => m_Register.ReadonlyRegisterValue = value;
            }

            public bool TestCmdFlags(StatusCmdFlags flags) {
                uint _flags = (uint)flags;
                return (Cmd & _flags) == _flags;
            }

            public void ClearCmdFlags() {
                Cmd = 0;
            }

            public bool TestStatusFlags(StatusFlags flags) {
                uint _flags = (uint)flags;
                return (Status & _flags) == _flags;
            }

            public void ClearStatusFlags(StatusFlags flags) {
                uint _flags = (uint)flags;
                Status ^= _flags;
            }

            public StatusFlags StatusFlags {
                get => (StatusFlags)Status;
                set => Status = (uint)value;
            }
            
            private void OnChange() {
                Change?.Invoke();
            }

            public bool IsCmdEmpty => Cmd == 0;
    }
}
