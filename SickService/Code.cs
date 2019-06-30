using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SickService
{
    public struct Code
    {
        public readonly string Name;
        public readonly string Value;

        public Code(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
