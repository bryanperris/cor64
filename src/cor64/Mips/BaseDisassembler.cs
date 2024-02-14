using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.IO;

using static cor64.Mips.Operands;

namespace cor64.Mips
{
    /// <summary>
    /// /// Decode BinaryInstructions into DecodedInstructions
    /// </summary>
    public abstract class BaseDisassembler
    {
        private ISymbolProvider m_SymbolProvider;
        private readonly byte[] m_InstBuffer = new Byte[4];

        private BinaryReader m_InstBinaryReader;
        private static readonly String[] Cop1ConditionalTable = {
            "f", "un", "eq", "olt", "ult", "ole", "ule", "sf",
            "ngle", "sqe", "ngl", "lt", "nge", "le", "ngt"
        };
        
        protected BaseDisassembler(String abi)
        {
            ABI = abi;
        }

        public String ABI { get; }

        protected abstract Opcode DecodeOpcode(BinaryInstruction inst);

        // Reads raw instruction from big endian memory into a native structure
        public BinaryInstruction DecodeBinary(uint value) {
            return new BinaryInstruction(value);
        }

        // Decode the instruction
        public DecodedInstruction DecodeInst(BinaryInstruction inst) {
            return new DecodedInstruction(DecodeOpcode(inst), inst);
        }

        public DecodedInstruction Decode(uint value) {
            return DecodeInst(DecodeBinary(value));
        }

        // Disassembles instruction ignoring its memory address
        public virtual String Disassemble(DecodedInstruction inst)
        {
            var opcode = inst.Op;
            var bInst = inst.Inst;

            if (opcode.Family != OperationFamily.Invalid)
            {
                String op = opcode.Op;
                String operands = "";

                op = op.Replace("condition", DecodeCop1Conditional(bInst));
                op = op.Replace("fmt", DecodeCop1Format(bInst));

                if (opcode.OperandFmt != OperandType.Empty)
                {
                    var fmt = Operands.GetFormat(opcode.OperandFmt);

                    if (opcode.Family == OperationFamily.Branch && op.EndsWith("z"))
                    {
                        fmt.ModifyFormat(fmt.Format.Replace(",rt", ""));
                    }

                    /* Decode operands */
                    operands = Operands.Decode(this.ABI, bInst, fmt, m_SymbolProvider);
                }

                if (op == "sll" && inst.Source == 0 && inst.Target == 0 && inst.Destination == 0)
                {
                    op = "nop";
                    operands = "";
                }

                return String.Format("{0} {1}", op, operands).Trim();
            }
            else
            {
                throw new InvalidOperationException("Invalid instruction");
            }
        }

        // Disassembles instruction including information related to its memory address
        public virtual String Disassemble(long address, DecodedInstruction inst)
        {
            var opcode = inst.Op;
            var strict = this.ABI == "strict";

            var symbol = GetSymbol(address);

            if (!String.IsNullOrEmpty(symbol)) {
                symbol = String.Format("<{0}> ", symbol);
            }

            var disassembled = Disassemble(inst);

            if (!strict && opcode.Family == OperationFamily.Branch)
            {
                /* Don't show target on jumps that use register based computations */
                if (!opcode.Op.EndsWith("r") && !opcode.Op.StartsWith("eret"))
                {
                    if (String.IsNullOrEmpty(symbol)) {
                        disassembled += String.Format(
                            " ----> 0x{0:X8}",
                            opcode.Op.StartsWith("j") ? ComputeJumpTarget(address, inst) : ComputeBranchTarget(address, inst));
                    }
                }
            }

            return String.Format("{0}{1}", symbol, disassembled);
        }

        public String GetSymbol(long address, bool vmem = true)
        {
            /* We must clamp the virtual address to what ELF expects */
            if (vmem && (address & 0xF0000000L) == 0xA0000000L)
            {
                address <<= 8;
                address >>= 8;
                address |= 0x80000000;
            }

            if (m_SymbolProvider != null)
            {
                var sym = m_SymbolProvider.GetSymbol(address);

                if (sym != null)
                {
                    return sym;
                }
            }

            return "";
        }

