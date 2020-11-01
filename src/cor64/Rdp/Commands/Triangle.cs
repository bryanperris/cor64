using System.Text;
using System;

/* Order of coeff (it will be reversed for little-endian )
   ------
   Edge
   Shade
   Texture
   ZBuffer
*/

namespace cor64.Rdp.Commands {
    public class Triangle : RdpCommand {
        private readonly int m_OffsetBase;

        public readonly ShadeCoeff Shade;
        public readonly TextureCoeff Texture;
        public readonly ZCoeff Depth;

        public readonly bool UseShade;
        public readonly bool UseTexture;
        public readonly bool UseZ;

        [ThreadStatic] private static readonly StringBuilder s_StringBuilder = new StringBuilder();

        public Triangle(RdpCommandType type, byte[] data, bool useTex = false) : base(type, data)
        {
            // Edge coeffs
            m_OffsetBase = 4;

            Shade = new ShadeCoeff(m_OffsetBase, type, data);

            if ((type.Flags & RdpCommandFlags.Shade) == RdpCommandFlags.Shade) {
                m_OffsetBase += 8;
                UseShade = true;
            }

            Texture = new TextureCoeff(m_OffsetBase, type, data);

            if (useTex || (type.Flags & RdpCommandFlags.Texture) == RdpCommandFlags.Texture) {
                m_OffsetBase += 8;
                UseTexture = true;
            }

            Depth = new ZCoeff(m_OffsetBase, type, data);

            if ((type.Flags & RdpCommandFlags.ZBuffer) == RdpCommandFlags.ZBuffer) {
                m_OffsetBase += 2;
                UseZ = true;
            }
        }

        public Triangle(RdpCommandType type, byte[] data) : this(type, data, false)
        {

        }

        public RdpCommandFlags TriangleFlags {
            get {
                return this.Type.Flags & (RdpCommandFlags.ZBuffer | RdpCommandFlags.Shade | RdpCommandFlags.Texture);
            }
        }

        public bool TestFlags(RdpCommandFlags flags) {
            return (Type.Flags & flags) == flags;
        }

        public bool IsZ => (TriangleFlags & RdpCommandFlags.ZBuffer) == RdpCommandFlags.ZBuffer;
        public bool IsTex =>(TriangleFlags & RdpCommandFlags.Texture) == RdpCommandFlags.Texture;
        public bool IsShade => (TriangleFlags & RdpCommandFlags.Shade) == RdpCommandFlags.Shade;

        private int ReadInverseSlope(int index, int offset) {
            return ReadField(index, offset * 16, 0xFFFF);
        }

        private int ReadXCoordinate(int index, int offset) {
            return ReadField(index, (offset * 16) + 32, 0xFFFF);
        }

        private int ReadYCoordinate(int index, int offset) {
            return ReadField(index, offset * 16, 0b11111111111111);
        }

        /* ----------------------------------------
            Row 3
        */

        public int MiddleEdge_InverseSlopeF => ReadInverseSlope(3, 0);
        public int MiddleEdge_InverseSlopeI => ReadInverseSlope(3, 1);
        public int MiddleEdge_XCoordF => ReadXCoordinate(3, 0);
        public int MiddleEdge_XCoordI => ReadXCoordinate(3, 1);


        /* ----------------------------------------
            Row 2
        */

        public int DxMDy => (MiddleEdge_InverseSlopeI << 16) | MiddleEdge_InverseSlopeF;
        public int XM => (MiddleEdge_XCoordI << 16) | MiddleEdge_XCoordF;

        public int MajorEdge_InverseSlopeF => ReadInverseSlope(2, 0);
        public int MajorEdge_InverseSlopeI => ReadInverseSlope(2, 1);
        public int MajorEdge_XCoordF => ReadXCoordinate(2, 0);
        public int MajorEdge_XCoordI => ReadXCoordinate(2, 1);


        /* ----------------------------------------
            Row 1
        */

        public int DxHDy => (MajorEdge_InverseSlopeI << 16) | MajorEdge_InverseSlopeF;
        public int XH => (MajorEdge_XCoordI << 16) | MajorEdge_XCoordF;

        public int LowEdge_InverseSlopeF => ReadInverseSlope(1, 0);
        public int LowEdge_InverseSlopeI => ReadInverseSlope(1, 1);
        public int LowEdge_XCoordF => ReadXCoordinate(1, 0);
        public int LowEdge_XCoordI => ReadXCoordinate(1, 1);

        public int DxLDy => (LowEdge_InverseSlopeI << 16) | LowEdge_InverseSlopeF;
        public int XL => (LowEdge_XCoordI << 16) | LowEdge_XCoordF;

        /* ----------------------------------------
            Row 0
        */

        public int MajorEdge_YCoord => ReadYCoordinate(0, 0);
        public int YH => MajorEdge_YCoord;
        public int MiddleEdge_YCoord => ReadYCoordinate(0, 1);
        public int YM => MiddleEdge_YCoord;
        public int LowEdge_YCoord => ReadYCoordinate(0, 2);
        public int YL => LowEdge_YCoord;

        public int Tile => ReadField(0, 48, 0b111);

        public int MipMapCount => ReadField(0, 51, 0b111);

        public bool IsRightMajor => ReadFlag(0, 55);

        protected override String Params() {
            s_StringBuilder.Clear();

            AsmParams_Start();

            AsmParams_AppendParam(String.Format(
                "{0},{1},{2}, {3},{4},{5}, {6},{7},{8},{9}, {10},{11},{12},{13}, {14},{15},{16},{17}",
                IsRightMajor ? 1 : 0, MipMapCount, Tile,
                (short)(ushort)LowEdge_YCoord,
                (short)(ushort)MiddleEdge_YCoord,
                (short)(ushort)MajorEdge_YCoord,
                (short)(ushort)LowEdge_XCoordI,
                (short)(ushort)LowEdge_XCoordF,
                (short)(ushort)LowEdge_InverseSlopeI,
                (short)(ushort)LowEdge_InverseSlopeF,
                (short)(ushort)MajorEdge_XCoordI,
                (short)(ushort)MajorEdge_XCoordF,
                (short)(ushort)MajorEdge_InverseSlopeI,
                (short)(ushort)MajorEdge_InverseSlopeF,
                (short)(ushort)MiddleEdge_XCoordI,
                (short)(ushort)MiddleEdge_XCoordF,
                (short)(ushort)MiddleEdge_InverseSlopeI,
                (short)(ushort)MiddleEdge_InverseSlopeF
            ));

            s_StringBuilder.AppendLine(AsmParams_End());

            if (UseShade) s_StringBuilder.AppendLine(Shade.ToString());
            if (UseTexture) s_StringBuilder.AppendLine(Texture.ToString());
            if (UseZ) s_StringBuilder.AppendLine(Depth.ToString());

            return s_StringBuilder.ToString().TrimEnd();
        }
    }
}