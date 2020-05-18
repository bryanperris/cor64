using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp.Eval
{
    internal class Node
    {
        public enum Type
        {
            Null,
            Literal,
            Function,
            Subscript,
            Member,
            SuffixIncrement,
            SuffixDecrement,
            Reference,
            Dereference,
            LogicalNot,
            BitwiseNot,
            Positive,
            Negative,
            PrefixIncrement,
            PrefixDecrement,
            Multiply,
            Divide,
            Modulo,
            Add,
            Subtract,
            RotateLeft,
            RotateRight,
            ShiftLeft,
            ShiftRight,
            BitwiseAnd,
            BitwiseOr,
            BitwiseXor,
            Concatenate,
            Equal,
            NotEqual,
            LessThanEqual,
            GreaterThanEqual,
            LessThan,
            GreaterThan,
            LogicalAnd,
            LogicalOr,
            Coalesce,
            Condition,
            Assign,
            Create,
            AssignMultiply,
            AssignDivide,
            AssignModulo,
            AssignAdd,
            AssignSubtract,
            AssignRotateLeft,
            AssignRotateRight,
            AssignShiftLeft,
            AssignShiftRight,
            AssignBitwiseAnd,
            AssignBitwiseOr,
            AssignBitwiseXor,
            AssignConcatenate,
            Separator,
        }

        private Type m_Type;
        private String m_Literal;
        private List<Node> m_Link = new List<Node>();

        public Node(Type type)
        {
            m_Type = type;
        }

        public Node()
        {
            m_Type = Type.Null;
        }

        public Type NodeType
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        public String Literal
        {
            get { return m_Literal; }
            set { m_Literal = value; }
        }

        public IList<Node> Link => m_Link;
    }
}
