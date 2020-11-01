using System;

namespace cor64.Rdp {
    public struct RdpCommandType {
        public readonly String Name;
        public readonly RdpCommandFlags Flags;
        public readonly int Size;
        public readonly int Id;

        public readonly RuntimeTypeHandle? AssoicatedClassType;

        public RdpCommandType(int id, String name, int size, RdpCommandFlags flags, RuntimeTypeHandle? associatedClassType = null) {
            Name = name;
            Size = size;
            Flags = flags;
            Id = id;
            AssoicatedClassType = associatedClassType;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj);
        }

        public bool Equals(RdpCommandType type)
        {
            if (Object.ReferenceEquals(this, type))
            {
                return true;
            }

            if (this.GetType() != type.GetType())
            {
                return false;
            }

            return this.Id == type.Id;
        }

        public override int GetHashCode()
        {
            return this.Id;
        }

        public static bool operator ==(RdpCommandType lhs, RdpCommandType rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(RdpCommandType lhs, RdpCommandType rhs)
        {
            return !(lhs == rhs);
        }
    }

    [Flags]
    public enum RdpCommandFlags : ushort {
        None = 0,
        ZBuffer   = 0b1,
        Shade   =   0b10,
        Texture   = 0b100,
        Flip      = 0b1000,
        Load      = 0b10000,
        Pipeline  = 0b100000,
        Tile      = 0b1000000,
        GreenBlue  =0b10000000,
        Red        =0b100000000,
        Color      =0b1000000000,
        Blend      =0b10000000000,
        Fog        =0b100000000000,
        Environment=0b1000000000000,
        Primitive  =0b10000000000000,
        Mask       =0b100000000000000
    }
}