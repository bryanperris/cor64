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
        private readonly Dictionary<ulong, String> m_LocalSymbols = new Dictionary<ulong, string>();
        private readonly Dictionary<ulong, String> m_Labels = new Dictionary<ulong, string>();

        public BassSymbolSource(Bass bassAssembler)
        {
            foreach (KeyValuePair<long, string> entry in bassAssembler.Symbols)
            {
                m_LocalSymbols.Add((ulong)entry.Key, entry.Value);
                m_Labels.Add((ulong)entry.Key, entry.Value);
            }
        }

        public string GetSymbol(ulong address)
        {
            if (m_LocalSymbols.ContainsKey(address))
            {
                return m_LocalSymbols[address];
            }
            else
            {
                return "";
            }
        }

        public string GetLabel(ulong address)
        {
            if (m_Labels.ContainsKey(address)) {
                return m_Labels[address];
            }
            else {
                return "";
            }
        }
    }
}
