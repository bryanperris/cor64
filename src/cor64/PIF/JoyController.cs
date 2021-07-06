using System;
using System.Collections.Generic;

namespace cor64.PIF {
    public class JoyController {
        private readonly Object m_Lock = new object();
        private ButtonPress m_ButtonPresses = ButtonPress.None;

        private bool m_Pending;

        [Flags]
        public enum ButtonPress : ushort {
            None = 0,
            CRight = 0x0001,
            CLeft = 0x0002,
            CDown = 0x0004,
            CUp   = 0x0008,
            ShoulderL = 0x0010,
            ShoulderR = 0x0020,
            AnalogRight = 0x0100,
            AnalogLeft = 0x0200,
            AnalogDown = 0x0400,
            AnalogUp = 0x0800,
            Start = 0x1000,
            Z = 0x2000,
            B = 0x4000,
            A = 0x8000
        }

        public JoyController(int slot) {
            AssignedSlot = slot;
        }

        public int AssignedSlot { get; }

        public void AppendButtons(ButtonPress buttonPress) {
            lock (m_Lock) {
                m_ButtonPresses |= buttonPress;
                m_Pending = true;
            }
        }

        public bool CheckPending() {
            lock (m_Lock) {
                return m_Pending;
            }
        }

        public ButtonPress ReadPending() {
            lock (m_Lock) {
                m_Pending = false;
                var read = m_ButtonPresses;
                m_ButtonPresses = ButtonPress.None;
                return read;
            }
        }
    }
}