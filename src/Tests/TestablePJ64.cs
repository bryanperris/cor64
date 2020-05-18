using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using cor64.IO;
using cor64.Mips;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests
{
    public sealed class TestablePJ6 : ITestableCore
    {
        public static readonly String URL = "http://localhost:1984/test";

        private ProcessorMode m_Mode;
        private TestCase m_TestCase;

        private static Task<WebResponse> MakeRequest(String data)
        {
            UTF8Encoding encoding = new UTF8Encoding();
            byte[] bytes = encoding.GetBytes(data);

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(URL);

            request.Method = "POST";
            request.ContentType = "application/json;charset=utf-8";
            request.ContentLength = bytes.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                // Send the data.
                requestStream.Write(bytes, 0, bytes.Length);
            }

            return request.GetResponseAsync();
        }

        private static Task<WebResponse> MakeRequest(PJ64Message cmd)
        {
            return MakeRequest(
                JsonConvert.SerializeObject(
                    cmd, 
                    Formatting.None, 
                    new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
        }

        //[Fact]
        //public void PingTest()
        //{
        //    Assert.True(Ping());
        //}

        //[Fact]
        //public void InitTest()
        //{
        //    Init(null);
        //}

        private static bool TryCommand(PJ64Message message, out HttpWebResponse response)
        {
            try
            {
                var task = MakeRequest(message);
                task.Wait(30);
                var result = task.Result;

                if (!task.IsCompleted)
                {
                    response = null;
                    return false;
                }
                else
                {
                    response = ((HttpWebResponse)result);
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException)
            {
                response = null;
                return false;
            }
            catch (AggregateException)
            {
                response = null;
                return false;
            }
        }

        private static bool TryCommand(PJ64Message message)
        {
            HttpWebResponse resp = null;
            return TryCommand(message, out resp);
        }

        private static bool TryCommand(String message, out HttpWebResponse response)
        {
            var m = new PJ64Message(message);
            return TryCommand(m, out response);
        }

        private static bool TryCommand(String message)
        {
            HttpWebResponse resp = null;
            return TryCommand(message, out resp);
        }

        public static bool Ping()
        {
            return TryCommand("ping");
        }

        public void Init(TestCase tester)
        {
            m_TestCase = tester;
            var message = new PJ64Message("init");

            //var romStream = new Swap32Stream(tester.GetProgram());
            var romStream = tester.GetProgram();

            byte[] rom = new byte[romStream.Length];
            romStream.Position = 0;
            romStream.Read(rom, 0, rom.Length);
            message.Rom = Convert.ToBase64String(rom);

            message.GprData.Add(tester.SourceA.Key, tester.SourceA.Value.ToString("X16"));


            if (!tester.IsImmediate)
            {
                message.GprData.Add(tester.SourceB.Key, tester.SourceB.Value.ToString("X16"));
            }

            if (!TryCommand(message))
            {
                throw new WebException("Init command was unsucessful!");
            }
        }

        public void StepOnce()
        {
            if (!TryCommand("step"))
            {
                throw new WebException("step command was unsucessful!");
            }
        }

        private static String L(ulong value)
        {
            return value.ToString("X16");
        }

        private static String I(uint value)
        {
            return value.ToString("X8");
        }

        public void TestExpectations()
        {
            var message = new PJ64Message("get");
            HttpWebResponse resp = null;

            if (!TryCommand("get", out resp))
            {
                throw new WebException("step command was unsucessful!");
            }

            StringBuilder data = new StringBuilder();
            var stream = resp.GetResponseStream();
            int read = 0;

            while ((read = stream.ReadByte()) >= 0)
            {
                data.Append((char)read);
            }

            stream.Close();


            var dump = JsonConvert.DeserializeObject<PJ64Dump>(data.ToString());
            bool wordMode = (m_Mode & ProcessorMode.Runtime32) == 0;

            if ((m_TestCase.ExpectationFlags & TestCase.Expectations.Exceptions) == 0)
            {

                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.Result) == TestCase.Expectations.Result)
                {
                    if (!wordMode)
                        Assert.AreEqual(L(m_TestCase.Result.Value), dump.Gpr[m_TestCase.Result.Key.ToString("D2")]);
                    else
                    {
                        /* Test values as 32-bit words */
                        Assert.AreEqual(I((uint)m_TestCase.Result.Value), dump.Gpr[m_TestCase.Result.Key.ToString("D2")].Substring(8));
                    }
                }

                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.ResultLo) == TestCase.Expectations.ResultLo)
                {
                    if (!wordMode)
                    {
                        Assert.AreEqual(L(m_TestCase.ExpectedLo), dump.Lo);
                    }
                    else
                    {
                        Assert.AreEqual(I((uint)m_TestCase.ExpectedLo), dump.Lo.Substring(8));
                    }
                }
                else
                {
                    Assert.AreEqual(L(0UL), dump.Lo);
                }

                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.ResultHi) == TestCase.Expectations.ResultHi)
                {
                    if (!wordMode)
                    {
                        Assert.AreEqual(L(m_TestCase.ExpectedHi), dump.Hi);
                    }
                    else
                    {
                        Assert.AreEqual(I((uint)m_TestCase.ExpectedHi), dump.Hi.Substring(8));
                    }
                }
                else
                {
                    Assert.AreEqual(L(0UL), dump.Hi);
                }
            }

        }

        public void SetProcessorMode(ProcessorMode mode)
        {
            m_Mode = mode;

            /* NOTE: PJ64 Doesn't do anything KSU mode or even 32/64 bit mode
             */

            //PJ64Message message = new PJ64Message("state");
            //String ksuMode = null;

            //String run32 = (mode & ProcessorMode.Runtime32) == ProcessorMode.Runtime32 ? "1" : "0";

            //if ((mode & ProcessorMode.User) == ProcessorMode.User)
            //{
            //    ksuMode = "U" + run32;
            //}
            //else if ((mode & ProcessorMode.Supervisor) == ProcessorMode.Supervisor)
            //{
            //    ksuMode = "S" + run32;
            //}
            //else
            //{
            //    ksuMode = "K" + run32;
            //}

            //message.StateData.Add("ksu", ksuMode);

            //if (!TryCommand(message))
            //{
            //    throw new WebException("state command was unsucessful!");
            //}
        }

        [JsonObject]
        private class PJ64Dump
        {
            [JsonProperty(PropertyName = "gpr")]
            public Dictionary<String, String> Gpr { get; set; }

            [JsonProperty(PropertyName = "hi")]
            public String Hi { get; set; }

            [JsonProperty(PropertyName = "lo")]
            public String Lo { get; set; }
        }

        [JsonObject]
        private class PJ64Message
        {
            public PJ64Message(String command)
            {
                Command = command;
            }

            [JsonProperty(PropertyName = "command")]
            public String Command { get; private set; }

            [JsonProperty(PropertyName = "rom")]
            public String Rom { get; set; }

            [JsonProperty(PropertyName = "gpr")]
            public Dictionary<int, String> GprData { get; } = new Dictionary<int, string>();

            [JsonProperty(PropertyName = "state")]
            public Dictionary<String, String> StateData { get; } = new Dictionary<string, string>();
        }
    }
}
