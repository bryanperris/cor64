using System.Text;
using System;
using System.IO;
using cor64.Rdp;
using NUnit.Framework;

namespace Tests {
    public static class RdpTestCaseHelper {
        public static RdpTestCase RdpTest(params string[] commandAsm) {
            return new RdpTestCase {
                RdpAsm = commandAsm
            };
        }

        public static RdpTestCase ExpectDecode(this RdpTestCase testCase, params String[] expected) {
            testCase.ExpectedDecode = expected;
            return testCase;
        }

        public static void Run(this RdpTestCase testCase) {
            // var displayList = Asm.AssembleSingleCommandDisplayList(testCase.RdpAsm);

            throw new NotImplementedException("todo, fix this");

            // /* Dump out the displaylist */
            // StringBuilder sb = new StringBuilder();
            // foreach (var b in displayList) sb.Append(b.ToString("X2"));
            // //Console.WriteLine("Raw DP: " + sb.ToString());

            // var dpStream = new MemoryStream(displayList);
            // DisplayListReader dpReader = new DisplayListReader(dpStream);

            // var decodedCommand = dpReader.ReadDisplayList(0, displayList.Length)[0];
            // var decoded = decodedCommand.ResolveType().ToString();

            // //Console.WriteLine(decoded);

            // var results = decoded.Split(Environment.NewLine);

            // Assert.AreEqual(testCase.ExpectedDecode.Length, results.Length);

            // for (int i = 0; i < testCase.ExpectedDecode.Length; i++) {
            //     Assert.AreEqual(testCase.ExpectedDecode[i], results[i]);
            // }
        }
    }
}