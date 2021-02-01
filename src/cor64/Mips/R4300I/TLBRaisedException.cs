using System;

namespace cor64.Mips.R4300I {
    public class TLBRaisedException : Exception
    {
        public TLBRaisedException()
        {
        }

        public TLBRaisedException(String message) : base(message)
        {
        }

        public TLBRaisedException(String message, Exception innerException) : base(message, innerException)
        {
        }
    }
}