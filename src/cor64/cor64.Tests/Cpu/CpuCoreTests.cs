using cor64.IO;
using cor64.Mips;
using cor64.Mips.R4300I;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static cor64.Tests.Cpu.Helper.ParamType;
using static cor64.Mips.ExceptionType;
using static cor64.Tests.Cpu.Helper;

namespace cor64.Tests.Cpu
{
    public class CpuCoreTests
    {
        const ulong MinusOne = 0xFFFFFFFFFFFFFFFF;
        const ulong MinusTwo = 0xFFFFFFFFFFFFFFFE;
        const ulong SignedMax = 0x000000007FFFFFFF;
        const ulong SignedOvfl = 0xFFFFFFFF80000000;
        const ulong SignedMax64 = 0x7FFFFFFFFFFFFFFF;
        const ulong SignedOvfl64 = 0x8000000000000000;

        [Fact]
        [Logging]
        public void AdditionTest()
        {
            "add".CPUTest(1, 4).Expect(5).Run();
                  CPUTest(5, MinusOne).Expect(4).Run();
                  CPUTest(SignedMax, 2).Except(Overflow).Run();

            "addu".CPUTest(1, 4).Expect(5).Run();
                   CPUTest(SignedMax, 1).Expect(SignedOvfl).Run();
                   CPUTest(MinusOne, 1).Expect(0).Run();

            "addi".CPUTest(1, 4, Immediate).Expect(5).Run();
                   CPUTest(5, MinusOne, Immediate).Expect(4).Run();
                   CPUTest(SignedMax, 1, Immediate).Except(Overflow).Run();

            "addiu".CPUTest(1, 4, Immediate).Expect(5).Run();
                    CPUTest(5, MinusOne, Immediate).Expect(4).Run();
                    CPUTest(MinusOne, MinusOne, Immediate).Expect(MinusTwo).Run();

            "dadd".CPUTest(1, 4).Only64().Expect(5).Run();
                   CPUTest(1, 4).Only32().Except(Reserved).Run();
                   CPUTest(5, MinusOne).Only64().Expect(4).Run();
                   CPUTest(SignedMax64, 1).Only64().Except(Overflow).Run();

            "daddu".CPUTest(1, 4).Only64().Expect(5).Run();
                    CPUTest(1, 4).Only32().Except(Reserved).Run();
                    CPUTest(SignedMax64, 1).Only64().Expect(SignedOvfl64).Run();
                    CPUTest(MinusOne, 1).Only64().Expect(0).Run();

            "daddi".CPUTest(1, 4, Immediate).Only64().Expect(5).Run();
                    CPUTest(1, 4, Immediate).Only32().Except(Reserved).Run();
                    CPUTest(5, MinusOne, Immediate).Only64().Expect(4).Run();
                    CPUTest(SignedMax64, 1, Immediate).Only64().Except(Overflow).Run();
        }

        [Fact]
        [Logging]
        public void BitwiseTest()
        {
            "and".CPUTest(1, 0).Expect(0).Run();
                  CPUTest(0, 1).Expect(0).Run();
                  CPUTest(1, 1).Expect(1).Run();
                  CPUTest(0, 0).Expect(0).Run();

            "or".CPUTest(1, 0).Expect(1).Run();
                 CPUTest(0, 1).Expect(1).Run();
                 CPUTest(1, 1).Expect(1).Run();
                 CPUTest(0, 0).Expect(0).Run();


            "xor".CPUTest(1, 0).Expect(1).Run();
                  CPUTest(0, 1).Expect(1).Run();
                  CPUTest(1, 1).Expect(0).Run();
                  CPUTest(0, 0).Expect(0).Run();

            "nor".CPUTest(1, 0).Expect(MinusTwo).Run();


            "andi".CPUTest(1, 0, Immediate).Expect(0).Run();
                   CPUTest(0, 1, Immediate).Expect(0).Run();
                   CPUTest(1, 1, Immediate).Expect(1).Run();
                   CPUTest(0, 0, Immediate).Expect(0).Run();

            "ori".CPUTest(1, 0, Immediate).Expect(1).Run();
                  CPUTest(0, 1, Immediate).Expect(1).Run();
                  CPUTest(1, 1, Immediate).Expect(1).Run();
                  CPUTest(0, 0, Immediate).Expect(0).Run();


            "xori".CPUTest(1, 0, Immediate).Expect(1).Run();
                   CPUTest(0, 1, Immediate).Expect(1).Run();
                   CPUTest(1, 1, Immediate).Expect(0).Run();
                   CPUTest(0, 0, Immediate).Expect(0).Run();
        }

