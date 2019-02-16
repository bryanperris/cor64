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
        private Object m_Lock = new Object();

        // XXX: Locks are a bit of a penalty here, so disabled for now

        public abstract long Size { get; }

        public void SafeRead(long position, byte[] buffer, int offset, int count)
        {
            //lock (m_Lock)
            //{
                Read(position, buffer, offset, count);
            //}
        }

		public void SafeWrite(long position, byte[] buffer, int offset, int count)
        {
            //lock (m_Lock)
            //{
                Write(position, buffer, offset, count);
            //}
        }

        protected abstract void Read(long position, byte[] buffer, int offset, int count);

        protected abstract void Write(long position, byte[] buffer, int offset, int count);

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
