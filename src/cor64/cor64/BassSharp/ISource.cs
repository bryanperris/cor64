using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.BassSharp
{
    public interface ISource
    {
        Stream getStream();
    }
}
