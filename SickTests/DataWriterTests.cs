using Microsoft.VisualStudio.TestTools.UnitTesting;
using RDS.Logs;
using SickService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.Sick.Tests
{
    [TestClass()]
    public class DataWriterTests
    {
        readonly Log log = new Log();

        [TestMethod()]
        public void WriteTest()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            DataWriterConfig dwc1 = new DataWriterConfig("@CODE1.txt", path, "@CODE1;@CODE2");
            DataWriterConfig dwc2 = new DataWriterConfig("@CODE2.txt", path, "LAST(3,@CODE1);SUBSTRING(2,5,@CODE2)");
            DataWriter dw = new DataWriter(log);
            //dw.DataWriterConfigs.Add(dwc1);
            dw.DataWriterConfigs.Add(dwc2);

            Code[] codes = new Code[2];
            codes[0] = new Code("@CODE1", "012345678");
            codes[1] = new Code("@CODE2", "0987654321");
            
            DataParserEventArgs dpea = new DataParserEventArgs(codes);
            dw.Write(this, dpea);

            string tmp = File.ReadAllText($"{path}\\012345678.txt");
            Assert.IsTrue(tmp == "012345678;0987654321");
            tmp = File.ReadAllText($"{path}\\0987654321.txt");
            Assert.IsTrue(tmp == "678;87654");
        }
    }
}
