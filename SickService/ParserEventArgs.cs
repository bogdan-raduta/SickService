using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SickService
{
    public class DataParserEventArgs : EventArgs
    {
        public readonly Code[] Codes;

        public DataParserEventArgs(Code[] codes) => Codes = codes;
    }
}
