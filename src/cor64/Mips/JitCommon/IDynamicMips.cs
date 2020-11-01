using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.JitCommon
{
    public interface IDynamicMips
    {
        uint ReadGPR32(int select);

        void WriteGPR32(int select, uint value);

        ulong ReadGPR64(int select);

        void WriteGPR64(int select, ulong value);

        double ReadFPRDouble(int select);

        void WriteFPRDouble(int select, double value);

        float ReadFPRFloat(int select);

        void WriteFPRFloat(int select, float value);

        void SetExceptionState(ExceptionType type);

        void SetBlockJump(MipsExecutableBlock block, ulong pc);

        void ClearBlockJump(MipsExecutableBlock block);

        uint ReadLo32();

        uint ReadHi32();

        void WriteLo32(uint value);

        void WriteHi32(uint value);

        ulong ReadLo64();

        ulong ReadHi64();

        void WriteLo64(ulong value);

        void WriteHi64(ulong value);

        ulong Cop0_Read(int reg);

        void Cop0_Write(int reg, ulong val);

        uint Cop1_Ctrl_Read();

        void Cop1_Ctrl_Write(uint value);

        void Fallback(MipsExecutableBlock block, int instOffset);

        bool GetNullified();

        void SetNullified();

        void ClearNullified();

        void ReadDataMemory(long address, int size);

        void WriteDataMemory(long address, int size);

        byte GetMemData8();

        void SetMemData8(byte value);

        ushort GetMemData16();

        void SetMemData16(ushort value);

        uint GetMemData32();

        void SetMemData32(uint value);

        ulong GetMemData64();

        void SetMemData64(ulong value);

        bool CoprocessorFlag(bool compare);

        void SetFpuExceptionState(FpuExceptionFlags flags);

        void SetFpuCondition(bool condition);

        void ExceptionBegin(MipsExecutableBlock block);

        void ExceptionEnd();

        bool CheckInterrupts();
    }
}
