using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.IO;
using static cor64.Mips.R4300I.Operands;

namespace cor64.Mips.R4300I
{
    public class Disassembler : BaseDisassembler
    {
        public Disassembler(string abi, Mode mode) : base(abi, mode)
        {
        }

        protected override Opcode DecodeOpcode(BinaryInstruction inst)
        {
            return DecodeTable.OpcodeLookup(inst);
        }

        public override string GetFullDisassembly(DecodedInstruction inst)
        {
            var opcode = inst.Op;
            var bInst = inst.Inst;

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
                    operands = Operands.Decode(this.ABI, bInst, fmt);

                    /* Append some branch/jump info */
                    if (opcode.Family == OperationFamily.Branch)
                    {
                        /* Don't show target on jumps that use register based computations */
                        if (!opcode.Op.EndsWith("r"))
                        {
                            operands += String.Format(
                                " ----> 0x{0:X8}",
                                opcode.Op.StartsWith("j") ? ComputeJumpTarget(inst) : ComputeBranchTarget(inst));
                        }
                    }

                    /* Append debug symbol */
                    operands += " ";
                    operands += GetSymbol(inst.Address);
                    operands += " ";
                }

                var dis = op + " " + operands;

                if (dis == "sll zero,zero,0")
                {
                    return "nop";
                }

                return dis;

            }
            else
            {
                throw new InvalidOperationException("Invalid instruction");
            }
        }
    }
}
