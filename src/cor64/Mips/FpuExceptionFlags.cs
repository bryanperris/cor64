using System;
namespace cor64.Mips
{
    [Flags]
    public enum FpuExceptionFlags : byte
    {
        None =          0,
        Inexact =       0b1,
        Underflow =     0b10,
        Overflow =      0b100,
        DivideByZero =  0b1000,
        Invalid =       0b10000,
        Unimplemented = 0b100000
    }
}
