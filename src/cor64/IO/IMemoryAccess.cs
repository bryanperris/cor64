using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cor64.IO
{
    public interface IMemoryAccess
    {
        sbyte S8(long address);
        short S16(long address);
        int S32(long address);
        long S64(long address);



        byte U8(long address);
        ushort U16(long address);
        uint U32(long address);
        ulong U64(long address);



        void S8(long address, sbyte value);
        void S16(long address, short value);
        void S32(long address, int value);
        void S64(long address, long value);



        void U8(long address, byte value);
        void U16(long address, ushort value);
        void U32(long address, uint value);
        void U64(long address, ulong value);
    }
}