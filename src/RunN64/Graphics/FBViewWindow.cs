using System.Numerics;
using System.Buffers;
using System;
using ImGuiNET;
using cor64;
using cor64.RCP;
using Veldrid;
using cor64.IO;

namespace RunN64.Graphics {
    public class FBViewWindow : ImGuiWindow
    {
        private readonly bool m_WorkbenchMode;
        private readonly N64System m_System;
        private readonly Video m_VideoInterface;
        private readonly VideoControlReg m_VideoCtrl;
        private readonly GraphicsDevice m_Context;
        private readonly ImGuiRenderer m_Renderer;
        private Vector2 m_Position;

        public const int N64_MAX_RESOLUTION_X = 640;
        public const int N64_MAX_RESOLUTION_Y = 480;

        private IntPtr m_ImGuiTextFB;
        private Texture m_RdramTexture;
        private Texture m_N64RenderOutput;
        private int m_FramebufferColorMode = 0;
        private bool m_IsRGB8888 = true;

        private readonly uint BACK_COLOR = ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1));


        public FBViewWindow(
            N64System system,
            GraphicsDevice context,
            ImGuiRenderer renderer,
            bool workbenchMode) : base("RDRAM Framebuffer")
        {
            m_System = system;
            m_Context = context;
            m_Renderer = renderer;
            m_VideoInterface = system.DeviceRcp.VideoInterface;
            m_VideoCtrl = system.DeviceRcp.VideoInterface.ControlReg;
            m_WorkbenchMode = workbenchMode;

            if (!workbenchMode) {
                SetWindowFlags(
                    ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoDecoration |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoResize
                );
            }

            GenerateFramebufferTex(N64_MAX_RESOLUTION_X, N64_MAX_RESOLUTION_Y);
        }

        private void GenerateFramebufferTex(int width, int height) {
            m_RdramTexture = m_Context.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Staging));
            m_N64RenderOutput = m_Context.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            m_ImGuiTextFB = m_Renderer.GetOrCreateImGuiBinding(m_Context.ResourceFactory, m_N64RenderOutput);
        }

        public void CopyRdramToGPU(CommandList list) {
            list.CopyTexture(
                m_RdramTexture, 0, 0, 0, 0, 0,
                m_N64RenderOutput, 0, 0, 0, 0, 0,
                m_RdramTexture.Width, m_RdramTexture.Height, 1, 1
            );
        }

        public void Scan()
        {
            if (IsFBReadActive) {
                m_FramebufferColorMode = m_VideoInterface.ControlReg.GetPixelMode();
                var mode = m_FramebufferColorMode == VideoControlReg.PIXELMODE_32BPP;

                if (mode != m_IsRGB8888 || m_RdramTexture.Width != m_VideoInterface.Width || m_RdramTexture.Height != m_VideoInterface.Height) {

                    if (m_VideoInterface.Width >= 1 && m_VideoInterface.Height >= 1) {
                        m_IsRGB8888 = mode;

                        m_RdramTexture.Dispose();
                        m_N64RenderOutput.Dispose();

                        GenerateFramebufferTex(m_VideoInterface.Width, m_VideoInterface.Height);
                    }
                }
            }
        }

        private static float ComputeScaleFactor(float srcW, float srcH, float dstW, float dstH)
        {
            float dstRatio = dstW / dstH;
            float srcRatio = srcW / srcH;

            if (dstRatio > srcRatio)
            {
                return dstH / srcH;
            }
            else
            {
                return dstW / srcW;
            }
        }

        private bool IsFBReadActive => m_VideoInterface.IsVideoActive && !m_System.DeviceRcp.DisplayProcessorCommandInterface.IsBusy;

        public unsafe void StageRdramTexture() {
            var mp = m_Context.Map(m_RdramTexture, MapMode.Write);

            if (IsFBReadActive)
            {
                if (m_IsRGB8888) {
                    #if HOST_LITTLE_ENDIAN
                    uint * src = (uint*)m_VideoInterface.FramebufferPtr;
                    uint * dst = (uint*)mp.Data;

                    for (int i = 0; i < mp.SizeInBytes; i+=4) {
                        *dst = (*src).ByteSwapped();
                        src++;
                        dst++;
                    }
                    #else
                    Buffer.MemoryCopy((void*)m_VideoInterface.FramebufferPtr, (void*)mp.Data, mp.SizeInBytes, mp.SizeInBytes);
                    #endif
                }
                else {
                    // Convert RGBA5551 to RGBA8888
                    // BE: RRRRRGGG | GGBBBBBA | AA000000 | 00000000
                    // LE: 00000000 | AA000000 | GGBBBBBA | RRRRRGGG

                    ushort * readPtr = (ushort *)m_VideoInterface.FramebufferPtr;
                    uint * writePtr = (uint *)mp.Data;

                    int readOffset = 0;
                    int off = 1;

                    for (uint i = 0; i < mp.SizeInBytes; i+=4) {
                        #if HOST_LITTLE_ENDIAN
                        /// NATIVE-ENDIANESS: LITTLE
                        uint read = *(readPtr + off + readOffset++);
                        off = -off;
                        #else
                        // NATIVE-ENDIANESS: BIG (GL_RGBA8888 on BE is always little endian)?
                        uint read = (*(readPtr + readOffset++)).ByteSwapped();
                        #endif

                        // Big Endian
                        uint b = (read >> 1) & 0x1F;
                        uint g = (read >> 6) & 0x1F;
                        uint r = (read >> 11) & 0x1F;

                        r = ((r * 255) + 15) / 31;
                        g = ((g * 255) + 15) / 31;
                        b = ((b * 255) + 15) / 31;

                        r &= 0xFF;
                        g &= 0xFF;
                        b &= 0xFF;

                        // ABGR 8888
                        *writePtr = 0xFF000000 | (b << 16) | (g << 8) | r;
                        writePtr++;
                    }
                }
            }
            else
            {
                for (uint i = 0; i < mp.SizeInBytes; i++) {
                    mp.Data.AsType_8(0);
                }
            }

            m_Context.Unmap(m_RdramTexture);
        }

        public void UpdatePosition(Vector2 position) {
            m_Position = position;
        }

        protected override void BuildComponents()
        {
                if (!m_WorkbenchMode) {
                    ImGui.SetWindowPos(m_Position);
                    ImGui.SetWindowSize(new System.Numerics.Vector2(N64_MAX_RESOLUTION_X + 20, N64_MAX_RESOLUTION_Y + 10));
                }
                else {
                    String colorMode = m_VideoCtrl.GetPixelMode() switch
                    {
                        VideoControlReg.PIXELMODE_16BPP => "16BPP",
                        VideoControlReg.PIXELMODE_32BPP => "32BPP",
                        _ => "No Video",
                    };

                     var info = String.Format("{0} | {1}x{2} | {3:X8} | {4:X8}/{5:X8}",
                        colorMode,
                        m_VideoInterface.Width,
                        m_VideoInterface.Height,
                        m_VideoInterface.ReadFramebufferAddressSafe(),
                        m_System.DeviceCPU.Cop0.TimerCount,
                        m_System.DeviceCPU.Cop0.TimerMax
                    );


                    ImGui.Text(info);
                }

                float sf = ComputeScaleFactor(m_VideoInterface.Width, m_VideoInterface.Height, N64_MAX_RESOLUTION_X, N64_MAX_RESOLUTION_Y);

                var size = new System.Numerics.Vector2(m_VideoInterface.Width * sf, m_VideoInterface.Height * sf);

                // Draw a black box behind the image since alpha is enabled
                var dl = ImGui.GetWindowDrawList();

                var pos = ImGui.GetCursorScreenPos();

                dl.AddRectFilled(
                    pos,
                    new Vector2(pos.X + size.X, pos.Y + size.Y),
                    BACK_COLOR);

                ImGui.Image(
                    m_ImGuiTextFB,
                    size,
                    new System.Numerics.Vector2(0.0f, 0.0f),
                    new System.Numerics.Vector2(1.0f, 1.0f)
                );

                ImGui.End();
        }
    }
}