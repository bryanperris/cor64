using System;
using cor64.Rdp.Commands;
using static cor64.Rdp.RdpCommandFlags;

namespace cor64.Rdp {
    public class RdpCommandTypes : RdpCommandTypeFactory {

        private static RuntimeTypeHandle _T<T>()
            where T : RdpCommand 
        {
            return typeof(T).TypeHandle;
        }

        public static readonly RdpCommandType Invalid = Define("Invalid_Op");
        public static readonly RdpCommandType Noop = Define("No_Op");
        public static readonly RdpCommandType TriNoShade = Define("Fill_Triangle", 32, _T<Triangle>());
        public static readonly RdpCommandType TriZNoShade = Define("Fill_ZBuffer_Triangle", 48, ZBuffer, _T<Triangle>());
        public static readonly RdpCommandType TriTex = Define("Texture_Triangle", 96, Texture, _T<Triangle>());
        public static readonly RdpCommandType TriZTex = Define("Texture_ZBuffer_Triangle", 112, ZBuffer | Texture, _T<Triangle>());
        public static readonly RdpCommandType TriShade = Define("Shade_Triangle", 96, Shade, _T<Triangle>());
        public static readonly RdpCommandType TriZShade = Define("Shade_ZBuffer_Triangle", 112, ZBuffer | Shade, _T<Triangle>());
        public static readonly RdpCommandType TriTexShade = Define("Shade_Texture_Triangle", 160, Shade | Texture, _T<Triangle>());
        public static readonly RdpCommandType TriZTexShade = Define("Shade_Texture_Z_Buffer_Triangle", 176, ZBuffer | Shade | Texture, _T<Triangle>());
        public static readonly RdpCommandType TexRect = Define("Texture_Rectangle", 16, Texture, _T<TextureRectangle>());
        public static readonly RdpCommandType TexRectFlip = Define("Texture_Rectangle_Flip", 16, Texture | Flip, _T<TextureRectangleFlip>());
        public static readonly RdpCommandType SyncLoad = Define("Sync_Load", Load, _T<Sync>());
        public static readonly RdpCommandType SyncPipe = Define("Sync_Pipe", Pipeline, _T<Sync>());
        public static readonly RdpCommandType SyncTile = Define("Sync_Tile", Tile, _T<Sync>());
        public static readonly RdpCommandType SyncFull = Define("Sync_Full", Load | Pipeline | Tile, _T<Sync>());
        public static readonly RdpCommandType SetKeyGB = Define("Set_Key_GB", GreenBlue, _T<SetKeyGB>());
        public static readonly RdpCommandType SetKeyRed = Define("Set_Key_R", Red, _T<SetKeyR>());
        public static readonly RdpCommandType SetConvert = Define("Set_Convert", _T<SetConvert>());
        public static readonly RdpCommandType SetScissor = Define("Set_Scissor", _T<SetScissor>());
        public static readonly RdpCommandType SetPrimDepth = Define("Set_Prim_Depth", _T<SetPrimDepth>());
        public static readonly RdpCommandType SetOtherModes = Define("Set_Other_Modes", _T<SetOtherModes>());
        public static readonly RdpCommandType LoadTLUT = Define("Load_Tlut", _T<LoadTlut>());
        public static readonly RdpCommandType SetTileSize = Define("Set_Tile_Size",  _T<SetTileSize>());
        public static readonly RdpCommandType LoadBlock = Define("Load_Block", _T<LoadBlock>());
        public static readonly RdpCommandType LoadTile = Define("Load_Tile", _T<LoadTile>());
        public static readonly RdpCommandType SetTile = Define("Set_Tile", _T<SetTile>());
        public static readonly RdpCommandType FillRect = Define("Fill_Rectangle", _T<FillRectangle>());
        public static readonly RdpCommandType SetFillColor = Define("Set_Fill_Color", Color, _T<SetFillColor>());
        public static readonly RdpCommandType SetFogColor = Define("Set_Fog_Color", Fog, _T<SetFogColor>());
        public static readonly RdpCommandType SetBlendColor = Define("Set_Blend_Color", Blend, _T<SetBlendColor>());
        public static readonly RdpCommandType SetPrimColor = Define("Set_Prim_Color", Primitive, _T<SetPrimColor>());
        public static readonly RdpCommandType SetEnvColor = Define("Set_Env_Color", RdpCommandFlags.Environment, _T<SetEnvColor>());
        public static readonly RdpCommandType SetCombine = Define("Set_Combine_Mode", _T<SetCombineMode>());
        public static readonly RdpCommandType SetTexImage = Define("Set_Texture_Image", Texture, _T<SetTextureImage>());
        public static readonly RdpCommandType SetMaskImage = Define("Set_Z_Image", Mask, _T<SetZImage>());
        public static readonly RdpCommandType SetColorImage = Define("Set_Color_Image", Color, _T<SetColorImage>());
    }
}