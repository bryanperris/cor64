

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NLog;

namespace cor64.Mips.R4300I.TLB
{
    public class TLBCache
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private readonly BitFiddler64 m_BitFiddler = new BitFiddler64();
        private SystemController m_Cop0;
        private readonly EntryLoRegister m_EntryLo0 = new EntryLoRegister();
        private readonly EntryLoRegister m_EntryLo1 = new EntryLoRegister();
        private readonly EntryHiRegister m_EntryHi = new EntryHiRegister();
        private ulong m_Index;
        private ulong m_Wired;
        private ulong m_PageMask;
        private ulong m_Random;
        private readonly TLBEntry[] m_Entries = new TLBEntry[48];
        private readonly Dictionary<EntryKey, CacheMappedEntry> m_MappedEntries = new();
        private Boolean m_DisableTLB = false; // Used to simulate a real TLB shutdown

        private const int F_PAGE_MASK = 0;
        private const int F_INDEX = 1;
        private const int F_PROBE_FAIL = 2;
        private const int F_RANDOM = 3;
        private const int F_WIRED = 4;

        public bool TLBWritten { get; private set; }

        private struct EntryKey : IEqualityComparer<EntryKey>, IEquatable<EntryKey>
        {
            private readonly uint m_VPN2; // Max possible bits is 27 bits
            private readonly byte m_ASID; // address space ID
            private readonly uint m_Mask; // Page Mask
            private readonly byte m_Global; // global bit

            public EntryKey(uint vpn2, byte asid, uint mask, bool global)
            {
                m_VPN2 = vpn2 & 0x7FFFFFF;
                m_ASID = asid;
                m_Mask = mask;
                m_Global = global ? 1 : 0;
            }

            public static long MakeKey(EntryKey key)
            {
                ulong k = (ulong)key.m_Global << (27 + 8 + 12);
                k |= key.m_VPN2;
                k |= (ulong)key.m_ASID << 27;
                k |= (ulong)(key.m_Mask & 0xFFF) << (27 + 8);
                return (long)k;
            }

            public long GetKey() => MakeKey(this);

            public bool Equals(EntryKey other)
            {
                return GetKey() == other.GetKey();
            }

            public bool Equals(EntryKey x, EntryKey y)
            {
                return x.GetKey() == y.GetKey();
            }

            public int GetHashCode([DisallowNull] EntryKey obj)
            {
                // Do the slow and safe method
                return GetKey().ToString("X16").GetHashCode();
            }

            public override string ToString()
            {
                return GetKey().ToString("X16");
            }
        }

        private struct CacheMappedEntry
        {
            private readonly int m_Index;
            private readonly TLBEntry m_Entry;


            public CacheMappedEntry(int index, TLBEntry entry)
            {
                m_Index = index;
                m_Entry = entry;
            }

            public int Index => m_Index;
            public TLBEntry Entry => m_Entry;
        }

        public TLBCache()
        {
            /* Page Mask */
            m_BitFiddler.DefineField(13, 12);
            /* Index */
            m_BitFiddler.DefineField(0, 6);
            /* P-Fail */
            m_BitFiddler.DefineField(30, 1);
            /* Random */
            m_BitFiddler.DefineField(0, 6);
            /* Wired */
            m_BitFiddler.DefineField(0, 6);
        }

        public void AttachCoprocessor0(SystemController cop0) {
            m_Cop0 = cop0;
        }

        public void Initialize()
        {
            m_Cop0.REGS.Write(CTS.CP0_REG_BADVADDR, 0xFFFFFFFF);
            RF_Random = 32;
            m_MappedEntries.Clear();

            for (int i = 0; i < m_Entries.Length; i++)
            {
                m_Entries[i] = new TLBEntry();
            }
        }

        /// <summary>
        /// If successful, the probe returns the entry index matching the VPN2 / ASID values in EntryHi.
        /// </summary>
        /// <remarks>
        /// Ref: MIPS R4300I Manual Page 637
        /// Ref: MIPS R4300I Manual Page 115
        /// </remarks>
        public virtual void Probe()
        {
            if (m_DisableTLB)
                return;

            // #if DEBUG_TLB
            // Log.Debug("TLB probe entry {0}", RF_Index);
            // #endif

            UpdateRandom();

            var vpn2 = m_EntryHi.GetVPN2(m_Cop0.IsOperation64, RF_PageMask.VPN2Shift);
            var asid = m_EntryHi.ASID;
            var lKey = new EntryKey(vpn2, asid, RF_PageMask.SearchKey, false);
            var gKey = new EntryKey(vpn2, asid, RF_PageMask.SearchKey, true);

            // ASID Matching
            if (m_MappedEntries.TryGetValue(lKey, out CacheMappedEntry lEntry))
            {
                RF_Index = lEntry.Index;
                return;
            }

            // Global Matching
            if (m_MappedEntries.TryGetValue(gKey, out CacheMappedEntry gEntry))
            {
                RF_Index = gEntry.Index;
                return;
            }

            // Probe Failed - no matching entry
            RF_IndexProbeFailed = true;
        }

