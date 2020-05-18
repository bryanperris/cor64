using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp.Eval
{
    internal static class Evaluator
    {
        public static String EvaluateExpression (Node node)
        {
            Func<int, String> p = (n) => {
                return EvaluateExpression(node.Link[n]);
            };

            switch(node.NodeType) {
                case Node.Type.Null: return "Null";
                case Node.Type.Literal: return "Literal:" + node.Literal;
                case Node.Type.Function: return String.Format("Function(0:{0}, 1:{1})", p(0), p(1));
                case Node.Type.Subscript: return String.Format("Subscript(0:{0}, 1:{1})", p(0), p(1));
                case Node.Type.Member: return String.Format("Member(0:{0}, 1:{1})", p(0), (1));
                case Node.Type.SuffixIncrement: return String.Format("SuffixIncrement(0:{0})", p(0));
                case Node.Type.SuffixDecrement: return String.Format("SuffixDecrement(0:{0})", p(0));
                case Node.Type.Reference: return String.Format("Reference(0:{0})", p(0));
                case Node.Type.Dereference: return String.Format("Dereference(0:{0})", p(0));
                case Node.Type.BitwiseNot: return String.Format("Complement(0:{0})", p(0));
                case Node.Type.PrefixIncrement: return String.Format("PrefixIncrement(0:{0})", p(0));
                case Node.Type.PrefixDecrement: return String.Format("PrefixDecrement(0:{0})", p(0));
                case Node.Type.Add: return String.Format("Add(0:{0}, 1:{1})", p(0), p(1));
                case Node.Type.Multiply: return String.Format("Multiply(0:{0}, 1:{1})", p(0), p(1));
                case Node.Type.Concatenate: return String.Format("Concatenate(0:{0}, {1})", p(0), p(1));
                case Node.Type.Coalesce: return String.Format("Coalesce(0:{0}, {1})", p(0), p(1));
                case Node.Type.Condition: return String.Format("Condition(0:{0}, {1})", p(0), p(1));
                case Node.Type.Assign: return String.Format("Assign(0:{0}, {1})", p(0), p(1));
                case Node.Type.Separator: {
                        String result = "Seperator(";

                        foreach (Node link in node.Link) {
                            result += EvaluateExpression(link) + ", ";
                        }

                        return result.Replace(", ", "") + ")";
                    }

                default: throw new Error("Invalid operator");
            }
        }

        public static long EvaluateInteger (Node node)
        {
            Func<int, long> p = (n) => {
                return EvaluateInteger(node.Link[n]);
            };

            Func<long, long, long> o = (x, y) => {
                return (x == 0 ? 0 : 1) | (y == 0 ? 0 : 1);
            };

            Func<long, long, long> a = (x, y) => {
                return (x == 0 ? 0 : 1) & (y == 0 ? 0 : 1);
            };

            if (node.NodeType == Node.Type.Literal) {
                if (node.Literal.StartsWith("0b")) return Convert.ToInt64(node.Literal.Substring(2), 2);
                if (node.Literal.StartsWith("0o")) return Convert.ToInt64(node.Literal.Substring(2), 8);
                if (node.Literal.StartsWith("0x")) return long.Parse(node.Literal.Substring(2), System.Globalization.NumberStyles.HexNumber);
                if (node.Literal.StartsWith("%")) return Convert.ToInt64(node.Literal.Substring(2), 2);
                if (node.Literal.StartsWith("$")) return long.Parse(node.Literal.Substring(2), System.Globalization.NumberStyles.HexNumber);
                return int.Parse(node.Literal);
            }

            switch (node.NodeType) {
                case Node.Type.SuffixIncrement: return p(0);
                case Node.Type.SuffixDecrement: return p(0);
                case Node.Type.LogicalNot: return p(0) == 1 ? 0 : 1;
                case Node.Type.BitwiseNot: return ~p(0);
                case Node.Type.Positive: return +p(0);
                case Node.Type.Negative: return -p(0);
                case Node.Type.PrefixIncrement: return p(0) + 1;
                case Node.Type.PrefixDecrement: return p(0) - 1;
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
                case Node.Type.LogicalAnd: return a(p(0), p(1));
                case Node.Type.LogicalOr: return o(p(0), p(1));
                case Node.Type.Condition: return p(0) == 0 ? p(2) : p(1);
                case Node.Type.Assign: return p(1);
                case Node.Type.AssignMultiply: return p(0) * p(1);
                case Node.Type.AssignDivide: return p(0) / p(1);
                case Node.Type.AssignModulo: return p(0) % p(1);
                case Node.Type.AssignAdd: return p(0) + p(1);
                case Node.Type.AssignSubtract: return p(0) - p(1);
                case Node.Type.AssignShiftLeft: return p(0) << (int)p(1);
                case Node.Type.AssignShiftRight: return p(0) >> (int)p(1);
                case Node.Type.AssignBitwiseAnd: return p(0) & p(1);
                case Node.Type.AssignBitwiseOr: return p(0) | p(1);
                case Node.Type.AssignBitwiseXor: return p(0) ^ p(1);

                default: throw new Error("Invalid operator");
            }
        }

        public static double EvaluateReal(Node node)
        {
            if (node.NodeType == Node.Type.Literal) return double.Parse(node.Literal);

            Func<int, double> p = (n) => {
                return EvaluateReal(node.Link[n]);
            };

            Func<double, double, double> o = (x, y) => {
                bool result = (x == 0.0d ? false : true) | (y == 0.0d ? false : true);
                return result ? 1.0d : 0.0d;
            };

            Func<double, double, double> a = (x, y) => {
                bool result = (x == 0.0d ? false : true) & (y == 0.0d ? false : true);
                return result ? 1.0d : 0.0d;
            };

            switch (node.NodeType) {
                case Node.Type.LogicalNot: return p(0) == 0.0d ? 1.0d : 0.0d;
                case Node.Type.Positive: return +p(0);
                case Node.Type.Negative: return -p(0);
                case Node.Type.Multiply: return p(0) * p(1);
                case Node.Type.Divide: return p(0) / p(1);
                case Node.Type.Add: return p(0) + p(1);
                case Node.Type.Subtract: return p(0) - p(1);
                case Node.Type.Equal: return p(0) == p(1) ? 1.0d : 0.0d;
                case Node.Type.NotEqual: return p(0) != p(1) ? 1.0d : 0.0d;
                case Node.Type.LessThanEqual: return p(0) <= p(1) ? 1.0d : 0.0d;
                case Node.Type.GreaterThanEqual: return p(0) >= p(1) ? 1.0d : 0.0d;
                case Node.Type.LessThan: return p(0) < p(1) ? 1.0d : 0.0d;
                case Node.Type.GreaterThan: return p(0) > p(1) ? 1.0d : 0.0d;
                case Node.Type.LogicalAnd: return a(p(0), p(1));
                case Node.Type.LogicalOr: return o(p(0), p(1));
                case Node.Type.Condition: return p(0) == 0.0d ? p(2) : p(1);
                case Node.Type.Assign: return p(1);
                case Node.Type.AssignMultiply: return p(0) * p(1);
                case Node.Type.AssignDivide: return p(0) / p(1);
                case Node.Type.AssignAdd: return p(0) + p(1);
                case Node.Type.AssignSubtract: return p(0) - p(1);
                default: throw new Error("Invalid operator");
            }
        }

        public static long? Integer(String expression)
        {
            try {
                var tree = new Node();
                Parser.Parse(ref tree, ref expression, 0);
                return EvaluateInteger(tree);
            }
            catch (InvalidOperationException) {
                return null;
            }
        }

        public static double? Real(String expression)
        {
            try {
                var tree = new Node();
                Parser.Parse(ref tree, ref expression, 0);
                return EvaluateReal(tree);
            }
            catch (InvalidOperationException) {
                return null;
            }
        }
    }
}
