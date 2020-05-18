using System;

namespace cor64.Mips {
    [Flags]
    public enum VectorOpFlags : uint {
        None = 0,
        Accumulate       = 0b1,
        Quantized        = 0b10,
        PartialL         = 0b100,
        PartialM         = 0b1000,
        PartialN         = 0b10000,
        PartialH         = 0b100000,
        DctRoundPositive = 0b1000000,
        DctRoundNegative = 0b10000000,
        SquareRoot       = 0b100000000,
        Crimp            = 0b1000000000,
        Merge            = 0b10000000000,
        Half             = 0b100000000000,
        Forth            = 0b1000000000000,
        Upper            = 0b10000000000000,
        Wrap             = 0b100000000000000,
        Transpose        = 0b1000000000000000,
        Rest             = 0b10000000000000000
    }
}