        /// <summary>
        /// Writes an entry to the TLB.
        /// Registers used: EntryHi, PageMask, EntryLo0, EntryLo1, Index, Random, Wired, BadVAddr
        /// </summary>
        public virtual void Write(bool isRandomWrite)
        {
            if (m_DisableTLB) return;

            UpdateRandom();

            var index = isRandomWrite ? RF_Random : RF_Index;

            if (index >= RF_Wired)
            {
                TLBWritten = true;

                var entry = new CacheMappedEntry(
                    index,
                    new TLBEntry(
                        m_EntryHi,
                        RF_PageMask,
                        m_EntryLo0,
                        m_EntryLo1
                    )
                );

                // We must dual register entries so they can be globally searched

                var localKey = new EntryKey(
                    m_EntryHi.GetVPN2(m_Cop0.IsOperation64, RF_PageMask.VPN2Shift),
                    m_EntryHi.ASID,
                    RF_PageMask.SearchKey,
                    false
                );

                var globalKey = new EntryKey(
                    m_EntryHi.GetVPN2(m_Cop0.IsOperation64, RF_PageMask.VPN2Shift),
                    0,
                    RF_PageMask.SearchKey,
                    true
                );

                #if DEBUG_TLB
                Log.Debug("TLB Write to entry {0} SearchKeys: L|{1} G|{2}", RF_Index, localKey.ToString(), globalKey.ToString());
                Log.Debug("{0}", entry.Entry.ToString());
                #endif

                m_Entries[entry.Index] = entry.Entry;

                if (m_MappedEntries.ContainsKey(localKey))
                    m_MappedEntries.Remove(localKey);

                if (m_MappedEntries.ContainsKey(globalKey))
                    m_MappedEntries.Remove(globalKey);

                m_MappedEntries.Add(localKey, entry);
                m_MappedEntries.Add(globalKey, entry);
            }
        }

        public virtual void Read()
        {
            if (m_DisableTLB) return;

            UpdateRandom();

            if (RF_Index < 0 || RF_Index >= m_Entries.Length)
            {
                return;
            }

            var entry = m_Entries[RF_Index];

            RF_PageMask = entry.Mask;
            m_EntryHi.Write(entry.VPN2.Read());
            m_EntryLo0.Write(entry.Even.Read());
            m_EntryLo1.Write(entry.Odd.Read());
        }

        public void FlushAll()
        {
            if (m_DisableTLB) return;

            Initialize();
        }

        private void RaiseRefillException(ulong vaddr, uint vpn2, byte asid, bool isStore) {
            m_Cop0.REGS.Write(CTS.CP0_REG_BADVADDR, vaddr);
            m_Cop0.SetTLBRefillException(isStore);
            m_Cop0.REGS.Write(CTS.CP0_REG_CONTEXT, vpn2);
            m_EntryHi.ASID = asid;
            m_EntryHi.SetVPN2(vpn2);
        }

        private void RaiseInvalidException(ulong vaddr, uint vpn2, byte asid, bool isStore) {
            m_Cop0.REGS.Write(CTS.CP0_REG_BADVADDR, vaddr);
            m_Cop0.SetInvalidTLBException(isStore);
            m_Cop0.REGS.Write(CTS.CP0_REG_CONTEXT, vpn2);
            m_EntryHi.ASID = asid;
            m_EntryHi.SetVPN2(vpn2);
        }

