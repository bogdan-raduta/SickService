using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SickService
{
    public class DataRAWFormatProvider : IFormatProvider, ICustomFormatter
    {
        /// <summary>
        /// Char which each code ends
        /// </summary>
        /// <value>\u0002</value>
        public char StartCodeChar { get; set; } = '\u0002';

        /// <summary>
        /// Char which each code ends
        /// </summary>
        /// <value>\u0003</value>
        public char EndCodeChar { get; set; } = '\u0003';

        public char NewSeparator { get; set; } = ';';

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (format != "CODE")
                return null;

            if (arg.GetType() != typeof(string))
                return arg.ToString();

            string[] tmp = ((string)arg).Replace("\n", "").Replace("\r", "").Replace(StartCodeChar.ToString(), string.Empty).Split(EndCodeChar);

            for (int i = 0; i < tmp.Length; i++)
                tmp[i] = tmp[i].TrimEnd(EndCodeChar);

            return string.Join(NewSeparator.ToString(), tmp).TrimEnd(NewSeparator);
        }

        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
                return this;
            else
                return null;
        }
    }
}
