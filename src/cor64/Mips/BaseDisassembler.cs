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
        private Stream m_InstStream;
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

        /// <summary>
        /// An instruction stream in big endian ordering
        /// </summary>
        /// <param name="stream"></param>
        public virtual void SetStreamSource(Stream stream)
        {
            #if LITTLE_ENDIAN
            m_InstStream = stream;
            #else
            m_InstStream = new Swap32Stream(stream);
            #endif

            m_InstBinaryReader = new BinaryReader(m_InstStream);
        }

        public String ABI { get; }

        protected abstract Opcode DecodeOpcode(BinaryInstruction inst);

        public virtual String GetFullDisassembly(DecodedInstruction inst)
        {
            var opcode = inst.Op;
            var bInst = inst.Inst;
            var strict = this.ABI == "strict";

            var symbol = GetSymbol(inst.Address);

            if (!String.IsNullOrEmpty(symbol)) {
                symbol = String.Format("<{0}> ", symbol);
            }

            if (opcode.Family != OperationFamily.Null)
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
                    operands = Operands.Decode(this.ABI, bInst, fmt, inst.Address, m_SymbolProvider);

                    /* Append some branch/jump info */
                    if (!strict && opcode.Family == OperationFamily.Branch)
                    {
                        /* Don't show target on jumps that use register based computations */
                        if (!opcode.Op.EndsWith("r") && !opcode.Op.StartsWith("eret"))
                        {
                            if (String.IsNullOrEmpty(symbol)) {
                                operands += String.Format(
                                    " ----> 0x{0:X8}",
                                    opcode.Op.StartsWith("j") ? ComputeJumpTarget(inst) : ComputeBranchTarget(inst));
                            }
                        }
                    }
                }

                if (op == "sll" && inst.Source == 0 && inst.Target == 0 && inst.Destination == 0)
                {
                    op = "nop";
                    operands = "";
                }

                return String.Format("{0}{1} {2}", symbol, op, operands).Trim();
            }
            else
            {
                throw new InvalidOperationException("Invalid instruction");
            }
        }

        public String GetSymbol(ulong address, bool vmem = true)
        {
            /* We must clamp the virtual address to what ELF expects */
            if (vmem && (address & 0xF0000000UL) == 0xA0000000UL)
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

        public String GetLabel(ulong address, bool vmem = true) {
            /* We must clamp the virtual address to what ELF expects */
            if (vmem && (address & 0xF0000000UL) == 0xA0000000UL)
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

        private DecodedInstruction DecodeBinaryInst(long address)
        {
            m_InstStream.Position = address;
            var read = m_InstBinaryReader.ReadUInt32();
            return Decode((ulong)m_InstStream.Position, (ulong)m_InstStream.Length, read);
        }

        public DecodedInstruction Decode(ulong address, ulong bound, uint inst) {
            BinaryInstruction binst = new(inst);

            return new DecodedInstruction(
                address,
                DecodeOpcode(binst),
                binst,
                false,
                address + 4 >= bound);
        }

        public DecodedInstruction Disassemble(ulong address)
        {
            // If the stream position goes out of range, return internal null opcode
            if (address >= (ulong)m_InstStream.Length)
                return new DecodedInstruction(0, new Opcode(), new BinaryInstruction(), true, false);

            // Do the normal opcode decoding
            return DecodeBinaryInst((long)address);
        }

        public DecodedInstruction[] Disassemble(ulong address, int count)
        {
            DecodedInstruction[] disassembly = new DecodedInstruction[count];

            for (int i = 0; i < count; i++)
            {
                disassembly[i] = Disassemble(address + (uint)(i * 4));
            }

            return disassembly;
        }

        protected static ulong ComputeJumpTarget(DecodedInstruction inst)
        {
            return CoreUtils.ComputeTargetPC(false, inst.Address, 0, inst.Inst.target);
        }

        protected static ulong ComputeBranchTarget(DecodedInstruction inst)
        {
            return CoreUtils.ComputeBranchPC(false, inst.Address, CoreUtils.ComputeBranchTargetOffset(inst.Inst.imm));
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