        [Fact]
        [Logging]
        public void DivisionTest()
        {
            "div".CPUTest(4, 2).ExpectLo(2).Run();
                  CPUTest(SignedMax, MinusOne).ExpectLo(SignedOvfl + 1).Run();
                  CPUTest(MinusOne, SignedMax).ExpectHi(MinusOne).Run();
                  CPUTest(1, 0).ExpectLo(0xFFFFFFFFFFFFFFFF).ExpectHi(1).Run();
                  CPUTest(5, 2).ExpectLo(2).ExpectHi(1).Run();

            "divu".CPUTest(4, 2).ExpectLo(2).Run();
                   CPUTest(SignedMax, MinusOne).ExpectLo(0).ExpectHi(SignedMax).Run();
                   CPUTest(MinusOne, SignedMax).ExpectLo(2).ExpectHi(1).Run();
                   CPUTest(1, 0).ExpectLo(0xFFFFFFFFFFFFFFFF).ExpectHi(1).Run();
                   CPUTest(5, 2).ExpectLo(2).ExpectHi(1).Run();

            "ddiv".CPUTest(4, 2).Only64().ExpectLo(2).Run();
                   CPUTest(1, 1).Only32().NoDest().Except(Reserved).Run();
                   CPUTest(SignedMax64, MinusOne).Only64().ExpectLo(SignedOvfl64 + 1).Run();
                   CPUTest(MinusOne, SignedMax64).Only64().ExpectHi(MinusOne).Run();
                   CPUTest(1, 0).Only64().ExpectLo(0xFFFFFFFFFFFFFFFF).ExpectHi(1).Run();
                   CPUTest(5, 2).Only64().ExpectLo(2).ExpectHi(1).Run();


            "ddivu".CPUTest(4, 2).Only64().ExpectLo(2).Run();
                   CPUTest(1, 1).Only32().NoDest().Except(Reserved).Run();
                   CPUTest(SignedMax64, MinusOne).Only64().ExpectLo(0).ExpectHi(SignedMax64).Run();
                   CPUTest(MinusOne, SignedMax64).Only64().ExpectLo(2).ExpectHi(1).Run();
                   CPUTest(1, 0).Only64().ExpectLo(0xFFFFFFFFFFFFFFFF).ExpectHi(1).Run();
                   CPUTest(5, 2).Only64().ExpectLo(2).ExpectHi(1).Run();
        }

        [Fact]
        [Logging]
        public void MultiplicationTest()
        {
            "mult".CPUTest(1, 0).ExpectLo(0).ExpectHi(0).Run();
                   CPUTest(2, 4).ExpectLo(8).ExpectHi(0).Run();
                   CPUTest(SignedMax, 2).ExpectLo(MinusTwo).ExpectHi(0).Run();
                   CPUTest(SignedMax64, 2).ExpectLo(MinusTwo).ExpectHi(MinusOne).Run();

            "multu".CPUTest(1, 0).ExpectLo(0).ExpectHi(0).Run();
                    CPUTest(2, 4).ExpectLo(8).ExpectHi(0).Run();
                    CPUTest(SignedMax, 2).ExpectLo(MinusTwo).ExpectHi(0).Run();
                    CPUTest(SignedMax64, 2).ExpectLo(MinusTwo).ExpectHi(1).Run();

            "dmultu".CPUTest(1, 0).Only64().ExpectLo(0).ExpectHi(0).Run();
                     CPUTest(1, 1).Only32().NoDest().Except(Reserved).Run();
                     CPUTest(11111, 2).Only64().ExpectLo(22222).Run();
                     CPUTest(1234567890123456789, 987654321098765432).Only64().ExpectLo(0x85FC33F23D8F45D8).ExpectHi(0x00EAD56DB9553D9A).Run();
                     KromTestCases.Test_DMULTU();

            "dmult".CPUTest(1, 0).Only64().ExpectLo(0).ExpectHi(0).Run();
                    CPUTest(11111, 2).Only64().ExpectLo(22222).Run();
                    CPUTest(1, 1).Only32().NoDest().Except(Reserved).Run();
                    CPUTest(1234567890123456789, 987654321098765432).Only64().ExpectLo(0x85FC33F23D8F45D8).ExpectHi(0x00EAD56DB9553D9A).Run();
                    KromTestCases.Test_DMULT();
        }

