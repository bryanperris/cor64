using System;
using Veldrid;
using GLFW;
using System.Collections.Generic;
using System.Numerics;

namespace RunN64.Graphics {
    public sealed class GlfwInputSnapshot : InputSnapshot
    {
        private readonly Window m_Window;
        private Vector2 m_MousePosition;
        private readonly List<KeyEvent> m_KeyEvents = new List<KeyEvent>();
        private readonly List<MouseEvent> m_MouseEvents = new List<MouseEvent>();
        private readonly List<char> m_KeyCharPresses = new List<char>();

        public GlfwInputSnapshot(Window window) {
            m_Window = window;
        }

        public void UpdateMouse() {
            double mouseX, mouseY;

            Glfw.GetCursorPosition(m_Window, out mouseX, out mouseY);
            m_MousePosition = new Vector2((float)mouseX, (float)mouseY);

            var ms = Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Left);

            if (ms.HasFlag(InputState.Press)) {
                m_MouseEvents.Add(new MouseEvent(Veldrid.MouseButton.Left, true));
            }
        }

        public void AppendKeyEvent(KeyEvent keyEvent) => m_KeyEvents.Add(keyEvent);

        public void AppendKeyPress(Char c) => m_KeyCharPresses.Add(c);

        public IReadOnlyList<KeyEvent> KeyEvents => m_KeyEvents;

        public IReadOnlyList<MouseEvent> MouseEvents => m_MouseEvents;

        public IReadOnlyList<char> KeyCharPresses => m_KeyCharPresses;

        public Vector2 MousePosition => m_MousePosition;

        public float WheelDelta => 0.0f;

        public bool IsMouseDown(Veldrid.MouseButton button)
        {
            return button switch
            {
                Veldrid.MouseButton.Left => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Left) == InputState.Press,
                Veldrid.MouseButton.Middle => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Middle) == InputState.Press,
                Veldrid.MouseButton.Right => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Right) == InputState.Press,
                Veldrid.MouseButton.Button1 => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Button1) == InputState.Press,
                Veldrid.MouseButton.Button2 => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Button2) == InputState.Press,
                Veldrid.MouseButton.Button3 => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Button3) == InputState.Press,
                Veldrid.MouseButton.Button4 => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Button4) == InputState.Press,
                Veldrid.MouseButton.Button5 => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Button5) == InputState.Press,
                Veldrid.MouseButton.Button6 => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Button6) == InputState.Press,
                Veldrid.MouseButton.Button7 => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Button7) == InputState.Press,
                Veldrid.MouseButton.Button8 => Glfw.GetMouseButton(m_Window, GLFW.MouseButton.Button8) == InputState.Press,
                _ => false,
            };
        }

        internal void ClearMouse()
        {
            m_MouseEvents.Clear();
        }

        internal void ClearKeyboard() {
            m_KeyEvents.Clear();
            m_KeyCharPresses.Clear();
        }
    }
}