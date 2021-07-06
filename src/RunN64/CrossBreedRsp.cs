using System.Linq;
using System.Text;
using System.Net;
using System;
using cor64;
using cor64.Mips;
using cor64.Mips.Rsp;
using System.IO;
using cor64.RCP;
using NLog;
using System.Runtime.InteropServices;
using cor64.IO;

namespace RunN64 {
    public unsafe sealed class CrossBreedRsp : InterpreterBaseRsp
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly RspInterpreter m_CoreA;
        private readonly ModRsp m_CoreB;

        private readonly CAccumulator * m_CoreBAcc;
        private readonly CSURegs * m_CoreBRegs;

        private const int ACC_L = 0;
        private const int ACC_M = 1;
        private const int ACC_H = 2;

        private const uint PC_ALIGN_MASK = 0xFFC;

        private readonly RspVector[] m_VRegs = new RspVector[32];

        public CrossBreedRsp() : base(new Disassembler("o32"))
        {
            m_CoreB = new ModRsp();
            var sharedState = m_CoreB.GetPluginCoreState();

            for (int i = 0; i < m_VRegs.Length; i++) {
                m_VRegs[i] = new RspVectorCross((IntPtr)sharedState.VectorRegisters, i);
            }

            m_CoreBAcc = (CAccumulator *)sharedState.Accumulator;
            m_CoreBRegs = (CSURegs *)sharedState.SURegisters;

            m_CoreA = new RspInterpreter(m_VRegs);

            // m_CoreA = new RspInterpreter();

            m_Snapshot_VR = new RspVector[32];

            for (int i = 0; i < 32; i++) {
                m_Snapshot_VR[i] = new RspVector();
                m_Results_VR[i] = new RspVector();
            }
        }

        [StructLayoutAttribute(LayoutKind.Explicit, Pack = 128)]
        private struct CAccumulator {
            [FieldOffset(0)]
            public fixed ushort ACC[3 * 8];
        }

        [StructLayoutAttribute(LayoutKind.Explicit)]
        private struct CSURegs {
            [FieldOffset(0)]
            public fixed uint GPR[32];
        }

        private class RspVectorCross : RspVector
        {
            public RspVectorCross(IntPtr ptr, int index) : base(ptr, index) {
            }

            public override void PackedU8(int element, byte value) {
                m_ElementsPtr->PACKED_U8[element ^ 1] = value;
            }

            public override byte PackedU8(int element) {
                return m_ElementsPtr->PACKED_U8[element ^ 1];
            }

            public override ushort PackedU16(int element)
            {
                return m_ElementsPtr->PACKED_U16[element];
            }

            public override short PackedS16(int element) {
                return m_ElementsPtr->PACKED_S16[element];
            }

            public override void PackedS16(int element, short value) {
                m_ElementsPtr->PACKED_S16[element] = value;
            }

            public override void PackedU16(int element, ushort value)
            {
                m_ElementsPtr->PACKED_U16[element] = value;
            }

            public override uint PackedU32(int element)
            {
                return m_ElementsPtr->PACKED_U32[element];
            }

            public override void PackedU32(int element, uint value)
            {
                m_ElementsPtr->PACKED_U32[element] = value;
            }

            public override ulong PackedU64(int element)
            {
                return m_ElementsPtr->PACKED_U64[element];
            }

            public override void PackedU64(int element, ulong value)
            {
                m_ElementsPtr->PACKED_U64[element] = value;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < 8; i++) {
                    sb.Append(m_ElementsPtr->PACKED_U16[i].ToString("X4"));
                }

