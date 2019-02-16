using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    public class Directives
    {
        public class EmitBytesOp
        {
            private String m_Token;
            private int m_Length;

            public EmitBytesOp(String token, int len)
            {
                this.m_Token = token;
                this.m_Length = len;
            }

            public String Token
            {
                get {
                    return m_Token;
                }
                set {
                    m_Token = value;
                }
            }

            public int Length => m_Length;
        }

        private List<EmitBytesOp> m_EmitBytes;

        public Directives()
        {
            m_EmitBytes = new List<EmitBytesOp>() {
                new EmitBytesOp("db ", 1),
                new EmitBytesOp("dw ", 2),
                new EmitBytesOp("dl ", 3),
                new EmitBytesOp("dd ", 4),
                new EmitBytesOp("dq ", 8)
            };
        }

        public IList<EmitBytesOp> EmitBytes => m_EmitBytes;
    }
}
