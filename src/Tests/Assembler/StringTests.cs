using cor64.BassSharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Assembler
{
    [TestFixture]
    public class StringTests : BaseTest
    {
        [Test]
        public void ReduceWhitespaceTest()
        {
            var test = "var    b    =  \"my   string value\" + \" another    value\"";
            var expected = "var b = \"my   string value\" + \" another    value\"";

            Assert.AreEqual(expected, Bass.FixWhitespace(test));
        }
    }
}