                return sb.ToString();
            }
        }

        private class CVecHiLoFlag {
            private readonly ushort * lo;
            private readonly ushort * hi;

            public CVecHiLoFlag(ushort * lo, ushort * hi) {
                this.lo = lo;
                this.hi = hi;
            }

            public ushort Value {
                get => (ushort)(((byte)*hi << 8) | (byte)*lo);
                set {
                    *lo = (byte)value;
                    *hi = (byte)(value >> 8);
                }
            }
        }


        public override ExecutionState State => m_CoreA.State;

        private void StepCoreA(DecodedInstruction inst) {
            var call = m_CoreA.GetOpcodeHandler(inst);
            call(inst);
        }

        private void StepCoreB(DecodedInstruction inst) {

            m_CoreB.SafeSetPC(m_CoreA.ReadPC());

            for (int i = 0; i < 8; i++) {
                WriteCoreBAcc(ACC_H, i, m_CoreA.Acc.Lo(i));
                WriteCoreBAcc(ACC_M, i, m_CoreA.Acc.Mi(i));
                WriteCoreBAcc(ACC_L, i, m_CoreA.Acc.Hi(i));
            }

            m_CoreB.WriteVCE(m_CoreA.ReadVCE());
            m_CoreB.WriteVC0(m_CoreA.ReadVCO());
            m_CoreB.WriteVCC(m_CoreA.ReadVCC());

            for (int i = 0; i < 32; i++) {
                m_CoreBRegs->GPR[i] = State.GetGpr32(i);
                // m_VRegs[i].ReverseElements();
            }

            m_CoreB.CoreSingleStep();

            for (int i = 0; i < 8; i++) {
                m_CoreA.Acc.Lo(i, ReadCoreBAcc(ACC_H, i));
                m_CoreA.Acc.Mi(i, ReadCoreBAcc(ACC_M, i));
                m_CoreA.Acc.Hi(i, ReadCoreBAcc(ACC_L, i));
            }

            m_CoreA.WriteVCE(m_CoreB.ReadVCE());
            m_CoreA.WriteVC0(m_CoreB.ReadVCO());
            m_CoreA.WriteVCC(m_CoreB.ReadVCC());

            m_CoreA.SafeSetPC(m_CoreB.ReadPC());

            for (int i = 0; i < 32; i++) {
                State.SetGpr32(i, m_CoreBRegs->GPR[i]);
                // m_VRegs[i].ReverseElements();
            }
        }

        private readonly ushort[][] m_Snapshot_ACC = {
            new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        private readonly ushort[][] m_Results_ACC = {
            new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0 },
            new ushort[] { 0, 0, 0, 0, 0, 0, 0, 0 }
        };

        private ulong m_Snapshot_PC = 0;
        private ushort m_Snapshot_VC0 = 0;
        private ushort m_Snapshot_VCC = 0;
        private ushort m_Snapshot_VCE = 0;

        private readonly RspVector[] m_Snapshot_VR = new RspVector[32];
        private readonly RspVector[] m_Results_VR = new RspVector[32];

        public void StepCompareCore(DecodedInstruction inst) {
            // Avoid callling Load/Store for comparisons
            // use this only for VU math, don't feel like snapshoting GRPs lol

            // Make a data snapshot
            var snapshot_PC  = m_CoreA.ReadPC();
            var snapshot_Vc0 = m_CoreA.ReadVCO();
            var snapshot_Vce = m_CoreA.ReadVCE();
            var snapshot_Vcc = m_CoreA.ReadVCC();

            for (int i = 0; i < 8; i++) {
                m_Snapshot_ACC[0][i] = m_CoreA.Acc.Lo(i);
                m_Snapshot_ACC[1][i] = m_CoreA.Acc.Mi(i);
                m_Snapshot_ACC[2][i] = m_CoreA.Acc.Hi(i);
            }

            for (int i = 0; i < 32; i++)
                m_VRegs[i].CopyTo(m_Snapshot_VR[i]);

            ///////////////////////////////////////

            /// First Step Core A
            StepCoreA(inst);

            // snapshot core A results VU results

            for (int i = 0; i < 8; i++) {
                m_Results_ACC[0][i] = m_CoreA.Acc.Lo(i);
                m_Results_ACC[1][i] = m_CoreA.Acc.Mi(i);
                m_Results_ACC[2][i] = m_CoreA.Acc.Hi(i);
            }

            for (int i = 0; i < 32; i++)
                m_VRegs[i].CopyTo(m_Results_VR[i]);

            var results_A_Vc0 = m_CoreA.ReadVCO();
            var results_A_Vce = m_CoreA.ReadVCE();
            var results_A_Vcc = m_CoreA.ReadVCC();

            /////////////////////////////////

            /// Roll back the VU state
            for (int i = 0; i < 8; i++) {
                m_CoreA.Acc.Lo(i, m_Snapshot_ACC[0][i]);
                m_CoreA.Acc.Mi(i, m_Snapshot_ACC[1][i]);
                m_CoreA.Acc.Hi(i, m_Snapshot_ACC[2][i]);
            }

            for (int i = 0; i < 32; i++)
                m_Snapshot_VR[i].CopyTo(m_VRegs[i]);

            m_CoreA.WriteVC0(snapshot_Vc0);
            m_CoreA.WriteVCE(snapshot_Vce);
            m_CoreA.WriteVCC(snapshot_Vcc);

            m_CoreA.SafeSetPC(snapshot_PC);
            m_CoreB.SafeSetPC(snapshot_PC);

            //////////////////////////////////

            StepCoreB(inst);

            // Compare VU results between A and B

            void PrintState() {
                Console.WriteLine("DISM: {0}", Disassembler.GetFullDisassembly(inst));
                PrintAcc(m_Snapshot_ACC);
                PrintVRegs(m_Snapshot_VR);
                Console.WriteLine("VC0: {0:X4}", m_Snapshot_VC0);
            }

            // if (results_A_Vcc != m_CoreA.ReadVCC()) {
            //     PrintState();
            //     Console.WriteLine("A: {0:X4} != B: {1:X4}", results_A_Vcc, m_CoreA.ReadVCC());
            //     throw new Exception("RSP core compare mismatch: VCC");
            // }

            for (int i = 0; i < 32; i++) {
                for (int j = 0; j < 8; j++) {
                    if (m_Results_VR[i].PackedU16(j) != m_VRegs[i].PackedU16(j)) {
                        PrintState();
                        Console.WriteLine("A: v{0:D2} {1} != B: v{0:D2} {2} @ e{3}", i, m_Results_VR[i].ToString(), m_VRegs[i].ToString(), j);
                        throw new Exception("RSP core compare mismatch: VU Regs");
                    }
                }
            }
        }

        public void DummyStep(DecodedInstruction inst) {
            PC += 4;
            PC &= PC_ALIGN_MASK;
            m_CoreA.SafeSetPC(PC);
            m_CoreB.SafeSetPC(PC);
        }

        private void WriteCoreBAcc(int level, int index, ushort val) {
            m_CoreBAcc->ACC[(level * 8) + index] = val;
        }

        private ushort ReadCoreBAcc(int level, int index) {
            return m_CoreBAcc->ACC[(level * 8) + index];
        }

        public override void Add(DecodedInstruction inst) => StepCoreA(inst);

        public override void AttachBootManager(BootManager bootManager)
        {
            throw new NotSupportedException();
        }

        public override void BitwiseLogic(DecodedInstruction inst) => StepCoreA(inst);

        public override void Branch(DecodedInstruction inst) => StepCoreA(inst);

        public override void Break(DecodedInstruction inst) => StepCoreA(inst);

        public override void Init()
        {
            m_CoreA.Init();
            m_CoreB?.Init();

            m_CoreA.CallHack = (decoded) => {
                return CallTable[decoded];
            };
        }

        public override void Jump(DecodedInstruction inst)  => StepCoreA(inst);

        public override void Load(DecodedInstruction inst)  => StepCoreA(inst);

        public override void ManualStart(ulong pc)
        {
            throw new NotSupportedException();
        }

        public override ushort ReadVCC() => m_CoreA.ReadVCC();

        public override byte ReadVCE() => m_CoreA.ReadVCE();

        public override ushort ReadVCO() => m_CoreA.ReadVCO();

        public override void SetOnLessThan(DecodedInstruction inst) => StepCoreA(inst);

        public override void Shift(DecodedInstruction inst) => StepCoreA(inst);

        public override void Step()
        {
            m_CoreA.Step();
        }

        public override void Store(DecodedInstruction inst) => StepCoreA(inst);

        public override void Subtract(DecodedInstruction inst)  => StepCoreA(inst);

        public override void TransferReg(DecodedInstruction inst) {
            StepCoreA(inst);
            

            // bool source_yes = false;
            // bool target_yes = false;

            // /* Source value to copy */
            // switch (inst.Op.XferSource)
            // {
            //     case RegBoundType.Gpr:
            //         {
            //             source_yes = true;
            //             break;
            //         }

            //     case RegBoundType.Cp0:
            //         {
            //             source_yes = true;
            //             break;
            //         }

            //     case RegBoundType.Cp2:
            //         {
            //             source_yes = true;
            //             break;
            //         }

            //     case RegBoundType.Cp2Ctl:
            //         {
            //             source_yes = true;
            //             break;
            //         }

            //     default: throw new NotSupportedException();
            // }

            // /* Target */
            // switch (inst.Op.XferTarget)
            // {
            //     case RegBoundType.Gpr:
            //         {
            //             target_yes = true;
            //             break;
            //         }

            //     case RegBoundType.Cp0:
            //         {
            //             target_yes = true;
            //             break;
            //         }

            //     case RegBoundType.Cp2:
            //         {
            //             target_yes = true;
            //             break;
            //         }

            //     case RegBoundType.Cp2Ctl:
            //         {
            //             target_yes = true;
            //             break;
            //         }

            //     default: throw new NotSupportedException();
            // }

            // if (source_yes && target_yes) {
            //     StepCoreA(inst);
            // }
            // else {
            //     StepCoreB(inst);
            // }
        }


        /***************************
         VU Opcodes
        ****************************/

        public override void VectorAccumulatorReadWrite(DecodedInstruction inst) => StepCoreA(inst);

        public override void VectorAdd(DecodedInstruction inst) => StepCoreA(inst);

        public override void VectorBitwise(DecodedInstruction inst)  => StepCoreA(inst);

        public override void VectorClip(DecodedInstruction inst)  => StepCoreA(inst);

        public override void VectorCompare(DecodedInstruction inst)  => StepCoreA(inst);

        public override void VectorLoad(DecodedInstruction inst)  => StepCoreA(inst);

        public override void VectorMove(DecodedInstruction inst)  => StepCoreA(inst);

        public override void VectorMultiply(DecodedInstruction inst) => StepCoreA(inst);

        public override void VectorReciprocal(DecodedInstruction inst)  => StepCoreA(inst);

        public override void VectorStore(DecodedInstruction inst) {
            // if (inst.Op.Op == "sdv") {
            //     StepCoreA(inst);
            // }
            // else {
            //     StepCoreB(inst);
            // }

            StepCoreA(inst);
            // StepCoreB(inst);
        }

        public override void VectorSubtract(DecodedInstruction inst)  => StepCoreA(inst);

        public override void VectorUnitReserved(DecodedInstruction inst)  => StepCoreA(inst);

        /*******************************/

        public override void WriteVCC(ushort vcc)
        {
            throw new NotSupportedException();
        }

        public override void WriteVC0(ushort vc0)
        {
            throw new NotSupportedException();
        }

        public override void AttachInterface(MipsInterface rcpInterface, SPInterface iface, DPCInterface rdpInterface) {
            base.AttachInterface(rcpInterface, iface, rdpInterface);
            m_CoreA.AttachInterface(rcpInterface, iface, rdpInterface);
            m_CoreB?.AttachInterface(rcpInterface, iface, rdpInterface);
        }

        public override void AttachDStream(Stream memoryStream)
        {
            m_CoreA.AttachDStream(memoryStream);
            m_CoreB?.AttachDStream(memoryStream);
        }

        public override void AttachIStream(Stream memoryStream)
        {
            base.AttachIStream(memoryStream);
            m_CoreA.AttachIStream(memoryStream);
            m_CoreB?.AttachIStream(memoryStream);
        }

        private void PrintVRegs() {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("// VU State Dump");

            for (int i = 0; i < m_VRegs.Length; i++)
                sb.AppendLine(String.Format("v{0:D2} {1}", i, m_VRegs[i].ToString()));

            sb.AppendLine("////////");

            Console.WriteLine(sb.ToString());
        }

        private void PrintVRegs(RspVector[] vectors) {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("// VU Registers");

            for (int i = 0; i < vectors.Length; i++)
                sb.AppendLine(String.Format("v{0:D2} {1}", i, vectors[i].ToString()));

            sb.AppendLine("////////");

            Console.WriteLine(sb.ToString());
        }

        private void PrintAcc(ushort[][] acc) {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("// VU Accumulator");

            for (int i = 0; i < 8; i++) {
                sb.Append(acc[0][i].ToString("X4"));
            }

            sb.AppendLine();

            for (int i = 0; i < 8; i++) {
                sb.Append(acc[1][i].ToString("X4"));
            }

            sb.AppendLine();

            for (int i = 0; i < 8; i++) {
                sb.Append(acc[2][i].ToString("X4"));
            }

            sb.AppendLine();

            sb.AppendLine("////////");

            Console.WriteLine(sb.ToString());
        }

        public override string Description => "RSP Cross Core";

        public override bool IsHalted => m_CoreA.IsHalted;
    }
}