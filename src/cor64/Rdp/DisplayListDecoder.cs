using System;
using static cor64.Rdp.RdpCommandTypes;

namespace cor64.Rdp {
    public class DisplayListDecoder {
        private static readonly RdpCommandType[] s_Commands = {
            Noop,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            TriNoShade,
            TriZNoShade,
            TriTex,
            TriZTex,
            TriShade,
            TriZShade,
            TriTexShade,
            TriZTexShade,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            Invalid,
            TexRect,
            TexRectFlip,
            SyncLoad,
            SyncPipe,
            SyncTile,
            SyncFull,
            SetKeyGB,
            SetKeyRed,
            SetConvert,
            SetScissor,
            SetPrimDepth,
            SetOtherModes,
            LoadTLUT,
            Invalid,
            SetTileSize,
            LoadBlock,
            LoadTile,
            SetTile,
            FillRect,
            SetFillColor,
            SetFogColor,
            SetBlendColor,
            SetPrimColor,
            SetEnvColor,
            SetCombine,
            SetTexImage,
            SetMaskImage,
            SetColorImage
        };

        public static RdpCommandType Decode(int commandId) {
            if (commandId < s_Commands.Length) {
                return s_Commands[commandId];
            }
            else {
                throw new ArgumentOutOfRangeException("RDP command id is out of range: " + commandId.ToString("X"));
            }
        }
    }
}