using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace cor64.Tests.Cpu
{
    public class BitTests
    {
        [Fact]
        [Logging]
        public void BitFields()
        {
            uint value = 0b00000000000000000000000000000001U;
            BitFiddler f = new BitFiddler();
            f.DefineField(0, 1);
            uint v = f.Extract(0, ref value);
            Assert.Equal(1U, v);

            value = 0b00001111100000000000000000000000U;
            f = new BitFiddler();
            f.DefineField(27, 5);
            v = f.Extract(0, ref value);
            Assert.Equal(0b11111U, v);

            value = 0b00001111100000000000000000000000U;
            f = new BitFiddler();
            f.DefineField(27, 5);
            v = f.X(0, ref value);
            Assert.Equal(0b11111U, v);

            value = 0b00000000000000000000000000000000U;
            f = new BitFiddler();
            f.DefineField(27, 5);
            f.Inject(0, ref value, 0b11111U);
            Assert.Equal(0b00001111100000000000000000000000U, value);

            value = 0b00000000000000000000000000000000U;
            f = new BitFiddler();
            f.DefineField(27, 5);
            f.J(0, ref value, 0b11111U);
            Assert.Equal(0b00001111100000000000000000000000U, value);
        }
    }
}