        [Fact]
        [Logging]
        public void ShiftingTest()
        {
            "sll".CPUTest(2, 2, Shift).Expect(8).Run();
                  CPUTest(0xFF, 8, Shift).Expect(0xFF00).Run();
                  CPUTest(0xFF, 24, Shift).Expect(0xFFFFFFFFFF000000).Run();

            "sllv".CPUTest(2, 2).Expect(8).Run();
                   CPUTest(0xFF, 8).Expect(0xFF00).Run();

            "sra".CPUTest(0xFFFFFFFF, 1, Shift).Expect(0xFFFFFFFFFFFFFFFF).Run();
                  CPUTest(0xFFFFFFFF, 16, Shift).Expect(0xFFFFFFFFFFFFFFFF).Run();
                  CPUTest(0xFF00, 8, Shift).Expect(0xFF).Run();

            "srav".CPUTest(0xFFFFFFFF, 1).Expect(0xFFFFFFFFFFFFFFFF).Run();
                   CPUTest(0xFFFFFFFF, 16).Expect(0xFFFFFFFFFFFFFFFF).Run();
                   CPUTest(0xFF00, 8).Expect(0xFF).Run();

            "srl".CPUTest(0xFFFFFFFF, 1, Shift).Expect(0x7FFFFFFF).Run();
                  CPUTest(0xFFFFFFFF, 16, Shift).Expect(0xFFFF).Run();
                  CPUTest(0xFF00, 8, Shift).Expect(0xFF).Run();

            "srlv".CPUTest(0xFFFFFFFF, 1).Expect(0x7FFFFFFF).Run();
                   CPUTest(0xFFFFFFFF, 16).Expect(0xFFFF).Run();
                   CPUTest(0xFF00, 8).Expect(0xFF).Run();

            "dsll".CPUTest(2, 2, Shift).Only64().Expect(8).Run();
                   CPUTest(2, 2, Shift).Only32().Except(Reserved).Run();
                   CPUTest(0xFF, 8, Shift).Only64().Expect(0xFF00).Run();

            "dsllv".CPUTest(2, 2).Only64().Expect(8).Run();
                    CPUTest(2, 2).Only32().Except(Reserved).Run();
                    CPUTest(0xFF, 8).Only64().Expect(0xFF00).Run();

            "dsll32".CPUTest(2, 2, Shift).Only32().Except(Reserved).Run();
                     CPUTest(0xFF, 0, Shift).Only64().Expect(0x000000FF00000000).Run();

            "dsra".CPUTest(2, 2, Shift).Only32().Except(Reserved).Run();
                   CPUTest(0xFFFFFFFFFFFFFFFF, 1, Shift).Only64().Expect(0xFFFFFFFFFFFFFFFF).Run();
                   CPUTest(0xFFFFFFFFFFFFFFFF, 32, Shift).Only64().Expect(0xFFFFFFFFFFFFFFFF).Run();
                   CPUTest(0xFF00, 8, Shift).Only64().Expect(0xFF).Run();

            "dsrav".CPUTest(2, 2).Only32().Except(Reserved).Run();
                    CPUTest(0xFFFFFFFFFFFFFFFF, 1).Only64().Expect(0xFFFFFFFFFFFFFFFF).Run();
                    CPUTest(0xFFFFFFFFFFFFFFFF, 32).Only64().Expect(0xFFFFFFFFFFFFFFFF).Run();
                    CPUTest(0xFF00, 8).Only64().Expect(0xFF).Run();

            "dsra32".CPUTest(2, 2, Shift).Only32().Except(Reserved).Run();
                     CPUTest(0xFFFFFFFFFFFFFFFF, 1, Shift).Only64().Expect(0xFFFFFFFFFFFFFFFF).Run();
                     CPUTest(0xFFFFFFFFFFFFFFFF, 16, Shift).Only64().Expect(0xFFFFFFFFFFFFFFFF).Run();
                     CPUTest(0xFF00000000000000, 0, Shift).Only64().Expect(0xFFFFFFFFFF000000).Run();

            "dsrl".CPUTest(2, 2, Shift).Only32().Except(Reserved).Run();
                   CPUTest(0xFFFFFFFFFFFFFFFF, 1, Shift).Only64().Expect(0x7FFFFFFFFFFFFFFF).Run();
                   CPUTest(0xFFFFFFFFFFFFFFFF, 16, Shift).Only64().Expect(0x0000FFFFFFFFFFFF).Run();
                   CPUTest(0xFF00, 8, Shift).Only64().Expect(0xFF).Run();

            "dsrlv".CPUTest(2, 2).Only32().Except(Reserved).Run();
                    CPUTest(0xFFFFFFFFFFFFFFFF, 1).Only64().Expect(0x7FFFFFFFFFFFFFFF).Run();
                    CPUTest(0xFFFFFFFFFFFFFFFF, 16).Only64().Expect(0x0000FFFFFFFFFFFF).Run();
                    CPUTest(0xFF00, 8).Only64().Expect(0xFF).Run();

            "dsrl32".CPUTest(2, 2, Shift).Only32().Except(Reserved).Run();
                     CPUTest(0xFFFFFFFFFFFFFFFF, 1, Shift).Only64().Expect(0x000000007FFFFFFF).Run();
                     CPUTest(0xFFFFFFFFFFFFFFFF, 0, Shift).Only64().Expect(0x00000000FFFFFFFF).Run();
                     CPUTest(0xFF00000000000000, 0, Shift).Only64().Expect(0x00000000FF000000).Run();
        }

