using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cor64.Mips.Analysis
{
    public class NoteBasicBlock : InfoBasicBlock
    {
        private String m_Note;

        public NoteBasicBlock(ProgramTrace parent, String note) : base(parent, 0) {
            m_Note = note;
        }

        public string Note => m_Note;
    }
}