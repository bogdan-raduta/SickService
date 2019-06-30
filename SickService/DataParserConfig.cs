using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SickService
{
    public struct DataParserConfig
    {
        public readonly string Name;
        public readonly string Pattern;

        public DataParserConfig(string name, string pattern) { Name = name; Pattern = pattern; }
    }
}
