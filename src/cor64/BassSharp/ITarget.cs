using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    public interface ITarget
    {
        void WriteLE(ulong data, int len);

        void WriteBE(ulong data, int len);

        void Close();

        void Seek(long offset);

        Stream GetStream();
    }
}
