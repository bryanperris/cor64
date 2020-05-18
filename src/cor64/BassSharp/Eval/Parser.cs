using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp.Eval
{
    internal static class Parser
    {
        private static Node UnaryPrefix(ref Node node, ref String s, Node.Type type, int seek, int depth)
        {
            var parent = new Node(type);
            var newNode = new Node();
            s = s.Substring(seek);
            Parse(ref newNode, ref s, depth);
            parent.Link.Add(newNode);
            node = parent;
            return node;
        }

        private static Node UnarySuffix(ref Node node, ref String s, Node.Type type, int seek, int depth)
        {
            var parent = new Node(type);
            s = s.Substring(seek);
            Parse(ref parent, ref s, depth);
            parent.Link.Add(node);
            node = parent;
            return node;
        }

        private static Node Binary(ref Node node, ref String s, Node.Type type, int seek, int depth)
        {
            var parent = new Node(type);
            var newNode = new Node();
            s = s.Substring(seek);
            Parse(ref newNode, ref s, depth);
            parent.Link.Add(node);
            parent.Link.Add(newNode);
            node = parent;
            return node;
        }

        private static Node Ternary(ref Node node, ref String s, Node.Type type, int seek, int depth)
        {
            var parent = new Node(type);
            var newNode = new Node();
            var newNode2 = new Node();
            var originalNode = node;

            /* Parse true side */
            s = s.Substring(seek);
            Parse(ref newNode, ref s, depth);

            if (s.Length == 0 || s[0] != ':')
                throw new Error("Mismatched ternary");

            /* Parse false side */
            s = s.Substring(seek);
            Parse(ref newNode2, ref s, depth);

            parent.Link.Add(originalNode);
            parent.Link.Add(newNode);
            parent.Link.Add(newNode2);

            node = parent;
            return node;
        }

        private static Node Seperator(ref Node node, ref String s, Node.Type type, int seek, int depth)
        {
            if (node.NodeType == Node.Type.Separator)
                return Binary(ref node, ref s, type, seek, depth);

            var parent = new Node(type);
            var newNode = new Node();
            s = s.Substring(seek);
            Parse(ref newNode, ref s, depth);
            parent.Link.Add(newNode);
            node = parent;
            return node;
        }

        public static Node Parse(String expression)
        {
            var node = new Node();
            Parse(ref node, ref expression, 0);
            return node;
        }

        public static void Parse(ref Node node, ref String s, int depth)
        {
            if (s.Length == 0)
                return;

            while (Char.IsWhiteSpace(s[0])) {
                s = s.Substring(1);
            }

            if (s[0] == '(' && node.Link.Count == 0) {
                s = s.Substring(1);
                Parse(ref node, ref s, 1);
                if (s.Length == 0 || s[0] != ')')
                    throw new Error("Mismatched group");

                s = s.Substring(1);
            }

            if (LiteralHelper.IsLiteral(s)) {
                node.NodeType = Node.Type.Literal;
                node.Literal = LiteralHelper.Literal(ref s);
            }

            Node localNode = node;

            bool p()
            {
                return
                    String.IsNullOrEmpty(localNode.Literal) &&
                    localNode.Link.Count == 0;
            }

            while (s.Length > 0) {
                while (Char.IsWhiteSpace(s[0])) s = s.Substring(1);

                if (depth >= 13) break;

                if (s[0] == '(' && !p()) {
                    localNode = Binary(ref node, ref s, Node.Type.Function, 1, 1);

                    if (s.Length == 0 || s[0] != ')')
                        throw new Error("Mismatched function");

                    s = s.Substring(1);
                    continue;
                }

                if (s[0] == '[') {
                    localNode = Binary(ref node, ref s, Node.Type.Subscript, 1, 1);

                    if (s.Length == 0 || s[0] != ']')
                        throw new Error("Mismatched subscript");

                    s = s.Substring(1);
                    continue;
                }

                if (s[0] == '.') {
                    localNode = Binary(ref node, ref s, Node.Type.Member, 1, 13); continue;
                }

                if (s.Length >= 2 && s[0] == '+' && s[1] == '+' && !p()) { localNode = UnarySuffix(ref node, ref s, Node.Type.SuffixIncrement, 2, 13); continue; }
                if (s.Length >= 2 && s[0] == '-' && s[1] == '-' && !p()) { localNode = UnarySuffix(ref node, ref s, Node.Type.SuffixDecrement, 2, 13); continue; }

                if (s[0] == '&' && p()) { localNode = UnaryPrefix(ref node, ref s, Node.Type.Reference, 1, 12); continue; }
                if (s[0] == '*' && p()) { localNode = UnaryPrefix(ref node, ref s, Node.Type.Dereference, 1, 12); continue; }
                if (s[0] == '!' && p()) { localNode = UnaryPrefix(ref node, ref s, Node.Type.LogicalNot, 1, 12); continue; }
                if (s[0] == '~' && p()) { localNode = UnaryPrefix(ref node, ref s, Node.Type.BitwiseNot, 1, 12); continue; }
                if (s.Length >= 2 && s[0] == '+' && s[1] != '+' && p()) { localNode = UnaryPrefix(ref node, ref s, Node.Type.Positive, 1, 12); continue; }
                if (s.Length >= 2 && s[0] == '-' && s[1] != '-' && p()) { localNode = UnaryPrefix(ref node, ref s, Node.Type.Negative, 1, 12); continue; }
                if (s.Length >= 2 && s[0] == '+' && s[1] == '+' && p()) { localNode = UnaryPrefix(ref node, ref s, Node.Type.PrefixIncrement, 2, 12); continue; }
                if (s.Length >= 2 && s[0] == '-' && s[1] == '-' && p()) { localNode = UnaryPrefix(ref node, ref s, Node.Type.PrefixDecrement, 2, 12); continue; }
                if (depth >= 12) break;

                if (depth >= 11) break;
                if (s.Length >= 2 && s[0] == '*' && s[1] != '=') { localNode = Binary(ref node, ref s, Node.Type.Multiply, 1, 11); continue; }
                if (s.Length >= 2 && s[0] == '/' && s[1] != '=') { localNode = Binary(ref node, ref s, Node.Type.Divide, 1, 11); continue; }
                if (s.Length >= 2 && s[0] == '%' && s[1] != '=') { localNode = Binary(ref node, ref s, Node.Type.Modulo, 1, 11); continue; }

                if (depth >= 10) break;
                if (s.Length >= 2 && s[0] == '+' && s[1] != '=') { localNode = Binary(ref node, ref s, Node.Type.Add, 1, 10); continue; }
                if (s.Length >= 2 && s[0] == '-' && s[1] != '=') { localNode = Binary(ref node, ref s, Node.Type.Subtract, 1, 10); continue; }

                if (depth >= 9) break;
                if (s.Length >= 4 && s[0] == '<' && s[1] == '<' && s[2] == '<' && s[3] != '=') { localNode = Binary(ref node, ref s, Node.Type.RotateLeft, 3, 9); continue; }
                if (s.Length >= 4 && s[0] == '>' && s[1] == '>' && s[2] == '>' && s[3] != '=') { localNode = Binary(ref node, ref s, Node.Type.RotateRight, 3, 9); continue; }
                if (s.Length >= 3 && s[0] == '<' && s[1] == '<' && s[2] != '=') { localNode = Binary(ref node, ref s, Node.Type.ShiftLeft, 2, 9); continue; }
                if (s.Length >= 3 && s[0] == '>' && s[1] == '>' && s[2] != '=') { localNode = Binary(ref node, ref s, Node.Type.ShiftRight, 2, 9); continue; }

                if (depth >= 8) break;
                if (s.Length >= 2 && s[0] == '&' && s[1] != '&' && s[1] != '=') { localNode = Binary(ref node, ref s, Node.Type.BitwiseAnd, 1, 8); continue; }
                if (s.Length >= 2 && s[0] == '|' && s[1] != '|' && s[1] != '=') { localNode = Binary(ref node, ref s, Node.Type.BitwiseOr, 1, 8); continue; }
                if (s.Length >= 2 && s[0] == '^' && s[1] != '^' && s[1] != '=') { localNode = Binary(ref node, ref s, Node.Type.BitwiseXor, 1, 8); continue; }

                if (depth >= 7) break;
                if (s.Length >= 2 && s[0] == '~' && s[1] != '=') { localNode = Binary(ref node, ref s, Node.Type.Concatenate, 1, 7); continue; }

                if (depth >= 6) break;
                if (s.Length >= 2 && s[0] == '=' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.Equal, 2, 6); continue; }
                if (s.Length >= 2 && s[0] == '!' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.NotEqual, 2, 6); continue; }
                if (s.Length >= 2 && s[0] == '<' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.LessThanEqual, 2, 6); continue; }
                if (s.Length >= 2 && s[0] == '>' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.GreaterThanEqual, 2, 6); continue; }
                if (s[0] == '<') { localNode = Binary(ref node, ref s, Node.Type.LessThan, 1, 6); continue; }
                if (s[0] == '>') { localNode = Binary(ref node, ref s, Node.Type.GreaterThan, 1, 6); continue; }

                if (depth >= 5) break;
                if (s.Length >= 2 && s[0] == '&' && s[1] == '&') { localNode = Binary(ref node, ref s, Node.Type.LogicalAnd, 2, 5); continue; }
                if (s.Length >= 2 && s[0] == '|' && s[1] == '|') { localNode = Binary(ref node, ref s, Node.Type.LogicalOr, 2, 5); continue; }

                if (s.Length >= 2 && s[0] == '?' && s[1] == '?') { localNode = Binary(ref node, ref s, Node.Type.Coalesce, 2, 4); continue; }
                if (s.Length >= 2 && s[0] == '?' && s[1] != '?') { localNode = Ternary(ref node, ref s, Node.Type.Condition, 1, 4); continue; }
                if (depth >= 4) break;

                if (s[0] == '=') { localNode = Binary(ref node, ref s, Node.Type.Assign, 1, 3); continue; }
                if (s.Length >= 2 && s[0] == ':' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.Create, 2, 3); continue; }
                if (s.Length >= 2 && s[0] == '*' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignMultiply, 2, 3); continue; }
                if (s.Length >= 2 && s[0] == '/' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignDivide, 2, 3); continue; }
                if (s.Length >= 2 && s[0] == '%' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignModulo, 2, 3); continue; }
                if (s.Length >= 2 && s[0] == '+' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignAdd, 2, 3); continue; }
                if (s.Length >= 2 && s[0] == '-' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignSubtract, 2, 3); continue; }
                if (s.Length >= 4 && s[0] == '<' && s[1] == '<' && s[2] == '<' && s[3] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignRotateLeft, 4, 3); continue; }
                if (s.Length >= 4 && s[0] == '>' && s[1] == '>' && s[2] == '>' && s[3] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignRotateRight, 4, 3); continue; }
                if (s.Length >= 3 && s[0] == '<' && s[1] == '<' && s[2] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignShiftLeft, 3, 3); continue; }
                if (s.Length >= 3 && s[0] == '>' && s[1] == '>' && s[2] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignShiftRight, 3, 3); continue; }
                if (s.Length >= 2 && s[0] == '&' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignBitwiseAnd, 2, 3); continue; }
                if (s.Length >= 2 && s[0] == '|' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignBitwiseOr, 2, 3); continue; }
                if (s.Length >= 2 && s[0] == '^' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignBitwiseXor, 2, 3); continue; }
                if (s.Length >= 2 && s[0] == '~' && s[1] == '=') { localNode = Binary(ref node, ref s, Node.Type.AssignConcatenate, 2, 3); continue; }

                if (depth >= 3) break;
                if (depth >= 2) break;

                if (s[0] == ',') {
                    localNode = Seperator(ref node, ref s, Node.Type.Separator, 1, 2); continue;
                }

                if (depth >= 1 && (s[0] == ')' || s[0] == ']')) break;

                while (Char.IsWhiteSpace(s[0]))
                    s = s.Substring(1);

                if (s.Length == 0)
                    break;

                throw new Error("Unrecognized terminal");
            }
        }
    }
}
