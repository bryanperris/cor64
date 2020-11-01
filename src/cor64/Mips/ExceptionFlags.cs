using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public enum ExceptionType : byte
    {
        Interrupt = 0,
        TLBMod,
        TLBLoad,
        TLBStore,
        AddressLoad,
        AddressStore,
        InstructionBusError,
        DataBusError,
        Syscall,
        Breakpoint,
        Reserved,
        Unusable,
        Overflow,
        Trap,
        VCEInstruction, // Virtual Coherency Exception
        Float,
        Watch,
        VCEData, // Virtual Coherency Exception,
        Undefined
    }
}
