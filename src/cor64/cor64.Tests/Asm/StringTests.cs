using cor64.BassSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace cor64.Tests.Asm
{
    public class StringTests
    {
        [Fact]
        [Logging]
        public void ReduceWhitespaceTest()
        {
            var test = "var    b    =  \"my   string value\" + \" another    value\"";
            var expected = "var b = \"my   string value\" + \" another    value\"";

            Assert.Equal(expected, Bass.FixWhitespace(test));
        }
    }
}
