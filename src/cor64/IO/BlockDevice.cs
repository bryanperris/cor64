using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    public abstract class BlockDevice : IDisposable
    {
        private static readonly UnmanagedBuffer m_DummyRead = new UnmanagedBuffer(4);
        private static readonly UnmanagedBuffer m_DummyWrite = new UnmanagedBuffer(4);

        public abstract long Size { get; }

        public abstract void Read(long position, byte[] buffer, int offset, int count);

        public abstract void Write(long position, byte[] buffer, int offset, int count);

        public long BaseAddress { get; set; }

        public virtual void WriteNotify(uint position)
        {

        }

        public virtual IntPtr[] GetReadPointerMap()
        {
            IntPtr[] map = new IntPtr[Size / 4];

            for (int i = 0; i < map.Length; i++)
                map[i] = m_DummyRead.GetPointer();

            return map;
        }

        public virtual IntPtr[] GetWritePointerMap()
        {
            IntPtr[] map = new IntPtr[Size / 4];

            for (int i = 0; i < map.Length; i++)
                map[i] = m_DummyWrite.GetPointer();

            return map;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BlockDevice() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
