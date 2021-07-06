using System;
using OpenTK;
using GLFW;

namespace RunN64.Graphics {
    public sealed class GlfwOpenTKContext : IBindingsContext
    {
        public IntPtr GetProcAddress(string procName)
        {
            return Glfw.GetProcAddress(procName);
        }
    }
}