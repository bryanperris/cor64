using System;
namespace cor64
{
    public abstract class SpecialRegister32
    {
        public abstract uint Value { get; set; }

        protected abstract void InternalSet(uint value);

        protected uint ReadField(int bitOffset, int bitCount)
        {
            bitOffset = 31 - bitOffset;
            uint x = Value;
            x = x << bitOffset >> bitOffset;
            return x >> (32 - bitOffset - bitCount);
        }

        protected void WriteField(int bitOffset, int bitCount, uint value)
        {
            uint mask = ~(uint.MaxValue >> bitCount << bitCount);

            bitCount--;

            int left = (bitOffset - bitCount);
            mask <<= left;
            mask = ~mask;
            uint x = Value & mask;
            x |= (value << left) & ~mask;

            InternalSet(x);
        }
    }
}
