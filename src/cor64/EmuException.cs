using System;
namespace cor64
{
    public class EmuException : Exception
    {
        public EmuException()
        {
        }

        public EmuException(String message) : base(message)
        {
        }

        public EmuException(String message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
