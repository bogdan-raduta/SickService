using Microsoft.VisualStudio.TestTools.UnitTesting;
using RDS.Logs;
using RDS.Sick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RDS.Sick.Tests
{
    [TestClass()]
    public class SickScannerTests
    {

        [TestMethod()]
        public void ConnectTest()
        {
            SickScanner ss = new SickScanner("CLV650", LogLevel.Debug);
            ss.Address = "192.168.0.104";
            if (!ss.Read())
                Assert.Fail();
        }

        [TestMethod()]
        public void NoConnectionReadTest()
        {
            SickScanner ss = new SickScanner("CLV650", LogLevel.Debug);
            ss.Address = "192.168.0.104";
            ss.Read();

            Assert.Fail();
        }

        [TestMethod()]
        public void ReconnectTest()
        {
            SickScanner ss = new SickScanner("CLV650", LogLevel.Debug);
            ss.Address = "192.168.0.104";
            ss.Read();

            Assert.Fail();
        }

        [TestMethod()]
        public void StartWorkTest()
        {
            SickScanner ss = new SickScanner("CLV650", LogLevel.Debug);
            ss.Address = "192.168.0.104";
            ss.MailRecipients.Add(new System.Net.Mail.MailAddress("test@example.local"));
            ss.ReconnectTime = 1;
            ss.StartWork();
            string raw = ss.DataRAW;

            
            Assert.Fail();
        }

        [TestMethod()]
        public void SubstringLastTest()
        {
            string a = "CLV650 SUBSTRING(0,3,123456) SUBSTRING(2, 3, 654321) 333333";
            string[] substring = Regex.Matches(a, @"SUBSTRING\(\d+,\d+,\w+\)").Cast<Match>().Select(m => m.Value).Distinct().ToArray();

            SickScanner ss = new SickScanner("CLV650", LogLevel.Debug);
            string[] codes = new string[3] { "123456", "654321", "333333" };
            string template = "@NAME SUBSTRING(0,3,@CODE0) LAST(3,@CODE1) @CODE2";
        }

        [TestMethod()]
        public void SendDisconnectMailTest()
        {
            SickScanner ss = new SickScanner("CLV650", LogLevel.Debug);
            ss.Address = "192.168.0.104";
            ss.MailOnConnectionTry = 1;
            ss.ReconnectTime = 0;
            ss.Read();
            Assert.Fail();
        }

        [TestMethod()]
        public void ReadHistoricalDataRegExTest()
        {
            string html = File.ReadAllText("HistoricalData.txt");
            string[] HistoricalCodes = Regex.Matches(html, @"00\d{18}<br/>").Cast<Match>().Select(m => m.Value).Distinct().ToArray();
            for (int i = 0; i < HistoricalCodes.Length; i++)
                HistoricalCodes[i] = Regex.Match(HistoricalCodes[i], @"\d+").ToString();
        }

        [TestMethod()]
        public void ParameterTest()
        {
            SickScanner ss = new SickScanner("CLV650", LogLevel.Debug);

            #region Port
            bool exception = false;
            try
            {
                ss.Port = -1;
            }
            catch (Exception ex)
            {
                exception = true;
            }

            if (!exception)
                Assert.Fail();

            exception = false;

            try
            {
                ss.Port = 70_000;
            }
            catch (Exception ex)
            {
                exception = true;
            }

            if (!exception)
                Assert.Fail();
            #endregion Port 

            #region Name

            ss.Name = null;
            Assert.AreEqual("Scanner", ss.Name);
            ss.Name = "";
            Assert.AreEqual("Scanner", ss.Name);

            #endregion
        }
    }
}