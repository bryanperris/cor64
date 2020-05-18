using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using cor64.IO;
using cor64.Utils;
using NLog;

namespace cor64
{
    /* Every real cartridge should start with 0x80371240, this means big endian */

    public class Cartridge : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private bool m_Disposed;
        private readonly PinnedBuffer m_RomBuffer;
        private readonly CartridgeBlock m_CartridgeBlock;

        public const int BootSize = 4032;
        public const int HeaderSize = 0x40;
        const int TitleSize = 20;

        /* Cartridge Header */
        private uint m_BusConfig;
        private String m_Name; /* Always 20 chars in length */
        private uint m_ClockRate;
        private uint m_EntryPoint;
        private GameSerial m_Serial;
        private uint m_Crc1;
        private uint m_Crc2;
        private uint m_Release;

        public const uint MAGIC_LITTLE = 0x40123780;
        public const uint MAGIC_BIG = 0x80371240;
        public const uint MAGIC_MIDDLE = 0x37804012;

        public enum RomEndianess
        {
            Unknown,
            Big, /* Big */
            Little, /* Little */
            Middle  /* Middle */
        }

        public Cartridge(Stream source)
        {
            /* Try to determine the endianess based on the first 4 bytes (Big-Endian) */
            byte[] bytes = new byte[4];
            source.Position = 0;
            source.Read(bytes, 0, bytes.Length);
            PiBusConfig = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];

            Stream gameSource;
            Stream nativeSource;
            RomEndianess endianess;

            switch (PiBusConfig)
            {
                case MAGIC_LITTLE:
                    {
                        endianess = RomEndianess.Little;
                        break;
                    }

                case MAGIC_BIG:
                    {
                        endianess = RomEndianess.Big;
                        break;
                    }

                case MAGIC_MIDDLE:
                    {
                        endianess = RomEndianess.Middle;
                        break;
                    }

                default:
                    {
                        endianess = AggressiveRomDetect(source);
                        break;
                    }
            }

            switch (endianess)
            {
                case RomEndianess.Little:
                    {
                        gameSource = GetSwappedStreamBE(endianess, source);
                        nativeSource = GetSwappedStreamLE(endianess, source);
                        Log.Info("Little-Endian Rom");
                        break;
                    }

                case RomEndianess.Big:
                    {
                        gameSource = GetSwappedStreamBE(endianess, source);
                        nativeSource = GetSwappedStreamLE(endianess, source);
                        Log.Info("Big-Endian Rom");
                        break;
                    }

                case RomEndianess.Middle:
                    {
                        gameSource = GetSwappedStreamBE(endianess, source);
                        nativeSource = GetSwappedStreamLE(endianess, source);
                        Log.Info("Middle-Endian Rom");
                        break;
                    }

                default:
                    {
                        throw new EmuException("Unable to detect endianess of rom");
                    }
            }

            /* Create a MD5 checksum */
            // TODO: This is for future use of using MD5 instead of Crc32
            BootChecksumMD5 = GenerateMD5Checksum(gameSource);

            /* Detect the CIC type (Input must be in big endian) */
            if (IPL == null)
            {
                IPL = GetIPLType(gameSource);
            }

            ReadHeader(nativeSource);

            /* Now Copy the rom into memory */
            RomStream = new MemoryStream();
            BinaryReader reader = new BinaryReader(CoreConfig.Current.ByteSwap ? gameSource : nativeSource);
            BinaryWriter writer = new BinaryWriter(RomStream);

            nativeSource.Position = 0;

            for (int i = 0; i < nativeSource.Length; i += 4)
            {
                uint v = reader.ReadUInt32();
                writer.Write(v);
            }

            m_RomBuffer = new PinnedBuffer((int)RomStream.Length);
            byte[] buffer = new byte[RomStream.Length];
            RomStream.Position = 0;
            RomStream.Read(buffer, 0, buffer.Length);
            m_RomBuffer.CopyInto(buffer);

            m_CartridgeBlock = new CartridgeBlock(m_RomBuffer, buffer.Length);