        [Fact]
        [Logging]
        public void SubtractTest()
        {
            "sub".CPUTest(5, 4).Expect(1).Run();
                  CPUTest(0x7FFFFFFF, 0xFFFFFFF6).Except(Overflow).Run();
                  CPUTest(0x7FFFFFFF, 0xF).Expect(0x7FFFFFF0).Run();
                  CPUTest(0xFFFFFFFF, 0x7FFFFFFF).Expect(0xFFFFFFFF80000000).Run();
                  CPUTest(5, 5).Expect(0).Run();

            "subu".CPUTest(5, 4).Expect(1).Run();
                   CPUTest(0x7FFFFFFF, 0xFFFFFFF6).Expect(0xFFFFFFFF80000009).Run();
                   CPUTest(0x7FFFFFFF, 0xF).Expect(0x7FFFFFF0).Run();
                   CPUTest(0xFFFFFFFF, 0x7FFFFFFF).Expect(0xFFFFFFFF80000000).Run();
                   CPUTest(5, 5).Expect(0).Run();

            "dsub".CPUTest(5, 4).Only64().Expect(1).Run();
                   CPUTest(1, 1).Only32().Except(Reserved).Run();
                   CPUTest(0x7FFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFF6).Only64().Except(Overflow).Run();
                   CPUTest(0x7FFFFFFFFFFFFFFF, 0xF).Only64().Expect(0x7FFFFFFFFFFFFFF0).Run();
                   CPUTest(0xFFFFFFFFFFFFFFFF, 0x7FFFFFFFFFFFFFFF).Only64().Expect(0x8000000000000000).Run();
                   CPUTest(5, 5).Only64().Expect(0).Run();

            "dsubu".CPUTest(5, 4).Only64().Expect(1).Run();
                    CPUTest(1, 1).Only32().Except(Reserved).Run();
                    CPUTest(0x7FFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFF6).Only64().Expect(0x8000000000000009).Run();
                    CPUTest(0x7FFFFFFFFFFFFFFF, 0xF).Only64().Expect(0x7FFFFFFFFFFFFFF0).Run();
                    CPUTest(0xFFFFFFFFFFFFFFFF, 0x7FFFFFFFFFFFFFFF).Only64().Expect(0x8000000000000000).Run();
                    CPUTest(5, 5).Only64().Expect(0).Run();
        }

