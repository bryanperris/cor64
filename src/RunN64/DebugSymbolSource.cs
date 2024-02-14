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
        private readonly Dictionary<long, String> m_LocalSymbols = new Dictionary<long, string>();
        private readonly Dictionary<long, String> m_GlobalSymbols = new Dictionary<long, string>();
        private readonly Dictionary<long, String> m_Labels = new Dictionary<long, string>();

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

        private void AddSymbol(SymbolEntry<uint> entry, IDictionary<long, string> dict)
        {
            #if PRINT_ELF_SYMBOLS
            Console.WriteLine("Loaded Symbol: {0} {1:X8}", entry.Name, entry.Value);
            #endif

            if (m_Labels.ContainsKey(entry.Value)) {
                m_Labels[entry.Value] += "|" + entry.Name;
            }
            else {
                m_Labels.Add(entry.Value, entry.Name);
            }

            if (entry.Size > 0)
            {
                for (uint i = 0; i < entry.Size; i++)
                {
                    var key = entry.Value + i;

                    if (!dict.ContainsKey(key)) {
                        dict.Add(entry.Value + i, entry.Name);
                    }
                    else {
                        dict[entry.Value + i] += "|" + entry.Name;
                    }
                }
            }
            else
            {
                if (!dict.ContainsKey(entry.Value)) {
                    dict.Add(entry.Value, entry.Name);
                }
                else {
                    dict[entry.Value] += "|" + entry.Name;
                }
            }
        }

        public string GetSymbol(long address)
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

        public string GetLabel(long address)
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
