using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using cor64.Mips;
using ELFSharp.ELF;
using ELFSharp.ELF.Sections;

namespace RunN64
{
    public class DebugSymbolSource : ISymbolProvider
    {
        private Dictionary<ulong, String> m_LocalSymbols = new Dictionary<ulong, string>();
        private Dictionary<ulong, String> m_GlobalSymbols = new Dictionary<ulong, string>();

        public DebugSymbolSource(String  filePath)
        {
            var reader = ELFReader.Load(filePath);
            var symbolTable = (ISymbolTable)reader.GetSection(".symtab");

            foreach (SymbolEntry<uint> entry in symbolTable.Entries)
            {
                if (entry.Type == SymbolType.Function)
                {
                    //Console.WriteLine("ELF SYMBOL: {0:X8} {1} {2} {3}", entry.Value, entry.Binding, entry.Name, entry.Size);

                    if (entry.Binding == SymbolBinding.Local)
                    {
                        AddSymbol(entry, m_LocalSymbols);
                    }
                    else if (entry.Binding == SymbolBinding.Global)
                    {
                        AddSymbol(entry, m_GlobalSymbols);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        private void AddSymbol(SymbolEntry<uint> entry, IDictionary<ulong, string> dict)
        {
            if (entry.Size > 0)
            {
                for (uint i = 0; i < entry.Size; i++)
                {
                    dict.Add(entry.Value + i, entry.Name);
                }
            }
            else
            {
                dict.Add(entry.Value, entry.Name);
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
                if (m_GlobalSymbols.ContainsKey(address))
                {
                    return m_GlobalSymbols[address];
                }
                else
                {
                    return "";
                }
            }
        }
    }
}