        [Fact]
        [Logging] 
        public void SetTest()
        {
            "slt".CPUTest(0, 1).Expect(1).Run();
                  CPUTest(1, 0).Expect(0).Run();
                  CPUTest(10.Neg(), 15).Expect(1).Run();
                  CPUTest(MinusTwo, MinusOne).Expect(1).Run();

            "sltu".CPUTest(0, 1).Expect(1).Run();
                   CPUTest(1, 0).Expect(0).Run();
                   CPUTest(MinusTwo, MinusOne).Expect(1).Run();

            "slti".CPUTest(0, 1, Immediate).Expect(1).Run();
                   CPUTest(1, 0, Immediate).Expect(0).Run();
                   CPUTest(10.Neg(), 15, Immediate).Expect(1).Run();
                   CPUTest(MinusTwo, MinusOne, Immediate).Expect(1).Run();

            "sltiu".CPUTest(0, 1, Immediate).Expect(1).Run();
                    CPUTest(1, 0, Immediate).Expect(0).Run();
                    CPUTest(MinusTwo, MinusOne, Immediate).Expect(0).Run();
        }

        [Fact]
        [Logging]
        public void XferTest()
        {
            "mfhi".CPUTest(5, RegBoundType.Hi, RegBoundType.Gpr).ExpectXfer(5).Run();
            "mthi".CPUTest(5, RegBoundType.Gpr, RegBoundType.Hi).ExpectXfer(5).Run();

            "mflo".CPUTest(5, RegBoundType.Lo, RegBoundType.Gpr).ExpectXfer(5).Run();
            "mtlo".CPUTest(5, RegBoundType.Gpr, RegBoundType.Lo).ExpectXfer(5).Run();

            "mtc0".CPUTest(5, RegBoundType.Gpr, RegBoundType.Cp0).ExpectXfer(5).Run();
            "mfc0".CPUTest(MinusOne, RegBoundType.Cp0, RegBoundType.Gpr).ExpectXfer(MinusOne).Run();

            "dmtc0".CPUTest(MinusOne, RegBoundType.Gpr, RegBoundType.Cp0).Only64().ExpectXfer(MinusOne).Run();
            "dmfc0".CPUTest(MinusOne, RegBoundType.Cp0, RegBoundType.Gpr).Only64().ExpectXfer(MinusOne).Run();

            // TODO: Move and To FPU
            // TODO: Move and To FPU Control
        }

