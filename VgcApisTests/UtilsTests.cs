﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static VgcApis.Libs.Utils;

namespace VgcApisTests
{
    [TestClass]
    public class UtilsTests
    {

        [DataTestMethod]
        [DataRow("vmess.mkcp.tls@whatsurproblem.com", "whatsurproblem.com@vmess.mkcp.tls")]
        [DataRow("vmess.ws.tls@1.2.3.4", "1.2.3.4@vmess.ws.tls")]
        [DataRow("a@b@c", "c@b@a")]
        [DataRow("a@b", "b@a")]
        [DataRow("", "")]
        [DataRow(null, "")]
        [DataRow("a", "a")]
        public void ReverseSummaryTest(string summary, string expect)
        {
            var result = ReverseSummary(summary);
            Assert.AreEqual(expect, result);
        }


        [DataTestMethod]
        [DataRow(
            @"c,,a,//---,中文,3,,2,1,//abc中文,中文,a,1,,中文,a,1",
            @"c,,a,//---,3,中文,,1,2,//abc中文,1,a,中文,,1,a,中文")]
        [DataRow(@"a,b,中文,3,2,1", @"1,2,3,a,b,中文")]
        [DataRow(@"3,2,1", @"1,2,3")]
        public void SortPacListTest(string source, string expStr)
        {
            var testList = source.Split(new char[] { ',' }, StringSplitOptions.None);
            var expectList = expStr.Split(',');
            var result = SortPacList(testList);

            Assert.IsTrue(expectList.SequenceEqual(result));

        }

