using System.Collections.Generic;
using System;

namespace RunN64.Graphics {
    public static class KeyTranslator {
        private static readonly Dictionary<GLFW.Keys, Veldrid.Key> s_KeyMappings = new();

        static KeyTranslator() {
            ADD(GLFW.Keys.Unknown, Veldrid.Key.Unknown);
            ADD(GLFW.Keys.Space, Veldrid.Key.Space);
            ADD(GLFW.Keys.Apostrophe, Veldrid.Key.Quote);
            ADD(GLFW.Keys.Comma, Veldrid.Key.Comma);
            ADD(GLFW.Keys.Minus, Veldrid.Key.Minus);
            ADD(GLFW.Keys.Period, Veldrid.Key.Period);
            ADD(GLFW.Keys.Slash, Veldrid.Key.Slash);
            ADD(GLFW.Keys.Alpha0, Veldrid.Key.Number0);
            ADD(GLFW.Keys.Alpha1, Veldrid.Key.Number1);
            ADD(GLFW.Keys.Alpha2, Veldrid.Key.Number2);
            ADD(GLFW.Keys.Alpha3, Veldrid.Key.Number3);
            ADD(GLFW.Keys.Alpha4, Veldrid.Key.Number4);
            ADD(GLFW.Keys.Alpha5, Veldrid.Key.Number5);
            ADD(GLFW.Keys.Alpha6, Veldrid.Key.Number6);
            ADD(GLFW.Keys.Alpha7, Veldrid.Key.Number7);
            ADD(GLFW.Keys.Alpha8, Veldrid.Key.Number8);
            ADD(GLFW.Keys.Alpha9, Veldrid.Key.Number9);
            ADD(GLFW.Keys.SemiColon, Veldrid.Key.Semicolon);
            ADD(GLFW.Keys.Equal, Veldrid.Key.Plus);
            ADD(GLFW.Keys.A, Veldrid.Key.A);
            ADD(GLFW.Keys.B, Veldrid.Key.B);
            ADD(GLFW.Keys.C, Veldrid.Key.C);
            ADD(GLFW.Keys.D, Veldrid.Key.D);
            ADD(GLFW.Keys.E, Veldrid.Key.E);
            ADD(GLFW.Keys.F, Veldrid.Key.F);
            ADD(GLFW.Keys.G, Veldrid.Key.G);
            ADD(GLFW.Keys.H, Veldrid.Key.H);
            ADD(GLFW.Keys.I, Veldrid.Key.I);
            ADD(GLFW.Keys.J, Veldrid.Key.J);
            ADD(GLFW.Keys.K, Veldrid.Key.K);
            ADD(GLFW.Keys.L, Veldrid.Key.L);
            ADD(GLFW.Keys.M, Veldrid.Key.M);
            ADD(GLFW.Keys.N, Veldrid.Key.N);
            ADD(GLFW.Keys.O, Veldrid.Key.O);
            ADD(GLFW.Keys.P, Veldrid.Key.P);
            ADD(GLFW.Keys.Q, Veldrid.Key.Q);
            ADD(GLFW.Keys.R, Veldrid.Key.R);
            ADD(GLFW.Keys.S, Veldrid.Key.S);
            ADD(GLFW.Keys.T, Veldrid.Key.T);
            ADD(GLFW.Keys.U, Veldrid.Key.U);
            ADD(GLFW.Keys.V, Veldrid.Key.V);
            ADD(GLFW.Keys.W, Veldrid.Key.W);
            ADD(GLFW.Keys.X, Veldrid.Key.X);
            ADD(GLFW.Keys.Y, Veldrid.Key.Y);
            ADD(GLFW.Keys.Z, Veldrid.Key.Z);
            ADD(GLFW.Keys.LeftBracket, Veldrid.Key.LBracket);
            ADD(GLFW.Keys.Backslash, Veldrid.Key.BackSlash);
            ADD(GLFW.Keys.RightBracket, Veldrid.Key.RBracket);
            ADD(GLFW.Keys.GraveAccent, Veldrid.Key.Grave);
            ADD(GLFW.Keys.World1, Veldrid.Key.Unknown);
            ADD(GLFW.Keys.World2, Veldrid.Key.Unknown);
            ADD(GLFW.Keys.Escape, Veldrid.Key.Escape);
            ADD(GLFW.Keys.Enter, Veldrid.Key.Enter);
            ADD(GLFW.Keys.Tab, Veldrid.Key.Tab);
            ADD(GLFW.Keys.Backspace, Veldrid.Key.BackSpace);
            ADD(GLFW.Keys.Insert, Veldrid.Key.Insert);
            ADD(GLFW.Keys.Delete, Veldrid.Key.Delete);
            ADD(GLFW.Keys.Right, Veldrid.Key.Right);
            ADD(GLFW.Keys.Left, Veldrid.Key.Left);
            ADD(GLFW.Keys.Down, Veldrid.Key.Down);
            ADD(GLFW.Keys.Up, Veldrid.Key.Up);
            ADD(GLFW.Keys.PageUp, Veldrid.Key.PageUp);
            ADD(GLFW.Keys.PageDown, Veldrid.Key.PageDown);
            ADD(GLFW.Keys.Home, Veldrid.Key.Home);
            ADD(GLFW.Keys.End, Veldrid.Key.End);
            ADD(GLFW.Keys.CapsLock, Veldrid.Key.CapsLock);
            ADD(GLFW.Keys.ScrollLock, Veldrid.Key.ScrollLock);
            ADD(GLFW.Keys.NumLock, Veldrid.Key.NumLock);
            ADD(GLFW.Keys.PrintScreen, Veldrid.Key.PrintScreen);
            ADD(GLFW.Keys.Pause, Veldrid.Key.Pause);
            ADD(GLFW.Keys.F1, Veldrid.Key.F1);
            ADD(GLFW.Keys.F2, Veldrid.Key.F2);
            ADD(GLFW.Keys.F3, Veldrid.Key.F3);
            ADD(GLFW.Keys.F4, Veldrid.Key.F4);
            ADD(GLFW.Keys.F5, Veldrid.Key.F5);
            ADD(GLFW.Keys.F6, Veldrid.Key.F6);
            ADD(GLFW.Keys.F7, Veldrid.Key.F7);
            ADD(GLFW.Keys.F8, Veldrid.Key.F8);
            ADD(GLFW.Keys.F9, Veldrid.Key.F9);
            ADD(GLFW.Keys.F10, Veldrid.Key.F10);
            ADD(GLFW.Keys.F11, Veldrid.Key.F11);
            ADD(GLFW.Keys.F12, Veldrid.Key.F12);
            ADD(GLFW.Keys.F13, Veldrid.Key.F13);
            ADD(GLFW.Keys.F14, Veldrid.Key.F14);
            ADD(GLFW.Keys.F15, Veldrid.Key.F15);
            ADD(GLFW.Keys.F16, Veldrid.Key.F16);
            ADD(GLFW.Keys.F17, Veldrid.Key.F17);
            ADD(GLFW.Keys.F18, Veldrid.Key.F18);
            ADD(GLFW.Keys.F19, Veldrid.Key.F19);
            ADD(GLFW.Keys.F20, Veldrid.Key.F20);
            ADD(GLFW.Keys.F21, Veldrid.Key.F21);
            ADD(GLFW.Keys.F22, Veldrid.Key.F22);
            ADD(GLFW.Keys.F23, Veldrid.Key.F23);
            ADD(GLFW.Keys.F24, Veldrid.Key.F24);
            ADD(GLFW.Keys.F25, Veldrid.Key.F25);
            ADD(GLFW.Keys.Numpad0, Veldrid.Key.Keypad0);
            ADD(GLFW.Keys.Numpad1, Veldrid.Key.Keypad1);
            ADD(GLFW.Keys.Numpad2, Veldrid.Key.Keypad2);
            ADD(GLFW.Keys.Numpad3, Veldrid.Key.Keypad3);
            ADD(GLFW.Keys.Numpad4, Veldrid.Key.Keypad4);
            ADD(GLFW.Keys.Numpad5, Veldrid.Key.Keypad5);
            ADD(GLFW.Keys.Numpad6, Veldrid.Key.Keypad6);
            ADD(GLFW.Keys.Numpad7, Veldrid.Key.Keypad7);
            ADD(GLFW.Keys.Numpad8, Veldrid.Key.Keypad8);
            ADD(GLFW.Keys.Numpad9, Veldrid.Key.Keypad9);
            ADD(GLFW.Keys.NumpadDecimal, Veldrid.Key.KeypadDecimal);
            ADD(GLFW.Keys.NumpadDivide, Veldrid.Key.KeypadDivide);
            ADD(GLFW.Keys.NumpadMultiply, Veldrid.Key.KeypadMultiply);
            ADD(GLFW.Keys.NumpadSubtract, Veldrid.Key.KeypadSubtract);
            ADD(GLFW.Keys.NumpadAdd, Veldrid.Key.KeypadAdd);
            ADD(GLFW.Keys.NumpadEnter, Veldrid.Key.KeypadEnter);
            ADD(GLFW.Keys.NumpadEqual, Veldrid.Key.KeypadPlus);
            ADD(GLFW.Keys.LeftShift, Veldrid.Key.ShiftLeft);
            ADD(GLFW.Keys.LeftControl, Veldrid.Key.ControlLeft);
            ADD(GLFW.Keys.LeftAlt, Veldrid.Key.AltLeft);
            ADD(GLFW.Keys.LeftSuper, Veldrid.Key.WinLeft);
            ADD(GLFW.Keys.RightShift, Veldrid.Key.ShiftRight);
            ADD(GLFW.Keys.RightControl, Veldrid.Key.ControlRight);
            ADD(GLFW.Keys.RightAlt, Veldrid.Key.AltRight);
            ADD(GLFW.Keys.RightSuper,Veldrid.Key.WinRight);
            ADD(GLFW.Keys.Menu, Veldrid.Key.Menu);
        }

        private static void ADD(GLFW.Keys a, Veldrid.Key b) {
            s_KeyMappings.Add(a, b);
        }

        public static Veldrid.KeyEvent MakeKeyEvent(GLFW.Keys key, bool isDown, bool isRepeat, GLFW.ModifierKeys mods) {
            Veldrid.Key mappedKey;
            Veldrid.ModifierKeys mappedMods = Veldrid.ModifierKeys.None;

            if ((mods & GLFW.ModifierKeys.Alt) != 0) {
                mappedMods |= Veldrid.ModifierKeys.Alt;
            }

            if ((mods & GLFW.ModifierKeys.Control) != 0) {
                mappedMods |= Veldrid.ModifierKeys.Control;
            }

            if ((mods & GLFW.ModifierKeys.Shift) != 0) {
                mappedMods |= Veldrid.ModifierKeys.Shift;
            }

            if (s_KeyMappings.TryGetValue(key, out mappedKey)) {
                return new Veldrid.KeyEvent(
                    mappedKey,
                    isDown,
                    mappedMods,
                    isRepeat
                );
                
            }

            return new Veldrid.KeyEvent(
                Veldrid.Key.Unknown,
                false,
                Veldrid.ModifierKeys.None
            );
        }
    }
}