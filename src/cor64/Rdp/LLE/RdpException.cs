using System;
namespace cor64.Rdp.LLE {
    public class RdpException : Exception {
        public RdpException() : base()
        {
        }

        public RdpException(string message) : base(message)
        {
        }

        public RdpException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}