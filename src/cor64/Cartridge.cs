using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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
		private Stream m_RomStream;
		private SecurityChipsetType m_CicType;
		private uint m_BootChecksum;
        private byte[] m_BootChecksumMd5;

        private PinnedBuffer m_RomBuffer;
        private CartridgeBlock m_CartridgeBlock;

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

		public enum RomEndianess
		{
            Unknown,
            Z64, /* Big */
            N64, /* Little */
            V64  /* Middle */
		}

		public Cartridge(Stream source)
		{
            m_RomStream = source;

            /* Determine Endianess */
            byte[] bytes = new byte[4];
            source.Position = 0;
            source.Read(bytes, 0, bytes.Length);
            uint word = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];


            /* NOTICE: The rom should be read as big-endian format
             *         Some embedded resources could get mangled by forcing little-end
             */

            /*
             * TODO: Optimize cartridge IO by caching byte-swapped version into memory used for reads greater than 1 byte
             *       Make all byte reads read from the original big-endian source
             */

            switch (word)
            {
                case 0x40123780: source = new Swap32Stream(source); break; // Little Endian -> Big Endian
                case 0x80371240: break;                                    // Big Endian
                case 0x37804012: source = new Swap16Stream(source); break; // Middle Endian -> Bid Endian
                default: source = DetectEndianessAggressive(source); break;
            }

            /* Create a MD5 checksum */
            // TODO: This is for future use of using MD5 instead of Crc32
            m_BootChecksumMd5 = GenerateMD5Checksum(source);


            /* Detect the CIC type (Input must be in big endian) */
            if (m_CicType == SecurityChipsetType.Unknown)
            {
                m_CicType = DetermineCICType(source, out m_BootChecksum);
            }

            /* Now Copy the rom into memory */
            byte[] temp = new byte[source.Length];
            source.Position = 0;
            source.Read(temp, 0, temp.Length);
            var newSource = new MemoryStream();
            newSource.Write(temp, 0, temp.Length);
            m_RomStream = newSource;

            ReadHeader();

            m_RomBuffer = new PinnedBuffer((int)m_RomStream.Length);
            byte[] buffer = new byte[m_RomStream.Length];
            m_RomStream.Position = 0;
            m_RomStream.Read(buffer, 0, buffer.Length);
            m_RomBuffer.CopyInto(buffer);
            m_CartridgeBlock = new CartridgeBlock(m_RomBuffer, buffer.Length);
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

		private void ReadHeader()
		{
			byte[] header = new byte[HeaderSize];
            byte[] serial = new byte[8];

            /* Swap the rom from big to little */
            var swapped = new Swap32Stream(m_RomStream);
            swapped.Position = 0;
            swapped.Read(header, 0, header.Length);
            
			unsafe
			{
				fixed (void* ptr = header)
				{
					IntPtr _ptr = (IntPtr)ptr;
					m_BusConfig =  _ptr.AsType_32();
					m_ClockRate =  _ptr.Offset(4).AsType_32();
					m_EntryPoint = _ptr.Offset(8).AsType_32();
					m_Release =    _ptr.Offset(12).AsType_32();
					m_Crc1 =       _ptr.Offset(16).AsType_32();
					m_Crc2 =       _ptr.Offset(20).AsType_32();
                    /* 8 bytes of padding */
					// Name (20 bytes)
                    /* 4 bytes of padding */
                    _ptr.Offset(20 + 8 + TitleSize + 4).AsBytes(serial);
                    m_Serial = new GameSerial(serial);
				}
			}

            /* Read the data in big endian */
            m_RomStream.Position = 0;
            m_RomStream.Read(header, 0, header.Length);
            m_Name = ConvertFromBytes(header, 20 + 8, TitleSize);
        }

        /* Copies the boot section out of the rom */
		public byte[] DumpBootSection()
		{
			if (m_RomStream == null)
				throw new InvalidOperationException("No stream source was set");

			byte[] buffer = new byte[BootSize];
			m_RomStream.Position = HeaderSize;
			m_RomStream.Read(buffer, 0, buffer.Length);
			return buffer;
		}
        
		private SecurityChipsetType DetermineCICType(Stream source, out uint bootChecksum)
        {
            // NOTE: This requires the input data to be in big-endian format

            Byte[] bootSection = new byte[BootSize];
            source.Position = HeaderSize;
            source.Read(bootSection, 0, bootSection.Length);

            Crc32HashAlgorithm alg = new Crc32HashAlgorithm();
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
                default: break;
            }

            bootChecksum = alg.CrcValue;

            return type;
        }
        
		private Stream DetectEndianessAggressive(Stream source)
        {
			/* Test each known rom format and see if the CIC is valid and the CRC's match */
			var types = new RomEndianess[] { RomEndianess.Z64, RomEndianess.V64, RomEndianess.N64 };

			try
			{
				foreach (var type in types)
				{
					Stream stream = source;

					switch (type)
					{
						default: break;
						case RomEndianess.N64: stream = new Swap32Stream(source); break;
						case RomEndianess.V64: stream = new Swap16Stream(source); break;
					}

					ReadHeader();
                    m_CicType = DetermineCICType(stream, out m_BootChecksum);

					CartridgeRomChecksum checksum = new CartridgeRomChecksum(m_CicType);

					byte[] data = new byte[CartridgeRomChecksum.InputSize];
					stream.Position = 0;
					stream.Read(data, 0, data.Length);

					checksum.ComputeHash(data);

					if (checksum.CRC1 == m_Crc1 && checksum.CRC2 == m_Crc2)
					{
						return stream;
					}
				}
			}
			catch (Exception e)
			{
				Log.Error(e);
			}

            return source;
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
					m_RomStream.Dispose();
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

		public SecurityChipsetType CICLockoutType => m_CicType;

		public uint BootChecksum => m_BootChecksum;

		public String Name => m_Name;

        public RegionType Region => m_Serial.GetRegionType();

        public GameSerial Serial => m_Serial;

        public uint EntryPoint => m_EntryPoint;

        public Stream RawStream => m_RomStream;

        public uint Crc1 => m_Crc1;

        public uint Crc2 => m_Crc2;

        public byte[] BootChecksumMD5 => m_BootChecksumMd5;
	}
}
