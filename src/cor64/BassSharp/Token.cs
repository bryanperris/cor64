using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    public abstract class Token
    {
        private String m_Name;

        public Token(String name)
        {
            this.m_Name = name;
        }

        public virtual void CloneFrom(Token token)
        {
            m_Name = token.m_Name;
        }

        public String Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(m_Name);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return Object.Equals(this, obj);

            if (obj is String @string) {
                return m_Name == @string;
            }
            else {
                return m_Name == ((Token)obj).m_Name;
            }
        }

        public static bool operator ==(Token a, Token b)
        {
            if (a is null) {
                return b is null;
            }

            return a.Equals(b);
        }

        public static bool operator !=(Token a, Token b)
        {
            return !(a == b);
        }

        public static bool operator <(Token a, Token b)
        {
            return String.Compare(a.m_Name, b.m_Name) < 0;
        }

        public static bool operator >(Token a, Token b)
        {
            return String.Compare(a.m_Name, b.m_Name) > 0;
        }
    }
}
