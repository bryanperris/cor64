using System.IO;
using cor64.IO;

namespace cor64.Rdp.LLE {
    public class TextureMemReader {
        private readonly int[] m_TextureAddress = new int[4];
        private readonly Color[] m_TexelColor = new Color[4];
        private static readonly byte[] s_ReplicatedRgba = new byte[32];
        private readonly UnmanagedBuffer m_TextureMemory;
        private readonly State GS;

        private const int BYTE_XOR_DWORD_SWAP = 7;
        private const int BYTE_ADDR_XOR = 3;
        private const int WORD_XOR_DWORD_SWAP = 3;
        private const int WORD_ADDR_XOR = 1;

        static TextureMemReader() {
            for (int i = 0; i < s_ReplicatedRgba.Length; i++) {
                int v = (i << 3) | ((i >> 2) & 7);
                s_ReplicatedRgba[i] = (byte)v;
            }
        }

        public TextureMemReader(State state, UnmanagedBuffer tmem) {
            GS = state;
            m_TextureMemory = tmem;
        }

        public Color ReadColor(int index) => m_TexelColor[index];

        public int ReadAddress(int index) => m_TextureAddress[index];

        public int ReadTmem8(int address) {
            return m_TextureMemory.GetPointer().Offset(address).AsType_8();
        }

        public int ReadTmem16(int address) {
            return m_TextureMemory.GetPointer().Offset(address << 1).AsType_16();
        }

        public int ReadTmem16Idx(int address) {
            address ^= WORD_ADDR_XOR;
            address <<= 1;
            return m_TextureMemory.GetPointer().Offset(address).AsType_16();
        }

        public void WriteTmem16Idx(int address, int value) {
            address ^= WORD_ADDR_XOR;
            address <<= 1;
            m_TextureMemory.GetPointer().Offset(address).AsType_16((ushort)value);
        }

        public int ReadTlut(int address) {
            return m_TextureMemory.GetPointer().Offset((address << 1) + 0x800).AsType_16();
        }

        public static byte ReadRGBA16Low(int offset) => s_ReplicatedRgba[(offset >> 1) & 0x1F];
        public static byte ReadRGBA16Med(int offset) => s_ReplicatedRgba[(offset >> 6) & 0x1F];
        public static byte ReadRGBA16High(int offset) => s_ReplicatedRgba[offset >> 11];

        private void SetupQuadroAddresses(int tmemBaseAddressA, int tmemBaseAddressB, int lshift, int rshift, int s0, int s1) {
            m_TextureAddress[0] = ((tmemBaseAddressA << lshift) + s0) >> rshift;
            m_TextureAddress[1] = ((tmemBaseAddressA << lshift) + s1) >> rshift;
            m_TextureAddress[2] = ((tmemBaseAddressB << lshift) + s0) >> rshift;
            m_TextureAddress[3] = ((tmemBaseAddressB << lshift) + s1) >> rshift;
        }

        private void SetupAddress(int tmemBaseAddress, int lshift, int rshift, int s) {
            TexelAddress = ((tmemBaseAddress << lshift) + s) >> rshift;
        }

        private int TexelAddress {
            get => m_TextureAddress[0];
            set => m_TextureAddress[0] = value;
        }


        private void SwapXorQuadroAddresses(int[] addr, int t0, int t1, int odd, int even) {
            if (t0.IsTrue(1)) {
                addr[0] ^= odd;
                addr[1] ^= odd;
            }
            else {
                addr[0] ^= even;
                addr[1] ^= even;
            }

            if (t1.IsTrue(1)) {
                addr[2] ^= odd;
                addr[3] ^= odd;
            }
            else {
                addr[2] ^= even;
                addr[3] ^= even;
            }
        }

        private void SwapXorQuadroAddresses(int t0, int t1, int odd, int even) {
            SwapXorQuadroAddresses(m_TextureAddress, t0, t1, odd, even);
        }

        private void SwapXorAddress(ref int addr, int t, int odd, int even) {
            if (t.IsTrue(1)) {
                addr ^= odd;
            }
            else {
                addr ^= even;
            }
        }

