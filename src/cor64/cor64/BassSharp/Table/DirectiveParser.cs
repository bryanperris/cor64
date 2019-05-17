using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp.Table
{
    /* This class attempts to parse C style preprocessor macros into data used by BassTable class */
    public class DirectiveParser
    {
        private BassTable m_BassTable;

        public DirectiveParser(BassTable bassTable)
        {
            m_BassTable = bassTable;
        }

        public String ParseDirective(String name)
        {
            String data = m_BassTable.ReadResourceData(name);
            StringBuilder stringBuilder = new StringBuilder();

            if (data == null)
                return null;

            var lines = data.Split('\n');
            bool m_InString = false;

            foreach (var line in lines) {
                var trimmed = line.Trim();


                var m = line.MatchAndTrimBoth("#include \"", "\"", true);

                if (m != null) {
                    stringBuilder.AppendLine(ParseDirective(m));
                }

                if (!m_InString) {
                    if (trimmed.StartsWith("R\"(")) {
                        m_InString = true;
                        continue;
                    }
                }
                else {
                    if (trimmed.StartsWith(")\"")) {
                        m_InString = false;
                        continue;
                    }

                    stringBuilder.AppendLine(trimmed);
                }
            }


            return stringBuilder.ToString();
        }
    }
}
