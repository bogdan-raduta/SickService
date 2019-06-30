using Microsoft.VisualStudio.TestTools.UnitTesting;
using RDS.Logs;
using RDS.Sick;
using SickService;
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
    public class DataParserTests
    {
        readonly Log log = new Log() { FileName = "test.txt" };

        [TestMethod()]
        public void ParseTest()
        {

            DataParser dp = new DataParser(log);
            dp.DataParserConfigs.Add(new DataParserConfig("@CODE1", @"^02\d{28}$"));
            dp.DataParserConfigs.Add(new DataParserConfig("@CODE2", @"^4125900001235\d*"));
            dp.DataParserConfigs.Add(new DataParserConfig("@CODE3", @"^00\d{18}$"));
            dp.Parsed += ParseTest_Parsed;

            dp.Parse(this, new SickScannerEventArgs("\u0002020590007310033915190918370028\u0003\u0002412590000123501024058021810887762\u0003\u000200059005710052701529\u0003"));
        }

        private void ParseTest_Parsed(object sender, DataParserEventArgs e)
        {
            Code c1 = new Code("@CODE1", "020590007310033915190918370028");
            Code c2 = new Code("@CODE2", "412590000123501024058021810887762");
            Code c3 = new Code("@CODE3", "00059005710052701529");

            Code b1 = new Code("@CODE1", "590007310033915190918370028");
            Code b2 = new Code("@CODE2", "020590007310033915190918370028");
            Code b3 = new Code("@CODE3", "00059005710052701");

            Assert.IsTrue(e.Codes.Contains(c1));
            Assert.IsTrue(e.Codes.Contains(c2));
            Assert.IsTrue(e.Codes.Contains(c3));

            Assert.IsFalse(e.Codes.Contains(b1));
            Assert.IsFalse(e.Codes.Contains(b2));
            Assert.IsFalse(e.Codes.Contains(b3));
        }

        [TestMethod()]
        public void HistoricalParseTest()
        {
            DataParser dp = new DataParser(log);
            dp.DataParserConfigs.Add(new DataParserConfig("@CODE1", @"^02\d{28}$"));
            dp.DataParserConfigs.Add(new DataParserConfig("@CODE2", @"^4125900001235\d*"));
            dp.DataParserConfigs.Add(new DataParserConfig("@CODE3", @"^00\d{18}$"));
            dp.Parsed += HistoricalParseTest_Parsed;
            dp.HistoricalCodePattern = @"&gt;02\d{28}&lt;br/&gt;4125900001235\d*&lt;br/&gt;00\d{18}&lt;br/&gt;";
            string html = @"
                <tr>
                <td class =""lc_contenttable_contentcol lc_padl10 lc_high_contentcol"" style=""padding - right:0px; "">3</td>
                < td class =""lc_contenttable_contentcol lc_padl10"" style=""padding-right:0px;"">2</td>
                <td class =""lc_contenttable_contentcol lc_padl10"" style=""padding-right:0px;"">Code Type 2&nbsp;<br/>Code Type 3&nbsp;<br/>&nbsp;<br/></td>
                <td class =""lc_contenttable_contentcol lc_padl10"" style=""padding-right:0px;"">1<br/>1<br/>1<br/></td>
                <td class =""lc_contenttable_contentcol lc_padl10"" style=""padding - right:0px; "">020590007306067115200311370072<br/>412590000123501024068087210884318<br/>00059005710050112402<br/></td>
                </tr>";

            dp.ParseHistorical(null, new SickScannerEventArgs(html));
        }

        private void HistoricalParseTest_Parsed(object sender, DataParserEventArgs e)
        {
            Code c1 = new Code("@CODE1", "020590007306067115200311370072");
            Code c2 = new Code("@CODE2", "412590000123501024068087210884318");
            Code c3 = new Code("@CODE3", "00059005710050112402");

            Code b1 = new Code("@CODE1", "020590007310033915190918370028");
            Code b2 = new Code("@CODE2", "412590000123501024058021810887762");
            Code b3 = new Code("@CODE3", "00059005710052701529");

            Assert.IsTrue(e.Codes.Contains(c1));
            Assert.IsTrue(e.Codes.Contains(c2));
            Assert.IsTrue(e.Codes.Contains(c3));

            Assert.IsFalse(e.Codes.Contains(b1));
            Assert.IsFalse(e.Codes.Contains(b2));
            Assert.IsFalse(e.Codes.Contains(b3));
        }

        [TestMethod()]
        public void CustomFormatProviderTest()
        {
            DataRAWFormatProvider dp = new DataRAWFormatProvider();
            string t = string.Format(dp, "{0:CODE}", "\u0002020590007310033915190918370028\u0003\u0002412590000123501024058021810887762\u0003\u000200059005710052701529\u0003");
            Assert.IsTrue(t == "020590007310033915190918370028;412590000123501024058021810887762;00059005710052701529");

            string[] tmp = t.Split(dp.NewSeparator);
            t = string.Format(dp, "{0:CODE} {1:C}", "\u0002020590007310033915190918370028\u0003\u0002412590000123501024058021810887762\u0003\u000200059005710052701529\u0003", 12.1);
        }
    }
}