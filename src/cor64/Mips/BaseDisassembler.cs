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
    /// Decode BinaryInstructions into DecodedInstructions
    /// </summary>
    public abstract class BaseDisassembler
    {
        private Stream m_BinaryStream;
        private ulong m_BaseAddress;
        private readonly String m_Abi;
        private ISymbolProvider m_SymbolProvider;
        private readonly byte[] m_InstBuffer = new Byte[4];
        private Swap32Stream m_SwapStream;
        private readonly Func<uint> m_ReadInstFunc;

        private static readonly String[] Cop1ConditionalTable = {
            "f", "un", "eq", "olt", "ult", "ole", "ule", "sf",
            "ngle", "sqe", "ngl", "lt", "nge", "le", "ngt"
        };

        protected BaseDisassembler(String abi)
        {
            m_Abi = abi;

            if (CoreConfig.Current.ByteSwap)
            {
                m_ReadInstFunc = () =>
                {
                    m_SwapStream.Read(m_InstBuffer, 0, 4);
                    return m_InstBuffer.ToUInt32();
                };
            }
            else
            {
                m_ReadInstFunc = () =>
                {
                    m_BinaryStream.Read(m_InstBuffer, 0, 4);
                    return m_InstBuffer.ToUInt32();
                };
            }
        }

        public virtual void SetStreamSource(Stream stream)
        {
            m_BinaryStream = stream;
            m_SwapStream = new Swap32Stream(stream);
        }

        public ulong CurrentAddress => m_BaseAddress;

        public String ABI => m_Abi;

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

        private DecodedInstruction _Disassemble()
        {
            DecodedInstruction decoded;

            /* Important note: When changing PC it becomes out of sync with the stream's position
             * So we need a better a way to handle so that PC jumps only update the Stream's position
             * else in sequencial IO, let the stream handle it basically */
            m_BinaryStream.Position = (long)m_BaseAddress;

            BinaryInstruction inst = new BinaryInstruction(m_ReadInstFunc());

            decoded = new DecodedInstruction(m_BaseAddress, DecodeOpcode(inst), inst, false, m_BinaryStream.Position + 4 >= m_BinaryStream.Length);

            return decoded;
        }

        public DecodedInstruction Disassemble(ulong address)
        {
            // if (address < 0x80000000) 
            //     Console.WriteLine("PC Alert: " + address.ToString("X8"));

            if (address < 0 || address >= (ulong)m_BinaryStream.Length)
                return new DecodedInstruction(0, new Opcode(), new BinaryInstruction(), true, false);

            m_BaseAddress = address;
            return _Disassemble();
        }

        public DecodedInstruction[] Disassemble(ulong address, int count)
        {
            DecodedInstruction[] disassembly = new DecodedInstruction[count];
            m_BaseAddress = address;

            for (int i = 0; i < count; i++)
            {
                disassembly[i] = _Disassemble();
            }

            return disassembly;
        }

        protected ulong ComputeJumpTarget(DecodedInstruction inst)
        {
            return CoreUtils.ComputeTargetPC(false, inst.Address, 0, inst.Inst.target);
        }

        protected ulong ComputeBranchTarget(DecodedInstruction inst)
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
            if (inst.fc >= 0 && inst.fc <= 15 && inst.fmtType != FpuValueType.Reserved)
                return Cop1ConditionalTable[inst.fc];

            else
                return "cond";
        }
    }
}
