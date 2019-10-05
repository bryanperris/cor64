using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace cor64.BassSharp
{
    [Serializable]
    public sealed class Error : Exception
    {
        public Error() : base()
        {

        }

        public Error(String message) : base(message)
        {

        }

        public Error(String message, Exception innerException) :
            base(message, innerException)
        {

        }

        public Error(String message, params object[] args) :
            this(String.Format(message, args))
        {

        }

        public Error(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {

        }
    }
}