        public long TranslateVirtualAddress(long virtualAddress, bool isStore)
        {
            // Extract the VPN2 from the virtual address just for kernel mode
            var shift = RF_PageMask.VPN2Shift;
            var vaddress = (uint)virtualAddress;
            var vpn2 = (vaddress & 0x1FFFFFFF) >> shift;

            var asid = m_EntryHi.ASID;
            var isGlobal = m_EntryLo0.IsGlobal && m_EntryLo1.IsGlobal;
            var pageSize = RF_PageMask;

            // Make the search keys
            var localKey = new EntryKey(
                vpn2,
                asid,
                RF_PageMask.SearchKey,
                false
            );

            var globalKey = new EntryKey(
                vpn2,
                0,
                RF_PageMask.SearchKey,
                true
            );

            CacheMappedEntry entry;

            if (isGlobal)
            {
                // #if DEBUG_TLB_TRANSLATE

                if (!m_MappedEntries.TryGetValue(globalKey, out entry))
                {
                    // Address bus is always 32 bit mode
                    RaiseRefillException((ulong)virtualAddress, vpn2, asid, isStore);
                    return 0;
                }
            }

            else
            {
                /* If nothing found globally, then access the entries grouped the provided ASID */
                if (!m_MappedEntries.TryGetValue(localKey, out entry))
                {
                    // Address bus is always 32 bit mode
                    RaiseRefillException((ulong)virtualAddress, vpn2, asid, isStore);
                    return 0;
                }
            }

            RF_Index = entry.Index;

            /* Check if hits the even page or oddpage
               If the address goes over the even page boundary
               it means its on the odd page
            */
            var isOddPage = (virtualAddress & pageSize.Size) != 0;
            var entryLo = isOddPage ? entry.Entry.Odd : entry.Entry.Even;

            if (entryLo.IsValid)
            {
                var physAddress = (entryLo.PageFrameNumber << 12) & ~0xFFFU; // Physical Base Address
                physAddress |= (uint)virtualAddress & RF_PageMask.Mask;      // Offset in the page
                return physAddress;
            }
            else
            {
                RaiseInvalidException((ulong)virtualAddress, vpn2, asid, isStore);
                return 0;
            }
        }

        public void WireSetNotify()
        {
            if (m_DisableTLB) return;

            RF_Random = 46;
        }

        private void UpdateRandom()
        {
            if (RF_Index <= RF_Random)
            {
                RF_Random = RF_Wired + 1;
            }
            else
            {
                RF_Random++;

                if (RF_Random >= 47)
                    RF_Random = RF_Wired + 1;
            }
        }

        public ulong PageMask
        {
            get { return m_PageMask; }
            set { m_PageMask = value; }
        }

        private PageMask RF_PageMask
        {
            get => new((uint)m_PageMask);
            set => m_PageMask = value.Value;
        }

        public ulong EntryHi
        {
            get => m_EntryHi.Read();
            set {
                m_EntryHi.Write(value);

                #if DEBUG_TLB_ENTRYHI
                Log.Debug("TLB EntryHi: {0:X16}", value);
                #endif
            }
        }

        public ulong EntryLo0
        {
            get => m_EntryLo0.Read();
            set {
                m_EntryLo0.Write(value);
                // #if DEBUG_TLB
                // Log.Debug("TLB EntryLo0: {0:X16}", value);
                // #endif
            }
        }

        public ulong EntryLo1
        {
            get => m_EntryLo1.Read();
            set {
                m_EntryLo1.Write(value);
                // #if DEBUG_TLB
                // Log.Debug("TLB EntryLo1: {0:X16}", value);
                // #endif
            }
        }

        public ulong Index
        {
            get => m_Index;
            set => m_Index = value;
        }

        private int RF_Index
        {
            get => (int)m_BitFiddler.X(F_INDEX, ref m_Index);
            set => m_BitFiddler.J(F_INDEX, ref m_Index, (uint)value);
        }

        private bool RF_IndexProbeFailed
        {
            get => m_BitFiddler.XB(F_PROBE_FAIL, ref m_Index);
            set => m_BitFiddler.J(F_PROBE_FAIL, ref m_Index, value);
        }

        public ulong Wired
        {
            get { return m_Wired; }
            set { m_Wired = value; }
        }

        private int RF_Wired
        {
            get => (int)m_BitFiddler.X(F_WIRED, ref m_Wired);
            set => m_BitFiddler.J(F_WIRED, ref m_Wired, (uint)value);
        }

        public ulong Random
        {
            get => m_Random;
            set => m_Random = value;
        }

        public int RF_Random
        {
            get => (int)m_BitFiddler.X(F_RANDOM, ref m_Random);
            set => m_BitFiddler.J(F_RANDOM, ref m_Random, (uint)value);
        }
    }
}