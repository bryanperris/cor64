using cor64.BassSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64
{
    public abstract class NamedAssemblySource : ISource
    {
        public abstract String Name { get; }

        public abstract Stream GetStream();
    }
}
