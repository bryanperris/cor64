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
        private readonly SystemController m_Cop0;
        private readonly TLBRegisters m_Registers;
        private readonly TLBEntry[] m_Entries = new TLBEntry[32];
        private Boolean m_DisableTLB = false; // Used to simulate a real TLB shutdown
        private readonly Dictionary<uint, int> m_EntryHashLookup = new Dictionary<uint, int>();

        public TLBCache(SystemController systemController)
        {
            m_Cop0 = systemController;
            m_Registers = systemController.REGS.TLBRegs;

            for (int i = 0; i < m_Entries.Length; i++)
            {
                m_Entries[i] = new TLBEntry();
            }
        }

        public void Initialize() {
            m_Registers.Reset();
        }

        public void Tick() {
            m_Registers.UpdateRandom();
        }

        private uint Key(uint pageMask, uint vpn2, uint asid, bool global) {
            if (global) {
                return (vpn2 ^ 0x100);
            }
            else {
                return (vpn2 ^ asid);
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
            if (m_DisableTLB) return;

            m_Registers.ProbeFail = false;

            for (int i = 0; i < m_Entries.Length; i++) {
                var entry = m_Entries[i];

                uint vpn2 = m_Registers.RegEntryHi.VPN2 & ~entry.Mask >> 13;

                bool regionMatch = true;

                // Console.WriteLine("Entry {0}: {1}", i, entry.ToString());
                // Console.WriteLine("EntryHi {0}", m_Registers.RegEntryHi.ToString());

                if (m_Cop0.IsOperation64) {
                    regionMatch = m_Registers.RegEntryHi.Region == entry.EntryHi.Region;
                }

                if (m_Registers.RegEntryHi.ASID == entry.ASID) {
                    if (vpn2 == entry.EntryHi.VPN2 && regionMatch) {
                        // Console.WriteLine("Match ASID: {0} {1}", i, entry.ToString());
                        m_Registers.Index = i;
                        return;
                    }
                }
                else {
                    if (entry.IsGlobal && regionMatch) {
                        if (vpn2 == entry.EntryHi.VPN2) {
                            // Console.WriteLine("Match G: {0} {1}", i, entry.ToString());
                            m_Registers.Index = i;
                            return;
                        }
                    }
                }
            }

            m_Registers.Index = 0;
            m_Registers.ProbeFail = true;
        }

        /// <summary>
        /// Writes an entry to the TLB.
        /// Registers used: EntryHi, PageMask, EntryLo0, EntryLo1, Index, Random, Wired, BadVAddr
        /// </summary>
        public virtual void Write(bool isRandomWrite)
        {
            if (m_DisableTLB) return;

            var index = isRandomWrite ? m_Registers.Random : m_Registers.Index;

            // Upper 32  or lower 32 entries

            int wired = m_Registers.Wired;

            if (wired > 32) {
                wired -= 32;
            }

            wired--;

            if (isRandomWrite && (index >= wired)) return;

            var entry = new TLBEntry(
                m_Registers.PageMaskFixed,
                new EntryHiStore(
                    m_Registers.RegEntryHi.ASID,
                    m_Registers.RegEntryLo0.IsGlobal && m_Registers.RegEntryLo1.IsGlobal,
                    m_Registers.RegEntryHi.VPN2 & (~m_Registers.PageMask >> 13),
                    m_Registers.RegEntryHi.Region
                ),
                new EntryLoStore(
                    m_Registers.RegEntryLo0.PageFrameNumber,
                    m_Registers.RegEntryLo0.IsDirty,
                    m_Registers.RegEntryLo0.IsValid,
                    m_Registers.RegEntryLo0.CoherencyMode
                ),
                new EntryLoStore(
                    m_Registers.RegEntryLo1.PageFrameNumber,
                    m_Registers.RegEntryLo1.IsDirty,
                    m_Registers.RegEntryLo1.IsValid,
                    m_Registers.RegEntryLo1.CoherencyMode
                )
            );

            #if DEBUG_TLB
            if (entry.IsValid)
                Console.WriteLine("Entry {0} set to {1}", index, entry.ToString());
            #endif

            #if FAST_TLB
            uint key = Key(entry.Mask, entry.EntryHi.VPN2, entry.ASID, entry.IsGlobal);

            foreach (KeyValuePair<uint, int> e in m_EntryHashLookup) {
                if (e.Value == index) {
                    m_EntryHashLookup.Remove(e.Key);
                }
            }

            if (entry.IsValid) m_EntryHashLookup.Add(key, index);
            #endif

            m_Entries[index] = entry;
        }

        public virtual void Read()
        {
            if (m_DisableTLB) return;

            var entry = m_Entries[m_Registers.Index];

            m_Registers.PageMask = entry.Mask;

            m_Registers.RegEntryHi.ASID = entry.EntryHi.ASID;
            m_Registers.RegEntryHi.VPN2 = entry.EntryHi.VPN2;

            m_Registers.RegEntryLo0.CoherencyMode = entry.Even.CoherencyMode;
            m_Registers.RegEntryLo0.PageFrameNumber = entry.Even.PageFrameNumber;
            m_Registers.RegEntryLo0.IsDirty = entry.Even.IsDirty;
            m_Registers.RegEntryLo0.IsValid = entry.Even.IsValid;

            m_Registers.RegEntryLo1.CoherencyMode = entry.Odd.CoherencyMode;
            m_Registers.RegEntryLo1.PageFrameNumber = entry.Odd.PageFrameNumber;
            m_Registers.RegEntryLo1.IsDirty = entry.Odd.IsDirty;
            m_Registers.RegEntryLo1.IsValid = entry.Odd.IsValid;
        }

        public void FlushAll()
        {
            if (m_DisableTLB) return;

            Initialize();
        }

        private void RaiseRefillException(ulong vaddr, uint vpn2, byte asid, bool isStore) {
            // m_Cop0.REGS.Write(CTS.CP0_REG_BADVADDR, vaddr);
            // m_Cop0.SetTLBRefillException(isStore);
            // m_Cop0.REGS.Write(CTS.CP0_REG_CONTEXT, vpn2);
            // m_EntryHi.ASID = asid;
            // m_EntryHi.SetVPN2(vpn2);
        }

        private void RaiseInvalidException(ulong vaddr, uint vpn2, byte asid, bool isStore) {
            // m_Cop0.REGS.Write(CTS.CP0_REG_BADVADDR, vaddr);
            // m_Cop0.SetInvalidTLBException(isStore);
            // m_Cop0.REGS.Write(CTS.CP0_REG_CONTEXT, vpn2);
            // m_EntryHi.ASID = asid;
            // m_EntryHi.SetVPN2(vpn2);
        }

        private static long SinglePageSize(uint pageMask) {
            return SinglePageMask(pageMask) + 1;
        }

        private static uint SinglePageMask(uint pageMask) {
            return (pageMask | 0x1FFF) >> 1;
        }

        private bool FastTranslate(long virtualAddress, out TLBEntry entry) {
            uint size = 1;

            entry = null;

            for (int i = 0; i < 7; i++) {
                size *= 4;
                uint pageMask = ((size << 12 ) - 1) >> 1;

                uint vpn2 = (uint)virtualAddress >> 13;
                vpn2 &= ~pageMask >> 13;
                vpn2 &= 0x7FFFFFF;

                // Try asid entry
                uint key_asid = Key(pageMask, vpn2, m_Registers.RegEntryHi.ASID, false);
                uint key_global = Key(pageMask, vpn2, 0, true);

                // Console.WriteLine("FastTLB try lookup: {0:X8} {1:X8} {2:X8}", pageMask, key_asid, key_global);

                if (m_EntryHashLookup.TryGetValue(key_asid, out int foundA)) {
                    // Console.WriteLine("index found A = {0}", foundA);
                    entry = m_Entries[foundA];
                }

                if (m_EntryHashLookup.TryGetValue(key_global, out int foundB)) {
                    // Console.WriteLine("index found B = {0}", foundB);
                    entry = m_Entries[foundB];
                }
            }

            return entry != null;
        }

        private bool TryPhysicalTranslate(TLBEntry entry, long virtualAddress, out long physicalAddress) {
            uint pfn;

            if ((virtualAddress & SinglePageSize(entry.Mask)) != 0) {
                if (!entry.Odd.IsValid)
                    throw new InvalidOperationException("odd page is invalid!");

                pfn = entry.Odd.PageFrameNumber;
            }
            else {
                if (!entry.Even.IsValid)
                    throw new InvalidOperationException("even page is invalid!");

                pfn = entry.Even.PageFrameNumber;
            }

            // offset from vaddr is always 12 bytes
            pfn &= 0xFFFFFF;
            physicalAddress = pfn << 12;
            physicalAddress |= (uint)virtualAddress & SinglePageMask(entry.Mask);

            #if DEBUG_TLB_TRANSLATE
            Log.Debug("TLB Translated: {0:X8} -> {1:X8}", virtualAddress, physicalAddress);
            #endif

            return true;
        }

        public long Translate(long virtualAddress, bool isStore)
        {
            // TODO: Throw TLB mod exception when isStore == isDirty

            #if FAST_TLB
            if (FastTranslate(virtualAddress, out TLBEntry entry)) {
                if (TryPhysicalTranslate(entry, virtualAddress, out long address)) {
                    return address;
                }
            }
            #else

            for (int i = 0; i < m_Entries.Length; i++) {
                var entry = m_Entries[i];

                if (!entry.Even.IsValid && !entry.Odd.IsValid)
                    continue;

                // Console.WriteLine("Test TLB entry: " + entry.ToString());

                if (entry.IsGlobal || (!entry.IsGlobal && entry.ASID == m_Registers.RegEntryHi.ASID) ) {
                    uint vpn2 = (uint)virtualAddress >> 13;
                    vpn2 &= ~entry.Mask >> 13;
                    vpn2 &= 0x7FFFFFF;

                    // Console.WriteLine("VPN2 from vaddr = {0:X8}, mask = {1:X8}", vpn2, mask);

                    if (entry.EntryHi.VPN2 == vpn2) {
                        // Console.WriteLine("Entry Found");
                        if (TryPhysicalTranslate(entry, virtualAddress, out long address)) {
                            return address;
                        }
                    }
                }
            }
            #endif

            // if (RaiseRefillException(virtualAddress, ))
            throw new InvalidOperationException("todo accurately throw TLB exceptions");
        }
    }
}