        [Fact]
        [Logging]
        public void BranchTest()
        {
            "beq".CPUTest(5, 5).ExpectBranch(true, true, false).Run();
                  CPUTest(5, 4).ExpectBranch(false, true, false).Run();

            "bne".CPUTest(5, 5).ExpectBranch(false, true, false).Run();
                  CPUTest(5, 4).ExpectBranch(true, true, false).Run();

            "beql".CPUTest(5, 5).ExpectBranch(true, true, false).Run();
                   CPUTest(5, 4).ExpectBranch(false, false, false).Run();

            "bnel".CPUTest(5, 5).ExpectBranch(false, false, false).Run();
                   CPUTest(5, 4).ExpectBranch(true, true, false).Run();

            "blez".CPUTestSingle(5.Neg()).ExpectBranch(true, true, false).Run();
                   CPUTestSingle(0).ExpectBranch(true, true, false).Run();
                   CPUTestSingle(1).ExpectBranch(false, true, false).Run();

            "blezl".CPUTestSingle(5.Neg()).ExpectBranch(true, true, false).Run();
                    CPUTestSingle(0).ExpectBranch(true, true, false).Run();
                    CPUTestSingle(1).ExpectBranch(false, false, false).Run();

            "bltz".CPUTestSingle(5.Neg()).ExpectBranch(true, true, false).Run();
                   CPUTestSingle(0).ExpectBranch(false, true, false).Run();

            "bltzal".CPUTestSingle(5.Neg()).ExpectBranch(true, true, true).Run();
                     CPUTestSingle(1).ExpectBranch(false, true, true).Run();

            "bltzl".CPUTestSingle(5.Neg()).ExpectBranch(true, true, false).Run();
                    CPUTestSingle(0).ExpectBranch(false, false, false).Run();

            "bltzall".CPUTestSingle(5.Neg()).ExpectBranch(true, true, true).Run();
                      CPUTestSingle(1).ExpectBranch(false, false, true).Run();

            "bgez".CPUTestSingle(5).ExpectBranch(true, true, false).Run();
                   CPUTestSingle(0).ExpectBranch(true, true, false).Run(true);
                   CPUTestSingle(1.Neg()).ExpectBranch(false, true, false).Run();

            "bgezl".CPUTestSingle(5).ExpectBranch(true, true, false).Run();
                    CPUTestSingle(0).ExpectBranch(true, true, false).Run();
                    CPUTestSingle(1.Neg()).ExpectBranch(false, false, false).Run();

            "bgtz".CPUTestSingle(5).ExpectBranch(true, true, false).Run();
                   CPUTestSingle(0).ExpectBranch(false, true, false).Run();

            "bgtzl".CPUTestSingle(5).ExpectBranch(true, true, false).Run();
                    CPUTestSingle(0).ExpectBranch(false, false, false).Run();


            /* These tests just assume Coprcessor 1 is always usable */

            "bc1f".CPUTest().ExpectBranch(false, true, false);

            "bc1fl".CPUTest().ExpectBranch(false, false, false);

            "bc1t".CPUTest().ExpectBranch(true, true, false);

            "bc1tl".CPUTest().ExpectBranch(true, true, false);
        }

        [Fact]
        [Logging]
        public void Jump()
        {
            "j".CPUTestSingle(4, ParamType.Offset).ExpectJump(4, false).Run();

            "jal".CPUTestSingle(4, ParamType.Offset).ExpectJump(4, true).Run();

            "jr".CPUTestSingle(4).ExpectJump(4, false).Run();

            "jalr".CPUTestSingle(4).ExpectJump(4, true).Run();
        }

        [Fact]
        [Logging]
        public void Cop0Tests()
        {
            var state = new ExecutionState();
            var controller = new SystemController(state);
            var registers = state.Cp0;
            var status = state.Cp0.Status;
            var cause = state.Cp0.Cause;

            Assert.Equal(0UL, registers.Read(CTS.CP0_REG_SR));
            Assert.False(status.TestFlags(StatusRegister.StatusFlags.UsableCop0));

            registers.Write(CTS.CP0_REG_SR, ~0U);
            Assert.True(status.TestFlags(StatusRegister.StatusFlags.UsableCop0));
            Assert.True(status.TestFlags(StatusRegister.StatusFlags.Kernel64Mode));

            Assert.Equal(0UL, registers.Read(CTS.CP0_REG_CAUSE));
            Assert.False(cause.TestInterrupt(CauseRegister.Interrupt.Int0));

            registers.Write(CTS.CP0_REG_CAUSE, ~0U);
            Assert.True(cause.TestInterrupt(CauseRegister.Interrupt.All));
        }

