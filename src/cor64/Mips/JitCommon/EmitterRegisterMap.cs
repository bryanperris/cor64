using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using cor64.Mips.Analysis;
using cor64.Mips.R4300I.JitIL;
using IL = System.Reflection.Emit.OpCodes;

namespace cor64.Mips.JitCommon
{
    public class EmitterRegisterMap
    {
        private int[] m_GprMap = new int[32] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        private int[] m_GprActiveEntries;
        private int[] m_FprDMap = new int[32] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        private int[] m_FprDActiveEntries;
        private int[] m_FprFMap = new int[32] { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
        private int[] m_FprFActiveEntries;
        private bool m_LoMapped;
        private bool m_HiMapped;
        private MipsILEmitter m_Emitter;
        private bool m_LocalsExist = false;
        private bool m_Address = false;

        public EmitterRegisterMap(MipsILEmitter emitter, RegUsageCollector regUsageCollector, int localOffset)
        {
            m_Emitter = emitter;
            LocalStart = localOffset;
            var offset = localOffset;
            m_GprActiveEntries = regUsageCollector.GprSelects.ToArray();
            m_FprDActiveEntries = regUsageCollector.FprDSelects.ToArray();
            m_FprFActiveEntries = regUsageCollector.FprFSelects.ToArray();

            for (int i = 0; i < m_GprActiveEntries.Length; i++)
            {
                m_GprMap[m_GprActiveEntries[i]] = offset++;
            }

            if (regUsageCollector.Lo)
            {
                m_LoMapped = true;
                MappedLo = offset++;
            }

            if (regUsageCollector.Hi)
            {
                m_HiMapped = true;
                MappedHi = offset++;
            }

            if (regUsageCollector.Address)
            {
                m_Address = true;
                MappedAddress = offset++;
            }

            for (int i = 0; i < m_FprDActiveEntries.Length; i++)
            {
                m_FprDMap[m_FprDActiveEntries[i]] = offset++;
            }

            for (int i = 0; i < m_FprFActiveEntries.Length; i++)
            {
                m_FprFMap[m_FprFActiveEntries[i]] = offset++;
            }
        }

        public void LoadLocals32()
        {
            if (!m_LocalsExist)
            {
                for (int i = 0; i < m_GprActiveEntries.Length; i++)
                {
                    m_Emitter.DeclareLocal(typeof(uint));
                }

                if (m_LoMapped)
                {
                    m_Emitter.DeclareLocal(typeof(uint));
                }

                if (m_HiMapped)
                {
                    m_Emitter.DeclareLocal(typeof(uint));
                }

                if (m_Address)
                {
                    m_Emitter.DeclareLocal(typeof(long));
                }

                DefineLocalsFpu();

                m_LocalsExist = true;
            }

            for (int i = 0; i < m_GprActiveEntries.Length; i++)
            {
                var gprSelect = m_GprActiveEntries[i];
                var gprLocal = m_GprMap[m_GprActiveEntries[i]];

                m_Emitter.EmitMipsCall(nameof(IDynamicMips.ReadGPR32), () => {
                    m_Emitter.Constant32(gprSelect);
                });

                m_Emitter.EmitLocalStore(gprLocal);
            }

            if (m_LoMapped)
            {
                m_Emitter.EmitMipsCall(nameof(IDynamicMips.ReadLo32), null);
                m_Emitter.EmitLocalStore(MappedLo);
            }

            if (m_HiMapped)
            {
                m_Emitter.EmitMipsCall(nameof(IDynamicMips.ReadHi32), null);
                m_Emitter.EmitLocalStore(MappedHi);
            }

            LoadLocalsFpu();
        }

        public void StoreLocals32()
        {
            for (int i = 0; i < m_GprActiveEntries.Length; i++)
            {
                var gprSelect = m_GprActiveEntries[i];
                var gprLocal = m_GprMap[m_GprActiveEntries[i]];

                m_Emitter.EmitMipsCall(nameof(IDynamicMips.WriteGPR32), () => {
                    m_Emitter.Constant32(gprSelect);
                    m_Emitter.EmitLocalLoad(gprLocal);
                });
            }

            if (m_LoMapped)
            {
                m_Emitter.EmitMipsCall(nameof(IDynamicMips.WriteLo32), () => {
                    m_Emitter.EmitLocalLoad(MappedLo);
                });
            }

            if (m_HiMapped)
            {
                m_Emitter.EmitMipsCall(nameof(IDynamicMips.WriteHi32), () => {
                    m_Emitter.EmitLocalLoad(MappedHi);
                });
            }

            StoreLocalsFpu();
        }

        public void LoadLocals64()
        {
            if (!m_LocalsExist)
            {
                for (int i = 0; i < m_GprActiveEntries.Length; i++)
                {
                    m_Emitter.DeclareLocal(typeof(ulong));
                }

                if (m_LoMapped)
                {
                    m_Emitter.DeclareLocal(typeof(ulong));
                }

                if (m_HiMapped)
                {
                    m_Emitter.DeclareLocal(typeof(ulong));
                }

                if (m_Address)
                {
                    m_Emitter.DeclareLocal(typeof(long));
                }

                DefineLocalsFpu();

                m_LocalsExist = true;
            }

            for (int i = 0; i < m_GprActiveEntries.Length; i++)
            {
                var gprSelect = m_GprActiveEntries[i];
                var gprLocal = m_GprMap[m_GprActiveEntries[i]];

                m_Emitter.EmitMipsCall(nameof(IDynamicMips.ReadGPR64), () => {
                    m_Emitter.Constant32(gprSelect);
                });

                m_Emitter.EmitLocalStore(gprLocal);
            }

            if (m_LoMapped)
            {
                m_Emitter.EmitMipsCall(nameof(IDynamicMips.ReadLo64), null);
                m_Emitter.EmitLocalStore(MappedLo);
            }

            if (m_HiMapped)
            {
                m_Emitter.EmitMipsCall(nameof(IDynamicMips.ReadHi64), null);
                m_Emitter.EmitLocalStore(MappedHi);
            }

            LoadLocalsFpu();
        }

        public void StoreLocals64()
        {
            for (int i = 0; i < m_GprActiveEntries.Length; i++)
            {
                var gprSelect = m_GprActiveEntries[i];
                var gprLocal = m_GprMap[m_GprActiveEntries[i]];

                m_Emitter.EmitMipsCall(nameof(IDynamicMips.WriteGPR64), () => {
                    m_Emitter.Constant32(gprSelect);
                    m_Emitter.EmitLocalLoad(gprLocal);
                });
            }

            if (m_LoMapped)
            {
                m_Emitter.EmitMipsCall(nameof(IDynamicMips.WriteLo64), () => {
                    m_Emitter.EmitLocalLoad(MappedLo);
                });
            }

            if (m_HiMapped)
            {
                m_Emitter.EmitMipsCall(nameof(IDynamicMips.WriteHi64), () => {
                    m_Emitter.EmitLocalLoad(MappedHi);
                });
            }

            StoreLocalsFpu();
        }


        public void DefineLocalsFpu()
        {
            for (int i = 0; i < m_FprDActiveEntries.Length; i++)
            {
                m_Emitter.DeclareLocal(typeof(double));
            }

            for (int i = 0; i < m_FprFActiveEntries.Length; i++)
            {
                m_Emitter.DeclareLocal(typeof(float));
            }
        }

        public void LoadLocalsFpu()
        {
            for (int i = 0; i < m_FprDActiveEntries.Length; i++)
            {
                var select = m_FprDActiveEntries[i];
                var local = m_FprDMap[m_FprDActiveEntries[i]];

                m_Emitter.EmitMipsCall(nameof(IDynamicMips.ReadFPRDouble), () => {
                    m_Emitter.Constant32(select);
                });

                m_Emitter.EmitLocalStore(local);
            }

            for (int i = 0; i < m_FprFActiveEntries.Length; i++)
            {
                var select = m_FprFActiveEntries[i];
                var local = m_FprFMap[m_FprFActiveEntries[i]];

                m_Emitter.EmitMipsCall(nameof(IDynamicMips.ReadFPRFloat), () => {
                    m_Emitter.Constant32(select);
                });

                m_Emitter.EmitLocalStore(local);
            }
        }

        public void StoreLocalsFpu()
        {
            for (int i = 0; i < m_FprDActiveEntries.Length; i++)
            {
                var select = m_FprDActiveEntries[i];
                var local = m_FprDMap[m_FprDActiveEntries[i]];

                m_Emitter.EmitMipsCall(nameof(IDynamicMips.WriteFPRDouble), () => {
                    m_Emitter.Constant32(select);
                    m_Emitter.EmitLocalLoad(local);
                });
            }

            for (int i = 0; i < m_FprFActiveEntries.Length; i++)
            {
                var select = m_FprFActiveEntries[i];
                var local = m_FprFMap[m_FprFActiveEntries[i]];

                m_Emitter.EmitMipsCall(nameof(IDynamicMips.WriteFPRFloat), () => {
                    m_Emitter.Constant32(select);
                    m_Emitter.EmitLocalLoad(local);
                });
            }
        }

        public int LocalStart { get; }

        public int LocalCount { get; }

        public IReadOnlyList<int> GprMap => m_GprMap.ToList();

        public IReadOnlyList<int> FprDMap => m_FprDMap.ToList();

        public IReadOnlyList<int> FprFMap => m_FprFMap.ToList();

        public int MappedLo { get; } = -1;

        public int MappedHi { get; } = -1;

        public int MappedAddress { get; } = -1;
    }
}