        public String GetLabel(long address, bool vmem = true) {
            /* We must clamp the virtual address to what ELF expects */
            if (vmem && (address & 0xF0000000L) == 0xA0000000L)
            {
                address <<= 8;
                address >>= 8;
                address |= 0x80000000;
            }

            if (m_SymbolProvider != null)
            {
                var sym = m_SymbolProvider.GetLabel(address);

                if (sym != null)
                {
                    return sym;
                }
            }

            return "";
        }

        public void AttachSymbolProvider(ISymbolProvider provider)
        {
            m_SymbolProvider = provider;
        }

        // private DecodedInstruction DecodeBinaryInst(long address)
        // {
        //     m_InstStream.Position = address;
        //     var read = m_InstBinaryReader.ReadUInt32();
        //     return Decode((ulong)m_InstStream.Position, (ulong)m_InstStream.Length, read);
        // }

        // private DecodedInstruction DecodeBinaryInst(long vaddr, long physaddr)
        // {
        //     m_InstStream.Position = physaddr;
        //     var read = m_InstBinaryReader.ReadUInt32();
        //     return Decode((ulong)vaddr, (ulong)m_InstStream.Length, read);
        // }

        // public DecodedInstruction Decode(ulong address, ulong bound, uint inst) {
        //     BinaryInstruction binst = new(inst);

        //     return new DecodedInstruction(
        //         address,
        //         DecodeOpcode(binst),
        //         binst,
        //         false,
        //         address + 4 >= bound);
        // }

        // public DecodedInstruction Decode(ulong address, uint inst) {
        //     BinaryInstruction binst = new(inst);

        //     return new DecodedInstruction(
        //         address,
        //         DecodeOpcode(binst),
        //         binst,
        //         false,
        //         false);
        // }

        // public DecodedInstruction Disassemble(ulong address)
        // {
        //     // If the stream position goes out of range, return internal null opcode
        //     if (address >= (ulong)m_InstStream.Length)
        //         return new DecodedInstruction(0, new Opcode(), new BinaryInstruction(), true, false);

        //     // Do the normal opcode decoding
        //     return DecodeBinaryInst((long)address);
        // }

        // public DecodedInstruction Disassemble(ulong vaddr, ulong phyaddr)
        // {
        //     // If the stream position goes out of range, return internal null opcode
        //     if (phyaddr >= (ulong)m_InstStream.Length)
        //         return new DecodedInstruction(0, new Opcode(), new BinaryInstruction(), true, false);

        //     // Do the normal opcode decoding
        //     return DecodeBinaryInst((long)vaddr, (long)phyaddr);
        // }


        public DecodedInstruction[] Disassemble(Stream stream, int count)
        {
            DecodedInstruction[] disassembly = new DecodedInstruction[count];

            BinaryReader reader = new BinaryReader(stream);

            for (int i = 0; i < count; i++)
            {
                disassembly[i] = Decode(reader.ReadUInt32());
            }

            return disassembly;
        }

        protected static long ComputeJumpTarget(long address, DecodedInstruction inst)
        {
            return CoreUtils.ComputeTargetPC(false, address, 0, inst.Inst.target);
        }

        protected static long ComputeBranchTarget(long address, DecodedInstruction inst)
        {
            return CoreUtils.ComputeBranchPC(false, address, CoreUtils.ComputeBranchTargetOffset(inst.Inst.imm));
        }

        protected static String DecodeCop1Format(BinaryInstruction inst)
        {
            switch (inst.fmtType)
            {
                case FpuValueType.FDouble: return "d";
                case FpuValueType.Doubleword: return "l";
                case FpuValueType.Word: return "w";
                case FpuValueType.FSingle: return "s";
                default: return "f";
            }
        }

        protected static String DecodeCop1Conditional(BinaryInstruction inst)
        {
            if (inst.fc <= 15 && inst.fmtType != FpuValueType.Reserved)
                return Cop1ConditionalTable[inst.fc];

            else
                return "cond";
        }
    }
}
