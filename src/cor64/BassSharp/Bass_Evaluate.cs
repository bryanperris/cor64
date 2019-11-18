using System;
using System.Collections.Generic;
using System.Linq;
using cor64.BassSharp.Eval;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace cor64.BassSharp
{
    public abstract partial class Bass
    {
        protected long Evaluate(String expression, Evaluation mode = Evaluation.Default)
        {
            String name = null;

            if (expression == "--") name = String.Format("lastLabel#{0}", m_LastLabelCounter - 2);
            if (expression == "-") name = String.Format("lastLabel#{0}", m_LastLabelCounter - 1);
            if (expression == "+") name = String.Format("nextLabel#{0}", m_NextLabelCounter + 0);
            if (expression == "++") name = String.Format("nextLabel#{0}", m_NextLabelCounter + 1);

            if (name != null) {
                Constant constant = FindConstant(name);
                if (constant != null)
                    return constant.Value;

                if (IsQueryPhase)
                    return Pc;

                throw new Error("relative label not declared");
            }

            try {
                Node node = Parser.Parse(expression);
                return Evaluate(node, mode);
            }
            catch (InvalidOperationException e) {
                throw new Error("Malformed expression: " + e.Message + ": " + expression);
            }
        }

        long Evaluate(Node node, Evaluation mode)
        {
            long p(int v)
            {
                return Evaluate(node.Link[v], mode);
            }

            switch (node.NodeType) {
                case Node.Type.Function: return EvaluateFunction(node, mode);
                case Node.Type.Literal: return EvaluateLiteral(node, mode);
                case Node.Type.LogicalNot: return p(0) == 0 ? 1 : 0;
                case Node.Type.BitwiseNot: return ~p(0);
                case Node.Type.Positive: return +p(0);
                case Node.Type.Negative: return -p(0);
                case Node.Type.Multiply: return p(0) * p(1);
                case Node.Type.Divide: return p(0) / p(1);
                case Node.Type.Modulo: return p(0) % p(1);
                case Node.Type.Add: return p(0) + p(1);
                case Node.Type.Subtract: return p(0) - p(1);
                case Node.Type.ShiftLeft: return p(0) << (int)p(1);
                case Node.Type.ShiftRight: return p(0) >> (int)p(1);
                case Node.Type.BitwiseAnd: return p(0) & p(1);
                case Node.Type.BitwiseOr: return p(0) | p(1);
                case Node.Type.BitwiseXor: return p(0) ^ p(1);
                case Node.Type.Equal: return p(0) == p(1) ? 1 : 0;
                case Node.Type.NotEqual: return p(0) != p(1) ? 1 : 0;
                case Node.Type.LessThanEqual: return p(0) <= p(1) ? 1 : 0;
                case Node.Type.GreaterThanEqual: return p(0) >= p(1) ? 1 : 0;
                case Node.Type.LessThan: return p(0) < p(1) ? 1 : 0;
                case Node.Type.GreaterThan: return p(0) > p(1) ? 1 : 0;
                case Node.Type.LogicalAnd: return p(0) == 1 ? p(1) : 0;
                case Node.Type.LogicalOr: return p(0) == 0 ? p(1) : 1;
                case Node.Type.Condition: return p(0) == 1 ? p(1) : p(2);
                case Node.Type.Assign: return EvaluateAssign(node, mode);
            }

            throw new Error("Unsupported operator");
        }

        IList<long> EvaluateParameters(Node node, Evaluation mode)
        {
            List<long> result = new List<long>();

            if (node.NodeType == Node.Type.Null)
                return result;

            if (node.NodeType == Node.Type.Separator) {
                result.Add(Evaluate(node, mode));
                return result;
            }

            foreach (Node link in node.Link) {
                result.Add(Evaluate(link, mode));
            }

            return result;
        }

        long EvaluateFunction(Node node, Evaluation mode)
        {
            var p = EvaluateParameters(node.Link[1], mode);
            var s = String.Format("{0}:{1}", node.Link[0].Literal, p.Count);

            if (s == "origin:0") return m_Origin;
            if (s == "base:0") return m_Base;
            if (s == "pc:0") return Pc;

            if (s == "putchar:1") {
                if (IsWritePhase) Trace.Write(p[0]);
                return p[0];
            }

            throw new Error("Unrecognized function: " + s);
        }

        long EvaluateLiteral(Node node, Evaluation mode)
        {
            String s = node.Literal;
            Variable variable;
            Constant constant;

            if (node.Literal.Length < 1) return 0;

            if (s.Length > 1 && s[0] == '0' && s[1] == 'b') return Convert.ToInt64(s.Substring(2), 2);
            if (s.Length > 1 && s[0] == '0' && s[1] == 'o') return Convert.ToInt64(s.Substring(2), 8);
            if (s.Length > 1 && s[0] == '0' && s[1] == 'x') return Convert.ToInt64(s.Substring(2), 16);
            if (s[0] >= '0' && s[0] <= '9') return Convert.ToInt64(s);
            if (s[0] == '%') return Convert.ToInt64(s.Substring(1), 2);
            if (s[0] == '$') return Convert.ToInt64(s.Substring(1), 16);
            if (s.Match("'.*'")) return Character(s);

            if ((variable = FindVariable(s)) != null) return variable.Value;
            if ((constant = FindConstant(s)) != null) return constant.Value;
            if (mode != Evaluation.Strict && IsQueryPhase) return Pc;

            throw new Error("Unrecognized variable: " + s);
        }

        long EvaluateAssign(Node node, Evaluation mode)
        {
            String s = node.Link[0].Literal;
            Variable variable = null;

            if ((variable = FindVariable(s)) != null) {
                variable.Value = Evaluate(node.Link[1], mode);
                return variable.Value;
            }

            throw new Error("Unrecognized variable");
        }
    }
}