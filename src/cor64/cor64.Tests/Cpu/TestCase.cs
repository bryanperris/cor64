using cor64.Mips;
using cor64.Mips.R4300I;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Tests.Cpu
{
    public class TestCase
    {
        private Expectations m_ExpectationFlags;
        private Stream m_Program;
        private ExceptionType m_ExpectedExceptions;
        private FpuExceptionFlags m_ExpectedFpuExceptions;
        private ulong m_ExpectedLo;
        private ulong m_ExpectedHi;
        private bool m_NoDestReg;
        private RegBoundType m_XferSource;
        private RegBoundType m_XferTarget;
		private byte[] m_TestData;
        private int m_TestDataOffset;
        private bool m_InjectDMem;
        private bool m_IsFPU;

        [Flags]
        public enum Expectations : ushort
        {
            None =       0,
            Result =     1,
            Exceptions = 0b10,
            ResultLo =   0b100,
            ResultHi =   0b1000,
            ResultCp0 =  0b10000,
            BranchTaken  =    0b100000,
            DelaySlot =       0b1000000,
            Link =            0b10000000,
            DMemStore =         0b100000000,
            DMemLoad  =         0b1000000000
        }

        public TestCase(String opcode) : this(opcode, false)
        {
            
        }

        public TestCase(String opcode, bool isFPU)
        {
            m_IsFPU = isFPU;
            Opcode = opcode;
        }

        public TestCase WithImmediate()
        {
            IsImmediate = true;
            return this;
        }

        public TestCase WithOnlyImmediate()
        {
            IsImmediate = true;

            return this;
        }

        public TestCase Xfer(RegBoundType source, RegBoundType target, ulong value)
        {
            IsXfer = true;
            m_XferSource = source;
            m_XferTarget = target;

            switch (source)
            {
                case RegBoundType.Lo: SourceA = new KeyValuePair<int, dynamic>(SourceA.Key, value); break;
                case RegBoundType.Hi: SourceA = new KeyValuePair<int, dynamic>(SourceA.Key, value); break;
                case RegBoundType.Gpr: SourceA = new KeyValuePair<int, dynamic>(SourceA.Key, value); break;
                case RegBoundType.Cp0: SourceCp0 = new KeyValuePair<int, ulong>(Result.Key, value); break;
                default: throw new NotImplementedException();
            }

            switch (target)
            {
                case RegBoundType.Lo: m_ExpectationFlags = Expectations.ResultLo; break;
                case RegBoundType.Hi: m_ExpectationFlags = Expectations.ResultHi; break;
                case RegBoundType.Gpr: m_ExpectationFlags = Expectations.Result; break;
                case RegBoundType.Cp0: m_ExpectationFlags = Expectations.ResultCp0; break;
                default: throw new NotImplementedException();
            }

            return this;
        }

        public TestCase Values(dynamic a, dynamic b)
        {
            SourceA = new KeyValuePair<int, dynamic>(SourceA.Key, a);
            SourceB = new KeyValuePair<int, dynamic>(SourceB.Key, b);
            return this;
        }

        public TestCase FpuValues(dynamic a, dynamic b)
        {
            SourceA = new KeyValuePair<int, dynamic>(0, a);
            SourceB = new KeyValuePair<int, dynamic>(2, b);
            return this;
        }

        public TestCase FpuValues(dynamic a)
        {
            SourceA = new KeyValuePair<int, dynamic>(0, a);
            SourceB = new KeyValuePair<int, dynamic>(-1, 0);
            return this;
        }

        public TestCase Values(dynamic a, dynamic b, ulong c)
        {
            SourceA = new KeyValuePair<int, dynamic>(SourceA.Key, a);
            SourceB = new KeyValuePair<int, dynamic>(SourceB.Key, b);
            SourceC = new KeyValuePair<int, dynamic>(SourceC.Key, c);
            
            return this;
        }

        public TestCase Values(dynamic a)
        {
            SourceA = new KeyValuePair<int, dynamic>(SourceA.Key, a);
            SourceB = new KeyValuePair<int, dynamic>(-1, 0); // Make the target invalid
            return this;
        }

        public TestCase JOffset(uint value)
        {
            JumpOffset = value;
            return this;
        }

        public TestCase Expect(dynamic value)
        {
            m_ExpectationFlags |= Expectations.Result;

            if (!m_IsFPU)
            {
                Result = new KeyValuePair<int, dynamic>(Result.Key, value);
            }
            else
            {
                ExpectedFpuType = FpuHelper.ResolveFpuType(value);
                Result = new KeyValuePair<int, dynamic>(4, value);
            }
            
            return this;
        }

        public TestCase ExpectDMem(int offset, params byte[] value)
		{
			m_ExpectationFlags |= Expectations.DMemStore;
            m_TestData = value;
            m_TestDataOffset = offset;
            IsLoadStore = true;
			return this;
		}

        public TestCase TestDMem(int address, byte[] data)
        {
            m_ExpectationFlags |= Expectations.DMemLoad;
            IsLoadStore = true;
            m_TestData = data;
            m_InjectDMem = true;
            /* Better way to manage this? */
            Result = new KeyValuePair<int, dynamic>(SourceA.Key, Result.Value);
            SourceC = new KeyValuePair<int, dynamic>(SourceC.Key, (ulong)address);
            return this;
        }

        public TestCase ExpectXfer(dynamic value)
        {
            switch (m_XferTarget)
            {
                case RegBoundType.Lo: m_ExpectedLo = (ulong)value; ; break;
                case RegBoundType.Hi: m_ExpectedHi = (ulong)value; break;
                case RegBoundType.Gpr: Result = new KeyValuePair<int, dynamic>(Result.Key, (ulong)value); break;
                case RegBoundType.Cp0: SourceCp0 = new KeyValuePair<int, ulong>(SourceCp0.Key, (ulong)value); break;
                default: throw new NotImplementedException();
            }

            return this;
        }

        public TestCase ExpectBranch(bool taken, bool expectDelaySlot, bool expectLink)
        {
            IsBranch = true;

            if (taken)
            {
                m_ExpectationFlags = Expectations.BranchTaken;
            }

            if (expectDelaySlot)
            {
                m_ExpectationFlags |= Expectations.DelaySlot;
            }

            if (expectLink)
            {
                m_ExpectationFlags |= Expectations.Link;
            }

            return this;
        }

        public TestCase ExpectJump(ulong address, bool expectLink)
        {
            IsJump = true;
            ExpectedJump = address;

            if (expectLink)
            {
                m_ExpectationFlags |= Expectations.Link;
            }

            return this;
        }

        public TestCase Except(ExceptionType exceptions)
        {
            m_ExpectationFlags |= Expectations.Exceptions;
            m_ExpectedExceptions = exceptions;
            return this;
        }

        public TestCase Except(FpuExceptionFlags exceptions)
        {
            m_ExpectationFlags = Expectations.Exceptions;
            m_ExpectedFpuExceptions = exceptions;
            return this;
        }

        public TestCase CoreOverride(Type type)
        {
            CoreTypeOverride = type;
            return this;
        }

        public TestCase NoDest()
        {
            m_NoDestReg = true;
            return this;
        }

        public TestCase ExpectLo(ulong value)
        {
            m_ExpectationFlags |= Expectations.ResultLo;
            m_ExpectedLo = value;
            m_NoDestReg = true;
            return this;
        }

        public TestCase ExpectHi(ulong value)
        {
            m_ExpectationFlags |= Expectations.ResultHi;
            m_ExpectedHi = value;
            m_NoDestReg = true;
            return this;
        }

        public String Opcode { get; private set; }

        public ExceptionType ExpectedExceptions => m_ExpectedExceptions;

        public FpuExceptionFlags ExpectedFPUExceptions => m_ExpectedFpuExceptions;

        internal TestCase WithShift()
        {
            IsShift = true;
            return this;
        }

        public KeyValuePair<int, dynamic> SourceA { get; private set; } = new KeyValuePair<int, dynamic>(1, 0);

        public KeyValuePair<int, dynamic> SourceB { get; private set; } = new KeyValuePair<int, dynamic>(2, 0);

        public KeyValuePair<int, dynamic> SourceC { get; private set; } = new KeyValuePair<int, dynamic>(3, 0);

        public KeyValuePair<int, dynamic> Result { get; private set; } = new KeyValuePair<int, dynamic>(4, 0);

        public KeyValuePair<int, ulong> SourceCp0 { get; private set; } = new KeyValuePair<int, ulong>(1, 0);
         
        public bool Test32 { get; set; } = true;

        public bool Test64 { get; set; } = true;

        public bool IsImmediate { get; private set; }

        public bool IsShift { get; private set; }

        public bool IsLoadStore { get; private set; }

        public bool InjectDMem => m_InjectDMem;

        public bool IsXfer { get; private set; }

        public bool IsBranch { get; private set; }

        public bool IsJump { get; private set; }

        public bool IsFpuTest { get => m_IsFPU; }

        public uint? JumpOffset { get; private set; }

        public bool NoDestinationReg => m_NoDestReg;

        public ulong ExpectedLo => m_ExpectedLo;

        public ulong ExpectedHi => m_ExpectedHi;

        public byte[] ExpectedBytes => m_TestData;

        public int ExepectedBytesOffset => m_TestDataOffset;

        public ulong ExpectedJump { get; private set; }

        public RegBoundType XferSource => m_XferSource;

        public RegBoundType XferTarget => m_XferTarget;

        public Expectations ExpectationFlags => m_ExpectationFlags;

        public FpuValueType ExpectedFpuType { get; set; }

        public Type CoreTypeOverride { get; private set; }

        internal void SetProgram(Stream rom)
        {
            m_Program = rom;
        }

        public Stream GetProgram()
        {
            return m_Program;
        }

        public TestCase ExpectCp0(int select, ulong value)
        {
            throw new NotImplementedException();
        }
    }
}
