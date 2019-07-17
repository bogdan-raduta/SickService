using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SickService
{
    public static class StringHelper
    {
        public static string Replace(this string s, Code code)
        {
            return s.Replace(code.Name, code.Value).Replace("@ISHISTORICAL", code.IsHistorical.ToString());
        }
    }
}
