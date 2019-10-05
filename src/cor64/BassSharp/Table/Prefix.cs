using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp.Table
{
    public class Prefix
    {
        private String m_Text;
        private int m_Size;

        public Prefix(string text, int size)
        {
            this.m_Text = text;
            this.m_Size = size;
        }

        public string Text { get => m_Text; set => m_Text = value; }
        public int Size { get => m_Size; set => m_Size = value; }
    }
}
