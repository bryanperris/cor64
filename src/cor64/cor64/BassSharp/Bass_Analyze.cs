using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace cor64.BassSharp
{
    public abstract partial class Bass
    {
        bool Analyze()
        {
            m_BlockStack.Clear();
            m_Ip = 0;

            while (m_Ip < m_Program.Count) {
                Instruction i = m_Program[m_Ip++];
                if (!AnalyzeInstruction(i))
                    throw new Error("Unrecognized directive: " + i.statement);
            }

            return true;
        }

        private enum BlockMatchType
        {
            Pattern,
            MatchAndTrim,
            Equals
        }

        private enum BlockOp
        {
            Push,
            Update
        }

        private void PushBlock(String type)
        {
            int ip = m_Ip - 1;
            m_BlockStack.Push(new Block(ip, type));
            Log.Trace("Block Enter: {0}#{1:X8}", type, ip);
        }

        private void PopBlock()
        {
            var block = m_BlockStack.Pop();
            Log.Trace("Block Leave: {0}#{1:X8}", block.Type, block.Ip);
        }

        private bool BlockAction(BlockOp op, Instruction inst, String type, BlockMatchType matchMethod, params String[] args)
        {
            if (inst.statement.Contains("while"))
                Console.ResetColor();

            String statement = inst.statement;

            switch (matchMethod)
            {
                case BlockMatchType.Equals:
                    {
                        if (statement != args[0])
                        {
                            return false;
                        }

                        break;
                    }
                case BlockMatchType.Pattern:
                    {
                        bool match = false;

                        if (args != null && args.Length > 0)
                        {
                            for (int i = 0; i < args.Length; i++)
                                match |= statement.Match(args[i]);
                        }
                        else
                        {
                            match = statement.Match(type);
                        }

                        if (!match) return false;

                        break;
                    }
                case BlockMatchType.MatchAndTrim:
                    {
                        if (statement.MatchAndTrimBoth(args[0], args[1], true) == null)
                            return false;

                        break;
                    }
                default: return false;
            }

            switch (op)
            {
                case BlockOp.Push:
                    {
                        PushBlock(type);

                        if (matchMethod == BlockMatchType.Equals)
                        {
                            inst.statement = type + " " + args[0];
                        }

                        break;
                    }
                case BlockOp.Update:
                    {
                        int ip = m_Ip - 1;
                        var prevBlock = m_BlockStack.Peek();
                        m_Program[prevBlock.Ip].ip = ip;
                        prevBlock.Ip = ip;
                        Log.Trace("Block update: @{0}: {1}#{2:X8}", type, prevBlock.Type, ip);
                        break;
                    }
                default: return false;
            }


            return true;
        }

        bool AnalyzeInstruction(Instruction instruction)
        {

            /* Check for mismatched blocks */
            if (instruction.statement == "}" && m_BlockStack.Count == 0)
            {
                throw new Error(
                    "}} without matching {{ @{0}:{1}:{2}",
                    m_Sources[instruction.fileNumber], instruction.lineNumber, instruction.blockNumber);
            }


            if (BlockAction(BlockOp.Push, instruction, "scope", BlockMatchType.Pattern, "scope .*{", "scope {")) return true;
            if (BlockAction(BlockOp.Push, instruction, "macro", BlockMatchType.Pattern, "macro .*{")) return true;
            if (BlockAction(BlockOp.Push, instruction, "while", BlockMatchType.MatchAndTrim, "while ", " {")) return true;
            if (BlockAction(BlockOp.Update, instruction, "else", BlockMatchType.Pattern, "} else {")) return true;
            if (BlockAction(BlockOp.Update, instruction, "elseif", BlockMatchType.MatchAndTrim, "} else if ", " {")) return true;
            if (BlockAction(BlockOp.Push, instruction, "constant", BlockMatchType.Pattern, "((.*:)|-|\\+) {")) return true;
            if (BlockAction(BlockOp.Push, instruction, "if", BlockMatchType.Pattern, "if .*{")) return true;
            if (BlockAction(BlockOp.Push, instruction, "block", BlockMatchType.Equals, "{")) return true;

            /* End of block matchings */
            if (instruction.statement == "}")
            {
                var block = m_BlockStack.Peek();

                switch (block.Type)
                {
                    case "block":
                    case "constant":
                    case "scope": instruction.statement = "} end" + block.Type; break;

                    case "if":
                    case "macro":
                        {
                            m_Program[block.Ip].ip = m_Ip;
                            instruction.statement = "} end" + block.Type;
                            break;
                        }

                    case "while":
                        {
                            m_Program[block.Ip].ip = m_Ip;
                            instruction.ip = block.Ip;
                            instruction.statement = "} end" + block.Type;
                            break;
                        }
                    default: break;
                }

                PopBlock();

                return true;
            }

            return true;
        }
    }
}