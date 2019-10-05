using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Debugging
{
    public class MipsInstruction
    {
        private String m_DisassemblerLine;
        private long m_Address;
        private ProgramSectionType m_Section;
        private String m_DebugSymbol;

    }

    public enum ProgramSectionType
    {
        Unknown,
        SystemBootloader,
        LocalBootloader,
        Program
    }
}
