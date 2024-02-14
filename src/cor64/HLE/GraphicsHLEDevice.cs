using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using cor64.Mips;
using cor64.RCP;

namespace cor64.HLE
{
    public abstract class GraphicsHLEDevice
    {
        public delegate nint GLGetProcAddress(string name);
        public delegate void GLSwapBuffers();

        private readonly ConcurrentQueue<Action> m_RenderTaskQueue = new();

        public abstract void Init();
        public abstract void AttachInterface(IntPtr windowPtr, IntPtr cartridgePtr, MipsInterface rcpInterface, SPInterface iface, DPCInterface rdpInterface, Video videoInterface);
        public abstract void AttachGL(uint defaultFramebuffer, GLGetProcAddress getProcAddress, GLSwapBuffers swapBuffers);
        public abstract void ExecuteGfxTask(); // Execute the HLE graphics task
        public abstract void Render(); // Render to the host GPU

        public abstract String Description { get; }

        protected void EnqueueRenderTask(Action task) {
            m_RenderTaskQueue.Enqueue(task);
        }

        public Action GetNextTask() {
            if (m_RenderTaskQueue.TryDequeue(out var task)) {
                return task;
            }
            else {
                return null;
            }
        }
        public int PendingTasks => m_RenderTaskQueue.Count;
    }
}