using System.Text;
using System.Globalization;
using System.Threading;

using cor64.IO;
using cor64.RCP;
using GLFW;
using cor64;
using System;
using System.Runtime.InteropServices;
using NLog;
using Veldrid;
using ImGuiNET;
using cor64.Mips.Rsp;
using cor64.PIF;

namespace RunN64.Graphics
{
    public class GLWindow
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly N64System m_System;
        private readonly NativeWindow m_Window;
        private readonly Video m_VideoInterface;
        private readonly DPCInterface m_RdpInterface;
        private readonly Cartridge m_Cart;
        private readonly IntPtr m_GLContextPtr;
        private Veldrid.GraphicsDevice m_VelGLDevice;
        private readonly Veldrid.ImGuiRenderer m_ImGuiRenderer;
        private BackendInfoOpenGL m_GLBackend;
        private CommandList m_ImGuiCommandList;

        private readonly KeyCallback m_KeyboardCallback;
        private readonly CharCallback m_CharCallback;



        private bool m_Created;
        private bool m_WinInit = true;

        private readonly bool m_WorkbenchMode;
        private bool m_IsClosed;

        private readonly RgbaFloat CLEAR_COLOR_WB = new(0.545f, 0.458f, 0.666f, 1.0f);
        private readonly RgbaFloat CLEAR_COLOR = new(0.160f, 0.160f, 0.160f, 1.0f);


        private readonly FBViewWindow m_FBWIndow;
        private readonly RspWindow m_WinRsp;
        private readonly RspDisassemblyWindow m_WinRspDisasm;
        private readonly CpuDisassemblyWindow m_WinCpuDisasm;
        private readonly DebugWindow m_WinDebug;
        private readonly RdpWindow m_WinRdp;

        private readonly GlfwInputSnapshot m_Inputs;


        public GLWindow(N64System system, bool workbenchMode)
        {
            m_System = system;

            Glfw.WindowHint(Hint.ContextVersionMajor, 2);
            Glfw.WindowHint(Hint.ContextVersionMinor, 1);
            Glfw.WindowHint(Hint.Focused, true);
            Glfw.WindowHint(Hint.Resizable, true);
            Glfw.WindowHint(Hint.Doublebuffer, true);

            m_WorkbenchMode = workbenchMode;

            if (!workbenchMode)
            {
                m_Window = new NativeWindow(FBViewWindow.N64_MAX_RESOLUTION_X + 10, FBViewWindow.N64_MAX_RESOLUTION_Y, "Cor64");
            }
            else
            {
                m_Window = new NativeWindow(FBViewWindow.N64_MAX_RESOLUTION_X + 10, FBViewWindow.N64_MAX_RESOLUTION_Y + 80, "Cor64 Workbench");
                Glfw.MaximizeWindow(m_Window);
            }

            m_Inputs = new GlfwInputSnapshot(m_Window);

            m_KeyboardCallback = this.OnKeyboard;
            m_CharCallback = this.OnChar;

            Glfw.SetKeyCallback(m_Window, m_KeyboardCallback);
            Glfw.SetCharCallback(m_Window, m_CharCallback);

            m_Window.SizeChanged += (s, e) => {
                // m_RepositionFB = true;
                m_VelGLDevice.MainSwapchain.Resize((uint)e.Size.Width, (uint)e.Size.Height);
                m_ImGuiRenderer.WindowResized(e.Size.Width, e.Size.Height);
            };

            m_Window.Closing += (s, e) => {
                m_VelGLDevice.WaitForIdle();
                m_ImGuiRenderer.Dispose();
                m_VelGLDevice.Dispose();
            };

            m_Window.Closed += (s, e) => {
                Console.WriteLine("GLFW Window Closed");
                m_IsClosed = true;
            };

            // Glfw.IconifyWindow(m_Window);

            m_VideoInterface = system.DeviceRcp.VideoInterface;
            m_RdpInterface = system.DeviceRcp.DisplayProcessorCommandInterface;
            m_Cart = system.AttachedCartridge;

            m_GLContextPtr = GetNativeContext(m_Window);

            if (m_GLContextPtr == IntPtr.Zero) {
                throw new InvalidOperationException("Failed to get GL Context");
            }

            OpenTK.Graphics.OpenGL.GL.LoadBindings(new GlfwOpenTKContext());
            Console.WriteLine("GL Version: " + OpenTK.Graphics.OpenGL.GL.GetString(OpenTK.Graphics.OpenGL.StringName.Version));
            Console.WriteLine("GL Renderer: " + OpenTK.Graphics.OpenGL.GL.GetString(OpenTK.Graphics.OpenGL.StringName.Renderer));

            BindVeldrid();

            try {
                m_ImGuiRenderer = new ImGuiRenderer(
                    m_VelGLDevice,
                    m_VelGLDevice.MainSwapchain.Framebuffer.OutputDescription,
                    m_Window.Size.Width,
                    m_Window.Size.Height
                );
            }
            catch (System.Exception e) {
                Log.Error("Failed to create ImGUI renderer: " + e.Message);
                throw;
            }

            m_FBWIndow = new FBViewWindow(system, m_VelGLDevice, m_ImGuiRenderer, workbenchMode);

            if (workbenchMode) {
                m_WinRsp = new RspWindow(system);
                m_WinDebug = new DebugWindow(system);
                m_WinRspDisasm = new RspDisassemblyWindow(system);
                m_WinCpuDisasm = new CpuDisassemblyWindow(system);
                m_WinRdp = new RdpWindow(system);
            }
        }