            // XXX: For now we have a memory copy of the ROM in memory twice
        }

        private static Stream GetSwappedStream(RomEndianess endianess, Stream source)
        {
            /* We assume the host is little endian for now but the swap flag will control
               how to read in the rom source. If we are byteswapping, then we force the stream
               to be big endian else it will be forced to be little endian */

            var stream = source;
            stream = CoreConfig.Current.ByteSwap ? GetSwappedStreamBE(endianess, stream) : GetSwappedStreamLE(endianess, stream);
            return stream;
        }

        /// <summary>
        /// Converts source endianess to big endian format
        /// </summary>
        /// <param name="endianess"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        private static Stream GetSwappedStreamBE(RomEndianess endianess, Stream source)
        {
            return endianess switch
            {
                RomEndianess.Little => new Swap32Stream(source),
                RomEndianess.Big => source,
                RomEndianess.Middle => new Swap16Stream(source),
                _ => throw new EmuException("Unable to determine rom endianess"),
            };
        }

        /// <summary>
        /// Converts source endianess to little endian format
        /// </summary>
        /// <param name="endianess"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        private static Stream GetSwappedStreamLE(RomEndianess endianess, Stream source)
        {
            return endianess switch
            {
                RomEndianess.Little => source,
                RomEndianess.Big => new Swap32Stream(source),
                RomEndianess.Middle => new Swap32Stream(new Swap16Stream(source)),
                _ => throw new EmuException("Unable to determine rom endianess"),
            };
        }

        private static byte[] GenerateMD5Checksum(Stream stream)
        {
            byte[] bootSection = new byte[BootSize];
            stream.Position = HeaderSize;
            stream.Read(bootSection, 0, bootSection.Length);
            stream.Position = 0;

            MD5 md5 = MD5.Create();
            md5.Initialize();
            md5.ComputeHash(bootSection);

            return md5.Hash;
        }

        public int GetAudioRate()
        {
            switch (m_Serial.GetRegionType())
            {
                case RegionType.PAL: return 49656530;
                case RegionType.MPAL: return 48628316;
                case RegionType.NTSC:
                default:
                    return 48681812;
            }
        }

        public int GetVideoRate()
        {
            if (m_Serial.GetRegionType() == RegionType.NTSC || m_Serial.GetRegionType() == RegionType.MPAL)
                return 60;
            else
                return 50;
        }

        private static String ConvertFromBytes(byte[] buffer, int index, int count)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                char c = (char)buffer[index + i];
                sb.Append(c);
            }

            return sb.ToString();
        }

        private void ReadHeader(Stream source)
        {
            // Depends on little-endian format

            byte[] header = new byte[HeaderSize];
            byte[] serial = new byte[8];

            // if (CoreConfig.Current.ByteSwap)
            // {
            //     source = new Swap32Stream(source);
            // }

            source.Position = 0;
            source.Read(header, 0, header.Length);

            unsafe
            {
                fixed (void* ptr = header)
                {
                    IntPtr _ptr = (IntPtr)ptr;
                    m_BusConfig = _ptr.AsType_32();
                    m_ClockRate = _ptr.Offset(4).AsType_32();
                    m_EntryPoint = _ptr.Offset(8).AsType_32();
                    m_Release = _ptr.Offset(12).AsType_32();
                    m_Crc1 = _ptr.Offset(16).AsType_32();
                    m_Crc2 = _ptr.Offset(20).AsType_32();
                    /* 8 bytes of padding */
                    // Name (20 bytes)
                    /* 4 bytes of padding */
                    _ptr.Offset(20 + 8 + TitleSize + 4).AsBytes(serial);
                    m_Serial = new GameSerial(serial);
                }
            }

            /* Read the data in big endian */
            source.Position = 0;
            source.Read(header, 0, header.Length);
            m_Name = ConvertFromBytes(header, 20 + 8, TitleSize);
        }

        /* Copies the boot section out of the rom */
        public byte[] DumpBootSection()
        {
            if (RomStream == null)
                throw new InvalidOperationException("No stream source was set");

            byte[] buffer = new byte[BootSize];
            RomStream.Position = HeaderSize;
            RomStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        private IPLType GetIPLType(Stream source)
        {
            // NOTE: This requires the input data to be in big-endian format

            Byte[] bootSection = new byte[BootSize];
            source.Position = HeaderSize;
            source.Read(bootSection, 0, bootSection.Length);

            using Crc32HashAlgorithm alg = new Crc32HashAlgorithm();
            alg.Initialize();
            alg.ComputeHash(bootSection);

            SecurityChipsetType type = SecurityChipsetType.Unknown;

            switch (alg.CrcValue)
            {
                case 0x6170A4A1: type = SecurityChipsetType.X101; break;
                case 0x90BB6CB5: type = SecurityChipsetType.X102; break;
                case 0x0B050EE0: type = SecurityChipsetType.X103; break;
                case 0x98BC2C86: type = SecurityChipsetType.X105; break;
                case 0xACC8580A: type = SecurityChipsetType.X106; break;
                default:
                    {
                        Log.Warn("Unknown IPL Hash: " + alg.CrcValue.ToString("X8"));
                        break;
                    }
            }

            return new IPLType(type, alg.CrcValue);
        }

        private RomEndianess AggressiveRomDetect(Stream source)
        {
            /* Test each known rom format and see if the CIC is valid and the CRC's match */
            var types = new RomEndianess[] { RomEndianess.Big, RomEndianess.Middle, RomEndianess.Little };

            try
            {
                foreach (var type in types)
                {
                    Stream streamBe = GetSwappedStreamBE(type, source);
                    Stream streamLe = GetSwappedStreamLE(type, source);

                    var ipl = GetIPLType(streamBe);

                    if (ipl.Cic == SecurityChipsetType.Unknown)
                    {
                        continue;
                    }

                    CartridgeRomChecksum checksum = new CartridgeRomChecksum(ipl.Cic);

                    byte[] data = new byte[CartridgeRomChecksum.InputSize];
                    streamBe.Position = 0;
                    streamBe.Read(data, 0, data.Length);

                    checksum.ComputeHash(data);

                    ReadHeader(streamLe);

                    if (checksum.CRC1 == m_Crc1 && checksum.CRC2 == m_Crc2)
                    {
                        return type;
                    }
                    else {
                        Log.Debug("Rom Crc verification failed");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return RomEndianess.Unknown;
        }

        public BlockDevice GetBlockDevice()
        {
            return m_CartridgeBlock;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    RomStream.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                m_Disposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Cartridge() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        public IPLType IPL { get; }

        public String Name => m_Name;

        public RegionType Region => m_Serial.GetRegionType();

        public GameSerial Serial => m_Serial;

        public uint EntryPoint => m_EntryPoint;

        public Stream RomStream { get; }

        public uint Crc1 => m_Crc1;

        public uint Crc2 => m_Crc2;

        public byte[] BootChecksumMD5 { get; }

        public uint PiBusConfig { get; private set; }

        public int ClockRate => (int)m_ClockRate;
    }
}
