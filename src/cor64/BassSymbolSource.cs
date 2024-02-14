using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cor64.BassSharp;
using cor64.Mips;

namespace cor64
{
    public class BassSymbolSource : ISymbolProvider
    {
        private readonly Dictionary<long, String> m_LocalSymbols = new Dictionary<long, string>();
        private readonly Dictionary<long, String> m_Labels = new Dictionary<long, string>();

        public BassSymbolSource(Bass bassAssembler)
        {
            foreach (KeyValuePair<long, string> entry in bassAssembler.Symbols)
            {
                m_LocalSymbols.Add(entry.Key, entry.Value);
                m_Labels.Add(entry.Key, entry.Value);
            }
        }

        public string GetSymbol(long address)
        {
            if (m_LocalSymbols.TryGetValue(address, out string value))
            {
                return value;
            }
            else
            {
                return "";
            }
        }

        public string GetLabel(long address)
        {
            if (m_Labels.TryGetValue(address, out string value)) {
                return value;
            }
            else {
                return "";
            }
        }
    }
}