        [Fact]
        [Logging]
        public void StoreTests()
        {
            "sb".CPUTestTriple(5, 0, 0).ExpectDMem(0, 0x5).Run();

            "sh".CPUTestTriple(0xDEAD, 0, 0).ExpectDMem(0, 0xDE, 0xAD).Run();

            "sw".CPUTestTriple(0x00DEADBE, 0, 0).ExpectDMem(0, 0x00, 0xDE, 0xAD, 0xBE).Run();

            "sd".CPUTestTriple(0xDEADBEEFFEEDDEAF, 0, 0).Only64().ExpectDMem(0, 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED, 0xDE, 0xAF).Run();
                 CPUTestTriple(0, 0, 0).Only32().ExpectDMem(0, null).Except(ExceptionType.Reserved).Run();

            "swl".CPUTestTriple(0xDEADBEEF, 0, 0).ExpectDMem(0, 0xDE, 0xAD, 0xBE, 0xEF).Run();
                  CPUTestTriple(0xDEADBEEF, 0, 1).ExpectDMem(0, 0xDE, 0xAD, 0xBE, 0x00).Run();
                  CPUTestTriple(0xDEADBEEF, 0, 2).ExpectDMem(0, 0xDE, 0xAD, 0x00, 0x00).Run();
                  CPUTestTriple(0xDEADBEEF, 0, 3).ExpectDMem(0, 0xDE, 0x00, 0x00, 0x00).Run();
                  CPUTestTriple(0xDEADBEEF, 0, 4).ExpectDMem(4, 0xDE, 0xAD, 0xBE, 0xEF).Run();

            "swr".CPUTestTriple(0xDEADBEEF, 0, 0).ExpectDMem(0, 0xEF, 0x00, 0x00, 0x00).Run();
                  CPUTestTriple(0xDEADBEEF, 0, 1).ExpectDMem(0, 0xBE, 0xEF, 0x00, 0x00).Run();
                  CPUTestTriple(0xDEADBEEF, 0, 2).ExpectDMem(0, 0xAD, 0xBE, 0xEF, 0x00).Run();
                  CPUTestTriple(0xDEADBEEF, 0, 3).ExpectDMem(0, 0xDE, 0xAD, 0xBE, 0xEF).Run();
                  CPUTestTriple(0xDEADBEEF, 0, 4).ExpectDMem(4, 0xEF, 0x00, 0x00, 0x00).Run();
        }

        [Fact]
        [Logging]
        public void LoadTests()
        {
            "lui".CPUTestSingle(0xDEAD, Immediate).Expect(0xFFFFFFFFDEAD0000).Run();

            "lb".CPUTestDMem(0, 0xDE).Expect(0xFFFFFFFFFFFFFFDE).Run();
            "lbu".CPUTestDMem(0, 0xDE).Expect(0xDE).Run();

            "lh".CPUTestDMem(0, 0xDE, 0xAD).Expect(0xFFFFFFFFFFFFDEAD).Run();
            "lhu".CPUTestDMem(0, 0xDE, 0xAD).Expect(0xDEAD).Run();

            "lw".CPUTestDMem(0, 0xDE, 0xAD, 0xBE, 0xEF).Expect(0xFFFFFFFFDEADBEEF).Run();
            "lwu".CPUTestDMem(0, 0xDE, 0xAD, 0xBE, 0xEF).Expect(0xDEADBEEF).Run();

            "ld".CPUTestDMem(0, 0xDE, 0xAD, 0xBE, 0xEF, 0xDE, 0xAF, 0xDE, 0xED).Only64().Expect(0xDEADBEEFDEAFDEED).Run();
                 CPUTestDMem(0, 0x00).Only32().Except(ExceptionType.Reserved).Run();

            "lwl".CPUTestDMem(0, 0xDE, 0xAD, 0xBE, 0xEF).Expect(0xFFFFFFFFDEADBEEF).Run();
                  CPUTestDMem(1, 0xDE, 0xAD, 0xBE, 0xEF).Expect(0x00ADBEEF).Run();
                  CPUTestDMem(2, 0xDE, 0xAD, 0xBE, 0xEF).Expect(0x0000BEEF).Run();
                  CPUTestDMem(3, 0xDE, 0xAD, 0xBE, 0xEF).Expect(0x000000EF).Run();

            /* The hardware test passes on this instruction so this test unit was updated to pass but very confusing */
            "lwr".CPUTestDMem(0, 0xDE, 0xAD, 0xBE, 0xEF).Expect(0x000000DE).Run();
                  CPUTestDMem(1, 0xDE, 0xAD, 0xBE, 0xEF).Expect(0x00DEAD00).Run();
                  CPUTestDMem(2, 0xDE, 0xAD, 0xBE, 0xEF).Expect(0xFFFFFFFFADBE00DE).Run();
                  CPUTestDMem(3, 0xDE, 0xAD, 0xBE, 0xEF).Expect(0xFFFFFFFFEFDEADBE).Run();
        }
    }
}