        private void SwapXorAddress(int t0, int odd, int even) {
            SwapXorAddress(ref m_TextureAddress[0], t0, odd, even);
        }

        private void ClampQuadroAddresses(int[] addr, int mask) {
            addr[0] &= mask;
            addr[1] &= mask;
            addr[2] &= mask;
            addr[3] &= mask;
        }

        private void ClampQuadroAddresses(int mask = 0xFFF) {
            ClampQuadroAddresses(m_TextureAddress, mask);
        }

        public void FetchTexel_RGBA4(int tbase, int s, int t) {
            SetupAddress(tbase, 4, 1, s);
            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            TexelAddress &= 0xFFF;

            var read = ReadTmem8(TexelAddress);
            read = s.IsTrue(1) ? read & 0xF : read >> 4;
            read |= read << 4;
            m_TexelColor[0].SetFromSingle(read);
        }

        public void FetchTexel_RGBA8(int tbase, int s, int t) {
            SetupAddress(tbase, 3, 0, s);
            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            TexelAddress &= 0xFFF;

            var read = ReadTmem8(TexelAddress);
            m_TexelColor[0].SetFromSingle(read);
        }

        public void FetchTexel_RGBA16(int tbase, int s, int t) {
            SetupAddress(tbase, 2, 0, s);
            SwapXorAddress(t, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            TexelAddress &= 0x7FF;

            var read = ReadTmem16(TexelAddress);

            m_TexelColor[0] = new Color {
                    R = ReadRGBA16High(read),
                    G = ReadRGBA16Med(read),
                    B = ReadRGBA16Low(read),
                    A = read.IsTrue(1) ? 0xFF : 0
            };
        }

        public void FetchTexel_RGBA32(int tbase, int s, int t) {
            SetupAddress(tbase, 2, 0, s);
            SwapXorAddress(t, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            TexelAddress &= 0x3FF;

            var read = ReadTmem16(TexelAddress);

            m_TexelColor[0].R = read >> 8;
            m_TexelColor[0].G = read & 0xFF;
            
            read = ReadTmem16(TexelAddress | 0x400);

            m_TexelColor[0].B = read >> 8;
            m_TexelColor[0].A = read & 0xFF;
        }

        public void FetchTexel_YUV4(int tbase, int s, int t) {
            SetupAddress(tbase, 2, 0, s);
            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            TexelAddress &= 0x7FF;

            var read = ReadTmem8(TexelAddress);
            read &= 0xF0;
            read |= read >> 4;

            m_TexelColor[0] = new Color {
                R = read - 0x80,
                G = read - 0x80,
                B = read,
                A = read
            };
        }

        public void FetchTexel_YUV8(int tbase, int s, int t) {
            SetupAddress(tbase, 3, 0, s);
            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            TexelAddress &= 0x7FF;

            var read = ReadTmem8(TexelAddress);

            m_TexelColor[0] = new Color {
                R = read - 0x80,
                G = read - 0x80,
                B = read,
                A = read
            };
        }

        public void FetchTexel_YUV16(int tbase, int s, int t) {
            SetupAddress(tbase, 3, 0, s);
            int low = TexelAddress >> 1;

            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            low ^= t.IsTrue(1) ? WORD_XOR_DWORD_SWAP : WORD_ADDR_XOR;

            TexelAddress &= 0x7FF;
            low &= 0x3FF;

            var read = ReadTmem16(low);
            var y = ReadTmem8(read | 0x800);

            m_TexelColor[0] = new Color {
                R = (read >> 8) - 0x80,
                G = (read & 0xFF) - 0x80,
                B = y,
                A = y
            };
        }

        public void FetchTexel_YUV32(int tbase, int s, int t) {
            SetupAddress(tbase, 3, 0, s);

            int low = TexelAddress >> 1;
            low ^= t.IsTrue(1) ? WORD_XOR_DWORD_SWAP : WORD_ADDR_XOR;
            low &= 0x3FF;

            var read = ReadTmem16(low);

            m_TexelColor[0].R = (read >> 8) - 0x80;
            m_TexelColor[0].G = (read & 0xFF) - 0x80;

            if (s.IsTrue(1)) {
                TexelAddress ^= t.IsTrue(1) ? BYTE_XOR_DWORD_SWAP : BYTE_ADDR_XOR;
                TexelAddress &= 0x7FF;
                m_TexelColor[0].B = m_TexelColor[0].A = ReadTmem8(TexelAddress | 0x800);
            }
            else {
                read = ReadTmem16(low | 0x400);
                m_TexelColor[0].B = read >> 8;
                m_TexelColor[0].A = ((read >> 8) & 0xF) | (read & 0xF0);
            }
        }

        public void FetchTexel_CI4(int tbase, int s, int t, int tilePalette) {
            SetupAddress(tbase, 4, 1, s);
            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            TexelAddress &= 0xFFF;

            var read = ReadTmem8(TexelAddress);
            read = s.IsTrue(1) ? read & 0xF : read >> 4;
            read = (tilePalette << 4) | read;

            m_TexelColor[0].SetFromSingle(read);
        }

        public void FetchTexel_CI8(int tbase, int s, int t) {
            SetupAddress(tbase, 3, 0, s);
            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            TexelAddress &= 0xFFF;

            var read = ReadTmem8(TexelAddress);

            m_TexelColor[0].SetFromSingle(read);
        }

        public void FetchTexel_CI32(int tbase, int s, int t) {
            SetupAddress(tbase, 2, 0, s);
            SwapXorAddress(t, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            TexelAddress &= 0x7FF;

            var read = ReadTmem16(TexelAddress);

            m_TexelColor[0] = new Color {
                R = read >> 8,
                G = read & 0xFF,
                B = read >> 8,
                A = read & 0xFF
            };
        }

        public void FetchTexel_IA4(int tbase, int s, int t) {
            SetupAddress(tbase, 4, 1, s);
            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            TexelAddress &= 0xFFF;

            var read = ReadTmem8(TexelAddress);
            read = s.IsTrue(1) ? read & 0xF : read >> 4;
            var i = read & 0xE;
            i = (i << 4) | (i << 1) | (i >> 2);

            m_TexelColor[0] = new Color {
                R = i,
                G = i,
                B = i,
                A = read.IsTrue(1) ? 0xFF : 0
            };
        }

        public void FetchTexel_IA8(int tbase, int s, int t) {
            SetupAddress(tbase, 3, 0, s);
            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            TexelAddress &= 0xFFF;

            var read = ReadTmem8(TexelAddress);
            var i = read & 0xF0;
            i |= i >> 4;

            m_TexelColor[0] = new Color {
                R = i,
                G = i,
                B = i,
                A = ((read & 0xF) << 4) | (read & 0xF)
            };
        }

        public void FetchTexel_IA16(int tbase, int s, int t) {
            SetupAddress(tbase, 2, 0, s);
            SwapXorAddress(t, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            TexelAddress &= 0x7FF;

            var read = ReadTmem16(TexelAddress);

            m_TexelColor[0] = new Color {
                R = read >> 8,
                G = read >> 8,
                B = read >> 8,
                A = read & 0xFF
            };
        }

        public void FetchTexel_IA32(int tbase, int s, int t) {
            SetupAddress(tbase, 2, 0, s);
            SwapXorAddress(t, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            TexelAddress &= 0x7FF;

            var read = ReadTmem16(TexelAddress);

            m_TexelColor[0] = new Color {
                R = read >> 8,
                G = read & 0xFF,
                B = read >> 8,
                A = read & 0xFF
            };
        }

        public void FetchTexel_I4(int tbase, int s, int t) {
            SetupAddress(tbase, 4, 1, s);
            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            TexelAddress &= 0xFFF;

            var read = ReadTmem8(TexelAddress);
            read = s.IsTrue(1) ? read & 0xF : read >> 4;
            read |= read << 4;

            m_TexelColor[0].SetFromSingle(read);
        }

        public void FetchTexel_I8(int tbase, int s, int t) {
            SetupAddress(tbase, 3, 0, s);
            SwapXorAddress(t, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            TexelAddress &= 0xFFF;

            var read = ReadTmem16(TexelAddress);

            m_TexelColor[0].SetFromSingle(read);
        }

        public void FetchTexel_I32(int tbase, int s, int t) {
            SetupAddress(tbase, 2, 0, s);
            SwapXorAddress(t, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            TexelAddress &= 0x7FF;

            var read = ReadTmem16(TexelAddress);

            m_TexelColor[0] = new Color {
                R = read >> 8,
                G = read & 0xFF,
                B = read >> 8,
                A = read & 0xFF
            };
        }

        public void Quadro_FetchTexel_RGBA4(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 4, 1, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses();

            for (int i = 0; i < 4; i++) {
                int read = ReadTmem8(m_TextureAddress[i]);

                var ands = i % 2 == 0 ? s0.IsTrue(1) : s1.IsTrue(1);

                if (ands) {
                    read &= 0xF;
                }
                else {
                    read >>= 4;
                }

                read |= read << 4;
                m_TexelColor[i].SetFromSingle(read);
            }
        }

        public void Quadro_FetchTexel_RGBA8(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses();

            for (int i = 0; i < 4; i++) {
                int read = ReadTmem8(m_TextureAddress[i]);
                m_TexelColor[i].SetFromSingle(read);
            }
        }

        public void Quadro_FetchTexel_RGBA16(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 2, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            for (int i = 0; i < 4; i++) {
                var read = ReadTmem16(m_TextureAddress[i]);

                m_TexelColor[i] = new Color {
                    R = ReadRGBA16High(read),
                    G = ReadRGBA16Med(read),
                    B = ReadRGBA16Low(read),
                    A = read.IsTrue(1) ? 0xFF : 0
                };
            }
        }

        public void Quadro_FetchTexel_RGBA32(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 2, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            ClampQuadroAddresses(0x3FF);

            for (int i = 0; i < 4; i++) {
                var readRG = ReadTmem16(m_TextureAddress[i]);
                var readBA = ReadTmem16(m_TextureAddress[i] | 0x400);

                m_TexelColor[i] = new Color {
                    R = readRG >> 8,
                    G = readRG & 0xFF,
                    B = readBA >> 8,
                    A = readBA & 0xFF
                };
            }
        }

        public void Quadro_FetchTexel_YUV4(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1, int sdiff, bool unequalUppers) {
            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1 + sdiff);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            for (int i = 0; i < 4; i++) {
                int index = unequalUppers ? 3 - i : i;

                var value = ReadTmem8(m_TextureAddress[i]);
                value &= 0xF0;
                value |= value >> 4;
                value -= 0x80;

                m_TexelColor[i].R = value;
                m_TexelColor[i].G = value;
                m_TexelColor[index].B = value;
                m_TexelColor[index].A = value;
            }
        }

        public void Quadro_FetchTexel_YUV8(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1, int sdiff, bool unequalUppers) {
            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1 + sdiff);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);


            for (int i = 0; i < 4; i++) {
                int index = unequalUppers ? 3 - i : i;

                var value = ReadTmem8(m_TextureAddress[i]) - 0x80;

                m_TexelColor[i].R = value;
                m_TexelColor[i].G = value;
                m_TexelColor[index].B = value;
                m_TexelColor[index].A = value;
            }
        }

        public void Quadro_FetchTexel_YUV16(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1, int sdiff) {
            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);

            var addressLow = new int[] {
                m_TextureAddress[0] >> 1,
                (m_TextureAddress[1] + sdiff) >> 1,
                m_TextureAddress[2] >> 1,
                (m_TextureAddress[3] + sdiff) >> 1
            };

            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            SwapXorQuadroAddresses(addressLow, t0, t1, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);

            ClampQuadroAddresses(0x7FF);
            ClampQuadroAddresses(addressLow, 0x3FF);

            for (int i = 0; i < 4; i++) {
                var c = ReadTmem16(addressLow[i]);
                var y = ReadTmem8(m_TextureAddress[i] | 0x800);
                var u = c >> 8;
                var v = c & 0xFF;

                u -= 0x80;
                v -= 0x80;

                m_TexelColor[i] = new Color {
                    R = u,
                    G = v,
                    B = y,
                    A = y
                };
            }
        }

        public void Quadro_FetchTexel_YUV32(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1, int sdiff) {
            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);

            var addressLow = new int[] {
                m_TextureAddress[0] >> 1,
                (m_TextureAddress[1] + sdiff) >> 1,
                m_TextureAddress[2] >> 1,
                (m_TextureAddress[3] + sdiff) >> 1
            };

            SwapXorQuadroAddresses(addressLow, t0, t1, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            ClampQuadroAddresses(addressLow, 0x3FF);

            for (int i = 0; i < 4; i++) {
                var c = ReadTmem8(addressLow[i]);
                var u = (c >> 8) - 0x80;
                var v = (c & 0xFF) - 0x80;

                m_TexelColor[i] = new Color {
                    R = u,
                    G = v,
                    B = 0,
                    A = 0
                };
            }

            int xor_t0 = t0.IsTrue(1) ? BYTE_XOR_DWORD_SWAP : BYTE_ADDR_XOR;
            int xor_t1 = t1.IsTrue(1) ? BYTE_XOR_DWORD_SWAP : BYTE_ADDR_XOR;

            if (s0.IsTrue(1)) {
                m_TextureAddress[0] ^= xor_t0;
                m_TextureAddress[2] ^= xor_t1;
                m_TextureAddress[0] &= 0x7FF;
                m_TextureAddress[2] &= 0x7FF;
                m_TexelColor[0].B = m_TexelColor[0].A = ReadTmem8(m_TextureAddress[0] | 0x800);
                m_TexelColor[2].B = m_TexelColor[2].A = ReadTmem8(m_TextureAddress[2] | 0x800);
            }
            else {
                var y0 = ReadTmem16(m_TextureAddress[0] | 0x400);
                var y2 = ReadTmem16(m_TextureAddress[2] | 0x400);

                m_TexelColor[0].B = y0 >> 8;
                m_TexelColor[0].A = ((y0 >> 8) & 0xF) | (y0 & 0xF0);
                m_TexelColor[2].B = y2 >> 8;
                m_TexelColor[2].A = ((y2 >> 8) & 0xF) | (y2 & 0xF0);
            }

            if (s1.IsTrue(1)) {
                m_TextureAddress[1] ^= xor_t0;
                m_TextureAddress[3] ^= xor_t1;
                m_TextureAddress[1] &= 0x7FF;
                m_TextureAddress[3] &= 0x7FF;
                m_TexelColor[0].B = m_TexelColor[0].A = ReadTmem8(m_TextureAddress[1] | 0x800);
                m_TexelColor[2].B = m_TexelColor[2].A = ReadTmem8(m_TextureAddress[3] | 0x800);
            }
            else {
                m_TextureAddress[1] ^= xor_t0;
                m_TextureAddress[3] ^= xor_t1;

                m_TextureAddress[1] = (m_TextureAddress[1] >> 1) & 0x3FF;
                m_TextureAddress[3] = (m_TextureAddress[3] >> 1) & 0x3FF;

                var y1 = ReadTmem16(m_TextureAddress[1] | 0x400);
                var y3 = ReadTmem16(m_TextureAddress[3] | 0x400);

                m_TexelColor[1].B = y1 >> 8;
                m_TexelColor[2].A = ((y1 >> 8) & 0xF) | (y1 & 0xF0);
                m_TexelColor[3].B = y3 >> 8;
                m_TexelColor[3].A = ((y3 >> 8) & 0xF) | (y3 & 0xF0);
            }
        }

        public void Quadro_FetchTexel_CI4(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1, int tilePalette) {
            SetupQuadroAddresses(tbaseA, tbaseB, 4, 1, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses();

            for (int i = 0; i < 4; i++) {
                var read = ReadTmem8(m_TextureAddress[i]);

                bool ands = i % 2 == 0 ? s0.IsTrue(1) : s1.IsTrue(1);
                read = ands ? read & 0xF : read >> 4;
                read = (tilePalette << 4) | read;

                m_TexelColor[i].SetFromSingle(read);
            }
        }

        public void Quadro_FetchTexel_CI8(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses();

            for (int i = 0; i < 4; i++) {
                var read = ReadTmem8(m_TextureAddress[i]);

                m_TexelColor[i].SetFromSingle(read);
            }
        }

        public void Quadro_FetchTexel_CI32(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 2, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            for (int i = 0; i < 4; i++) {
                var read = ReadTmem16(m_TextureAddress[i]);

                m_TexelColor[i] = new Color {
                    R = read >> 8,
                    G = read & 0xFF,
                    B = read >> 8,
                    A = read & 0xFF
                };
            }
        }

        public void Quadro_FetchTexel_IA4(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 4, 1, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses();

            for (int i = 0; i < 4; i++) {
                var read = ReadTmem8(m_TextureAddress[i]);

                bool ands = i % 2 == 0 ? s0.IsTrue(1) : s1.IsTrue(1);
                read = ands ? read & 0xF : read >> 4;
                var v = read & 0xE;
                v = (v << 4) | (v << 1) | (v >> 2);

                m_TexelColor[i] = new Color {
                    R = v,
                    G = v,
                    B = v,
                    A = read.IsTrue(1) ? 0xFF : 0
                };
            }
        }

        public void Quadro_FetchTexel_IA8(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses();

            for (int i = 0; i < 4; i++) {
                var read = ReadTmem8(m_TextureAddress[i]);

                var v = read & 0xF0;
                v |= v >> 4;

                m_TexelColor[i] = new Color {
                    R = v,
                    G = v,
                    B = v,
                    A = ((read & 0xF) << 4) | (read & 0xF)
                };
            }
        }

        public void Quadro_FetchTexel_IA16(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 2, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            for (int i = 0; i < 4; i++) {
                var read = ReadTmem16(m_TextureAddress[i]);

                m_TexelColor[i] = new Color {
                    R = read >> 8,
                    G = read >> 8,
                    B = read >> 8,
                    A = read & 0xFF
                };
            }
        }

        public void Quadro_FetchTexel_IA32(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 2, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            for (int i = 0; i < 4; i++) {
                var read = ReadTmem16(m_TextureAddress[i]);

                m_TexelColor[i] = new Color {
                    R = read >> 8,
                    G = read & 0xFF,
                    B = read >> 8,
                    A = read & 0xFF
                };
            }
        }

        public void Quadro_FetchTexel_I4(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 4, 1, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses();

            for (int i = 0; i < 4; i++) {
                bool ands = i % 2 == 0 ? s0.IsTrue(1) : s1.IsTrue(1);
                var read = ReadTmem8(m_TextureAddress[i]);

                var value = ands ? read & 0xF : read >> 4;
                value |= value << 4;

                m_TexelColor[i].SetFromSingle(value);
            }
        }

        public void Quadro_FetchTexel_I8(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses();

            for (int i = 0; i < 4; i++) {
                m_TexelColor[i].SetFromSingle(ReadTmem8(m_TextureAddress[i]));
            }
        }

        public void Quadro_FetchTexel_I32(int tbaseA, int tbaseB, int s0, int s1, int t0, int t1) {
            SetupQuadroAddresses(tbaseA, tbaseB, 2, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            for (int i = 0; i < 4; i++) {
                var read = ReadTmem16(m_TextureAddress[i]);

                m_TexelColor[i] = new Color {
                    R = read >> 8,
                    G = read & 0xFF,
                    B = read >> 8,
                    A = read & 0xFF
                };
            }
        }

        private void QuadroTlut_FetchTexel(int index, bool isUpper, bool isUpperRg, bool isNearest) {
            var xor = isUpperRg ? (WORD_ADDR_XOR ^ 3) : WORD_ADDR_XOR;
            var lookup = !isNearest ? ReadTlut(m_TextureAddress[index] ^ xor) : ReadTlut((m_TextureAddress[0] + index) ^ xor);

            // 16 RGBA
            if (!GS.OtherModes.TextureLookupTableType) {
                m_TexelColor[index].R = ReadRGBA16High(lookup);
                m_TexelColor[index].G = ReadRGBA16Med(lookup);

                if (isUpper == isUpperRg) {
                    m_TexelColor[index].B = ReadRGBA16Low(lookup);
                    m_TexelColor[index].A = lookup.IsTrue(1) ? 0xFF : 0;
                }
                else {
                    var i = !isNearest ? 3 - index : 0;
                    var x = !isNearest ? 0 : (3 - index);
                    lookup = ReadTlut((m_TextureAddress[i] + x) ^ xor);
                    m_TexelColor[3 - index].B = ReadRGBA16Low(lookup);
                    m_TexelColor[3 - index].A = lookup.IsTrue(1) ? 0xFF : 0;
                }
            }

            // Intensity Alpha
            else {
                m_TexelColor[index].R = lookup >> 8;
                m_TexelColor[index].G = lookup >> 8;

                if (isUpper == isUpperRg) {
                    m_TexelColor[index].B = lookup >> 8;
                    m_TexelColor[index].A = lookup & 0xFF;
                }
                else {
                    var i = !isNearest ? 3 - index : 0;
                    var x = !isNearest ? 0 : (3 - index);
                    lookup = ReadTlut((m_TextureAddress[i] + x) ^ xor);
                    m_TexelColor[3 - index].B = lookup >> 8;
                    m_TexelColor[3 - index].A = lookup & 0xFF;
                }
            }
        }

        public void QuadroTlut_FetchTexel_TypeA(int tbaseA, int tbaseB, int s0, int t0, int t1, int sdiff, int tilePalette, bool isUpper, bool isUpperRg, bool isNearest = false) {
            var s1 = s0 + sdiff;

            SetupQuadroAddresses(tbaseA, tbaseB, 4, 1, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            var l = isNearest ? 1 : 4;

            for (int i = 0; i < l; i++) {
                bool ands = i % 2 == 0 ? s0.IsTrue(1) : s1.IsTrue(1);

                var read = ReadTmem8(m_TextureAddress[i]);
                read = ands ? read & 0xF : read >> 4;
                m_TextureAddress[i] = ((tilePalette | read) << 2) + i;
            }

            for (int i = 0; i < 4; i++) {
                QuadroTlut_FetchTexel(i, isUpper, isUpperRg, isNearest);
            }
        }

        public void QuadroTlut_FetchTexel_TypeB(int tbaseA, int tbaseB, int s0, int t0, int t1, int sdiff, int tilePalette, bool isUpper, bool isUpperRg, bool isNearest = false) {
            var s1 = s0 + (sdiff << 1);

            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            var l = isNearest ? 1 : 4;

            for (int i = 0; i < l; i++) {
                var read = ReadTmem8(m_TextureAddress[i]) >> 4;
                m_TextureAddress[i] = ((tilePalette | read) << 2) + i;
            }

            for (int i = 0; i < 4; i++) {
                QuadroTlut_FetchTexel(i, isUpper, isUpperRg, isNearest);
            }
        }

        public void QuadroTlut_FetchTexel_TypeC(int tbaseA, int tbaseB, int s0, int t0, int t1, int sdiff, bool isUpper, bool isUpperRg, bool isNearest = false) {
            var s1 = s0 + sdiff;

            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            var l = isNearest ? 1 : 4;

            for (int i = 0; i < l; i++) {
                var read = ReadTmem8(m_TextureAddress[i]);
                m_TextureAddress[i] = (read << 2) + i;
            }

            for (int i = 0; i < 4; i++) {
                QuadroTlut_FetchTexel(i, isUpper, isUpperRg, isNearest);
            }
        }

        public void QuadroTlut_FetchTexel_TypeD(int tbaseA, int tbaseB, int s0, int t0, int t1, int sdiff, bool isUpper, bool isUpperRg, bool isNearest = false) {
            var s1 = s0 + (sdiff << 1);

            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            var l = isNearest ? 1 : 4;

            for (int i = 0; i < l; i++) {
                var read = ReadTmem8(m_TextureAddress[i]);
                m_TextureAddress[i] = (read << 2) + i;
            }

            for (int i = 0; i < 4; i++) {
                QuadroTlut_FetchTexel(i, isUpper, isUpperRg, isNearest);
            }
        }

        public void QuadroTlut_FetchTexel_TypeE(int tbaseA, int tbaseB, int s0, int t0, int t1, int sdiff, bool isUpper, bool isUpperRg, bool isNearest = false) {
            var s1 = s0 + sdiff;

            SetupQuadroAddresses(tbaseA, tbaseB, 2, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            ClampQuadroAddresses(0x3FF);

            var l = isNearest ? 1 : 4;

            for (int i = 0; i < l; i++) {
                var read = ReadTmem16(m_TextureAddress[i]);

                if (i < 3) {
                    m_TextureAddress[i] = ((read >> 6) & ~3) + i;
                }
                else {
                    m_TextureAddress[i] = (read >> 6) | 3;
                }
            }

            for (int i = 0; i < 4; i++) {
                QuadroTlut_FetchTexel(i, isUpper, isUpperRg, isNearest);
            }
        }

        public void QuadroTlut_FetchTexel_TypeF(int tbaseA, int tbaseB, int s0, int t0, int t1, int sdiff, bool isUpper, bool isUpperRg, bool isNearest = false) {
            var s1 = s0 + (sdiff << 1);

            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            var l = isNearest ? 1 : 4;

            for (int i = 0; i < l; i++) {
                var read = ReadTmem8(m_TextureAddress[i]);
                m_TextureAddress[i] = (read << 2) + i;
            }

            for (int i = 0; i < 4; i++) {
                QuadroTlut_FetchTexel(i, isUpper, isUpperRg, isNearest);
            }
        }

        public void QuadroTlut_FetchTexel_TypeG(int tbaseA, int tbaseB, int s0, int t0, int t1, int sdiff, bool isUpper, bool isUpperRg, bool isNearest = false) {
            var s1 = s0 + sdiff;

            SetupQuadroAddresses(tbaseA, tbaseB, 2, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, WORD_XOR_DWORD_SWAP, WORD_ADDR_XOR);
            ClampQuadroAddresses(0x3FF);

            var l = isNearest ? 1 : 4;

            for (int i = 0; i < l; i++) {
                var read = ReadTmem16(m_TextureAddress[i]);

                if (i < 3) {
                    m_TextureAddress[i] = ((read >> 6) & ~3) + i;
                }
                else {
                    m_TextureAddress[i] = (read >> 6) | 3;
                }
            }

            for (int i = 0; i < 4; i++) {
                QuadroTlut_FetchTexel(i, isUpper, isUpperRg, isNearest);
            }
        }

        public void QuadroTlut_FetchTexel_TypeH(int tbaseA, int tbaseB, int s0, int t0, int t1, int sdiff, bool isUpper, bool isUpperRg, bool isNearest = false) {
            var s1 = s0 + (sdiff << 1);

            SetupQuadroAddresses(tbaseA, tbaseB, 3, 0, s0, s1);
            SwapXorQuadroAddresses(t0, t1, BYTE_XOR_DWORD_SWAP, BYTE_ADDR_XOR);
            ClampQuadroAddresses(0x7FF);

            var l = isNearest ? 1 : 4;

            for (int i = 0; i < l; i++) {
                var read = ReadTmem8(m_TextureAddress[i]);
                m_TextureAddress[i] = (read << 2) + i;
            }

            for (int i = 0; i < 4; i++) {
                QuadroTlut_FetchTexel(i, isUpper, isUpperRg, isNearest);
            }
        }
    }
}