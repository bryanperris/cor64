using System;
namespace cor64.Debugging
{
    public class NamedMemorySection
    {
        public uint Start { get; private set; }
        public uint End { get; private set; }
        public String Name { get; private set; }

        public NamedMemorySection(uint start, uint end, String name)
        {
            Start = start;
            End = end;
            Name = name;
        }
    }
}
