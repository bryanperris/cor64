using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using cor64.IO;

namespace cor64.Tests.Mem
{
    public class MemoryTests
    {
        [Fact]
        [Logging]
        public void MemoryDeviceTests()
        {
            //var simpleBlock = new MemoryBlock(100);
            //var buffer = new byte[100];
            //simpleBlock.Read(0, buffer, 0, buffer.Length);

            //Assert.True(Array.TrueForAll(buffer, x => x == 0));
        }
    }
}
