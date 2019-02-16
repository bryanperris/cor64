using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I.CP0
{
    [Flags]
    public enum StatusFlags : uint
    {
        None = 0,
        InterruptsEnabled = 1,
        EnableExceptionLevel = 0b10,
        EnableErrorLevel = 0b100,
        User64Mode = 0b100000,
        Supervisor64Mode = 0b1000000,
        Kernel64Mode = 0b10000000,
        InterruptMask7 = 0b100000000,
        InterruptMask6 = 0b1000000000,
        InterruptMask5 = 0b10000000000,
        InterruptMask4 = 0b100000000000,
        InterruptMask3 = 0b1000000000000,
        InterruptMask2 = 0b10000000000000,
        InterruptMask1 = 0b100000000000000,
        InterruptMask0 = 0b1000000000000000,
        DisableECCExceptions = 0b10000000000000000,
        CacheCheckBits = 0b100000000000000000,
        LastCacheHit = 0b1000000000000000000,
        ResetSignal = 0b10000000000000000000,
        IsTLBShutdown = 0b100000000000000000000,
        UseBootstrapVectors = 0b1000000000000000000000,
        ReverseEndian = 0b10000000000000000000000,
        AdditonalFPU = 0b100000000000000000000000,
        ReducePower = 0b1000000000000000000000000,
        UsableCop3 = 0b10000000000000000000000000,
        UsableCop2 = 0b100000000000000000000000000,
        UsableCop1 = 0b1000000000000000000000000000,
        UsableCop0 = 0b10000000000000000000000000000
    }
}