        [TestMethod]
        public void ClumsyWriterTest()
        {
            var rand = new Random();
            var mainFile = "mainClumsyWriterTest.txt";
            var bakFile = "bakClumsyWriterTest.txt";

            int failCounter = 0;
            int successCounter = 1;

            string lastSuccess = null;

            var cts = new CancellationTokenSource(1000);
            Task.WaitAll(
                Task.Run(() =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var content = rand.Next().ToString();
                        if (ClumsyWriter(content, mainFile, bakFile))
                        {
                            successCounter++;
                            lastSuccess = content;
                        }
                        else
                        {
                            failCounter++;
                        };
                    }
                }),
                Task.Run(() =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var content = rand.Next().ToString();
                        try
                        {
                            File.WriteAllText(mainFile, content);
                        }
                        catch { }
                    }
                }));

            Console.WriteLine($"success: {successCounter}, fail: {failCounter}");
            var read = File.ReadAllText(bakFile);
            Assert.IsTrue(read.Equals(lastSuccess));
        }

        [DataTestMethod]
        [DataRow("a::b:123", true, "a::b", 123)]
        [DataRow("ab123", false, "127.0.0.1", 1080)]
        [DataRow("ab123:", false, "127.0.0.1", 1080)]
        [DataRow(":123", false, "127.0.0.1", 1080)]
        [DataRow(":", false, "127.0.0.1", 1080)]
        public void TryParseIPAddrTest(string address, bool expResult, string expIp, int expPort)
        {
            var result = VgcApis.Libs.Utils.TryParseIPAddr(address, out string ip, out int port);
            Assert.AreEqual(expResult, result);
            Assert.AreEqual(expIp, ip);
            Assert.AreEqual(expPort, port);

        }

        [TestMethod]
        public void AreEqualTest()
        {
            var minVal = VgcApis.Models.Consts.Config.FloatPointNumberTolerance;
            var a = 0.1;
            var b1 = a + minVal * 2;
            var b2 = a - minVal * 2;
            var c1 = a + minVal / 2;
            var c2 = a - minVal / 2;

            Assert.IsFalse(AreEqual(a, b1));
            Assert.IsFalse(AreEqual(a, b2));
            Assert.IsTrue(AreEqual(a, c1));
            Assert.IsTrue(AreEqual(a, c2));
        }


        [DataTestMethod]
        [DataRow(1, 2, 0.6, (long)(0.6 * 1 + 0.4 * 2))]
        [DataRow(-1, 2, 0.6, 2)]
        [DataRow(1, -2, 0.6, 1)]
        [DataRow(-1, -2, 0.6, -1)]
        public void IntegerSpeedtestMeanTest(
            long first,
            long second,
            double weight,
            long expect)
        {
            var result = SpeedtestMean(first, second, weight);
            Assert.AreEqual(expect, result);
        }

        [DataTestMethod]
        [DataRow(0.1, 0.2, 0.3, 0.1 * 0.3 + 0.2 * 0.7)]
        [DataRow(-0.1, 0.2, 0.3, 0.2)]
        [DataRow(0.1, -0.2, 0.3, 0.1)]
        [DataRow(-0.1, -0.2, 0.3, -0.1)]
        public void DoubleSpeedtestMeanTest(
            double first,
            double second,
            double weight,
            double expect)
        {
            var result = SpeedtestMean(first, second, weight);
            Assert.IsTrue(AreEqual(expect, result));
        }

        [DataTestMethod]
        [DataRow(@"o,o.14,o.11,o.1,o.3,o.4", @"o,o.1,o.3,o.4,o.11,o.14")]
        [DataRow(@"b3.2,b3.1.3,a1", @"a1,b3.1.3,b3.2")]
        [DataRow(@"b3,b10,a1", @"a1,b10,b3")]
        [DataRow(@"b,a,1,,", @",,1,a,b")]
        [DataRow(@"c.10.a,a,c.3.b,c.3.a", @"a,c.3.a,c.3.b,c.10.a")]
        public void JsonKeyComparerTest(string rawKeys, string rawExpects)
        {
            var keyList = rawKeys.Split(',').ToList();
            var expect = rawExpects.Split(',');

            keyList.Sort((a, b) => JsonKeyComparer(a, b));

            for (int i = 0; i < keyList.Count; i++)
            {
                Assert.AreEqual(expect[i], keyList[i]);
            }
        }

        [DataTestMethod]
        [DataRow(@"abeec", @"abc", 3)]
        [DataRow(@"abeecee", @"abc", 3)]
        [DataRow(@"eabec", @"abc", 5)]
        [DataRow(@"aeebc", @"abc", 5)]
        [DataRow(@"eeabc", @"abc", 7)]
        [DataRow(@"", @"", 1)]
        [DataRow(@"abc", @"", 1)]
        [DataRow(@"abc", @"abc", 1)]
        public void MeasureSimilarityTest(
            string source, string partial, long expect)
        {
            var result = MeasureSimilarity(source, partial);
            Assert.AreEqual(expect, result);
        }

        [DataTestMethod]
        [DataRow(
            @"{routing:{settings:{rules:[{},{}]},balancers:[{},{}],rules:[{},{}]}}",
            @"routing:{},routing.settings:{},routing.settings.rules:[],routing.settings.rules.0:{},routing.settings.rules.1:{},routing.balancers:[],routing.balancers.0:{},routing.balancers.1:{},routing.rules:[],routing.rules.0:{},routing.rules.1:{}")]
        [DataRow(
            @"{1:[[],[]],'':{},b:123,c:{}}",
            @"c:{}")]
        [DataRow(
            @"{a:[{},{}],b:{}}",
            @"a:[],b:{},a.0:{},a.1:{}")]
        [DataRow(
            @"{a:[[[],[],[]],[[]]],b:{}}",
            @"a:[],b:{},a.0:[],a.1:[]")]
        [DataRow(
            @"{a:[[[],[],[]],[[]]],b:'abc',c:{a:[],b:{d:1}}}",
            @"a:[],a.0:[],a.1:[],c:{},c.a:[],c.b:{}")]
        public void GetterJsonDataStructWorkerTest(string jsonString, string expect)
        {
            var expDict = expect
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split(
                    new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(v => v[0], v => v[1]);

            var jobject = JObject.Parse(jsonString);
            var sections = GetterJsonSections(jobject);

            foreach (var kv in expDict)
            {
                if (kv.Value != sections[kv.Key])
                {
                    Assert.Fail();
                }
            }

            foreach (var kv in sections)
            {
                if (kv.Value != expDict[kv.Key])
                {
                    Assert.Fail();
                }
            }
        }

        [DataTestMethod]
        [DataRow("EvABk文,tv字vvc", "字文", false)]
        [DataRow("EvABk文,tv字vvc", "ab字", true)]
        [DataRow("ab vvvc", "bc", true)]
        [DataRow("abc", "ac", true)]
        [DataRow("", "a", false)]
        [DataRow("", "", true)]
        public void PartialMatchTest(string source, string partial, bool expect)
        {
            var result = PartialMatchCi(source, partial);
            Assert.AreEqual(expect, result);
        }

        [DataTestMethod]
        [DataRow(@"http://abc.com", @"http")]
        [DataRow(@"https://abc.com", @"https")]
        [DataRow(@"VMess://abc.com", @"vmess")]
        public void GetLinkPrefixTest(string link, string expect)
        {
            var prefix = GetLinkPrefix(link);
            Assert.AreEqual(expect, prefix);
        }

        [DataTestMethod]
        [DataRow(@"http://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.http)]
        [DataRow(@"V://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.v)]
        [DataRow(@"vmess://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.vmess)]
        [DataRow(@"v2cfg://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.v2cfg)]
        [DataRow(@"linkTypeNotExist://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.unknow)]
        [DataRow(@"abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.unknow)]
        [DataRow(@"ss://abc.com",
            VgcApis.Models.Datas.Enum.LinkTypes.ss)]
        public void DetectLinkTypeTest(string link, VgcApis.Models.Datas.Enum.LinkTypes expect)
        {
            var linkType = DetectLinkType(link);
            Assert.AreEqual(expect, linkType);
        }


        [DataTestMethod]
        [DataRow(-4, -1)]
        [DataRow(-65535, -1)]
        [DataRow(-65535, -1)]
        [DataRow(0, 0)]
        [DataRow(1, 1)]
        [DataRow(4, 3)]
        [DataRow(8, 4)]
        [DataRow(16, 5)]
        [DataRow(65535, 16)]
        [DataRow(65536, 17)]
        public void GetLenInBitsOfIntTest(int value, int expect)
        {
            var len = GetLenInBitsOfInt(value);
            Assert.AreEqual(expect, len);
        }

        [TestMethod]
        public void GetFreePortMultipleThreadsTest()
        {
            List<int> ports = new List<int>();
            object portsWriteLocker = new object();
            void checkPort(int p)
            {
                lock (portsWriteLocker)
                {
                    if (ports.Contains(p))
                    {
                        Assert.Fail();
                    }
                    ports.Add(p);
                }
            }

            void worker()
            {
                var freePort = GetFreeTcpPort();
                checkPort(freePort);
                IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port: freePort);
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(ep);
                    Sleep(500);
                }
            }

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 500; i++)
            {
                tasks.Add(RunInBackground(worker));
            }

            Task.WaitAll(tasks.ToArray());
        }

        [TestMethod]
        public void GetFreePortSingleThreadTest()
        {
            List<int> ports = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                int port = GetFreeTcpPort();
                Assert.AreEqual(true, port > 0);
                Assert.AreEqual(false, ports.Contains(port));
                ports.Add(port);
            }
        }

        [TestMethod]
        public void LazyGuyTest()
        {
            var str = "";

            void task()
            {
                str += ".";
            }
            var adam = new VgcApis.Libs.Tasks.LazyGuy(task, 100);
            adam.DoItNow();
            Assert.AreEqual(".", str);

            str = "";
            adam.DoItLater();
            adam.ForgetIt();
            Assert.AreEqual("", str);

#if DEBUG
            str = "";
            adam.DoItLater();
            adam.DoItLater();
            adam.DoItLater();
            Thread.Sleep(1000);
            Assert.AreEqual(".", str);

            str = "";
            adam.DoItLater();
            Thread.Sleep(300);
            Assert.AreEqual(".", str);
#endif
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("11,22,abc")]
        public void CloneTest(string orgStr)
        {
            var org = orgStr?.Split(',').ToList();
            var clone = Clone<List<string>>(org);
            var sClone = SerializeObject(clone);
            var sOrg = SerializeObject(org);
            Assert.AreEqual(sOrg, sClone);
        }

        [DataTestMethod]
        [DataRow("0", 0)]
        [DataRow("-1", -1)]
        [DataRow("str-1.234", 0)]
        [DataRow("-1.234str", 0)]
        [DataRow("-1.234", -1)]
        [DataRow("1.432", 1)]
        [DataRow("1.678", 2)]
        [DataRow("-1.678", -2)]
        public void Str2Int(string value, int expect)
        {
            Assert.AreEqual(expect, VgcApis.Libs.Utils.Str2Int(value));
        }
    }
}