        private void OnKeyboard(IntPtr window, Keys key, int scanCode, InputState state, GLFW.ModifierKeys mods) {

            switch (key) {
                case Keys.A: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.A); break;
                case Keys.B: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.B); break;
                case Keys.Z: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.Z); break;
                case Keys.Left: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.AnalogLeft); break;
                case Keys.Right: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.AnalogRight); break;
                case Keys.Up: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.AnalogUp); break;
                case Keys.Down: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.AnalogDown); break;
                case Keys.Q: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.ShoulderL); break;
                case Keys.W: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.ShoulderR); break;
                case Keys.J: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.CLeft); break;
                case Keys.L: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.CRight); break;
                case Keys.I: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.CUp); break;
                case Keys.K: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.CDown); break;
                case Keys.Enter: m_System.Joys[0].AppendButtons(JoyController.ButtonPress.Start); break;
                default: break;
            }

            m_Inputs.AppendKeyEvent(
                KeyTranslator.MakeKeyEvent(
                    key,
                    state == InputState.Press,
                    state == InputState.Repeat,
                    mods
                ));
        }

        private void OnChar(IntPtr window, uint codePoint) {
            m_Inputs.AppendKeyPress(Char.ConvertFromUtf32((int)codePoint)[0]);
        }

        private void BindVeldrid() {
            GraphicsDeviceOptions options = new(false);

            Veldrid.OpenGL.OpenGLPlatformInfo platformInfo = new(
                m_GLContextPtr,
                Glfw.GetProcAddress,
                _ => Glfw.MakeContextCurrent(m_Window),
                () => Glfw.CurrentContext,
                () => Glfw.MakeContextCurrent(Window.None),
                _ => {}, // No destroying the context
                () => Glfw.SwapBuffers(m_Window),
                sync => Glfw.SwapInterval(sync ? 1 : 0)
            );

            m_VelGLDevice = GraphicsDevice.CreateOpenGL(
                options,
                platformInfo,
                (uint)m_Window.Size.Width,
                (uint)m_Window.Size.Height
            );

            m_GLBackend = m_VelGLDevice.GetOpenGLInfo();
            m_ImGuiCommandList = m_VelGLDevice.ResourceFactory.CreateCommandList();
        }

        public void Start() {
            m_Created = true;

            while (!m_Window.IsClosed)
            {
                m_FBWIndow.Scan();
                Render();
                Thread.Sleep(17);
            }
        }

        public bool IsCreated => m_Created;

        public bool IsClosed => m_IsClosed;

        private void Render()
        {
            if (m_WinInit) {
                Glfw.SetWindowSize(m_Window, m_Window.Size.Width, m_Window.Size.Height);
            }

            Glfw.PollEvents();

            if (m_Window.IsClosed || m_Window.IsClosing) {
                return;
            }

            m_FBWIndow.UpdateFramebufferTex();

            m_Inputs.UpdateMouse();

            m_ImGuiRenderer.Update(1f / 60f, m_Inputs);

            m_Inputs.ClearMouse();
            m_Inputs.ClearKeyboard();

            // m_FBWIndow.UpdatePosition(
            //     new(
            //         (m_Window.Size.Width / 2) - (FBViewWindow.N64_MAX_RESOLUTION_X / 2),
            //         1f
            //     )
            // );

            m_FBWIndow.UpdatePosition(
                new(
                    0,
                    0f
                )
            );

            BuildUI();
            RenderImGui();

            m_VelGLDevice.SwapBuffers();

            m_WinInit = false;
        }

        private void RenderImGui() {
            m_ImGuiCommandList.Begin();
                m_ImGuiCommandList.SetFramebuffer(m_VelGLDevice.MainSwapchain.Framebuffer);

                if (m_WorkbenchMode)
                    m_ImGuiCommandList.ClearColorTarget(0, CLEAR_COLOR_WB);
                else
                    m_ImGuiCommandList.ClearColorTarget(0, CLEAR_COLOR);

                m_FBWIndow.PrepareResources(m_ImGuiCommandList);
                m_ImGuiRenderer.Render(m_VelGLDevice, m_ImGuiCommandList);
            m_ImGuiCommandList.End();

            m_VelGLDevice.SubmitCommands(m_ImGuiCommandList);
        }

        private static IntPtr GetNativeContext(NativeWindow nativeWindow)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Native.GetWglContext(nativeWindow);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Try getting a XGL context (OpenGL on X11)
                var glx = Native.GetGLXContext(nativeWindow);

                // If this fails, try to get the Wayland (EGL) context
                if (glx == GLXContext.None) {
                    var egl = Native.GetEglContext(nativeWindow);

                    if (egl == EGLContext.None) {
                        throw new PlatformNotSupportedException("Could not find a context for X11/Wayland");
                    }

                    return egl;
                }

                return glx;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Native.GetNSGLContext(nativeWindow);
            }

            throw new PlatformNotSupportedException();
        }

        public void TriggerVI()
        {
            m_VideoInterface.SetVideoInterrupt();
        }

        private unsafe void BuildUI()
        {
            m_FBWIndow.Build();

            if (m_WorkbenchMode) {
                m_WinRsp.Build();
                m_WinDebug.Build();
                m_WinRspDisasm.Build();
                m_WinCpuDisasm.Build();
                m_WinRdp.Build();
            }
        }
    }
}