using RDS.Logs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SickService
{
    public class DataWriter
    {
        public List<DataWriterConfig> DataWriterConfigs = new List<DataWriterConfig>();

        private Log Log;

        public DataWriter(Log log) { Log = log; }

        /// <summary>
        /// Write codes to file(s)
        /// </summary>
        public void Write(object o, DataParserEventArgs e)
        {
            foreach(DataWriterConfig dwc in DataWriterConfigs)
                try
                { 
                    string fileName = FillTemplate(dwc.FileNameTemplate, e.Codes);
                    string fileContent = FillTemplate(dwc.FileContentTemplate, e.Codes);
                    File.WriteAllText($"{dwc.SavePath}\\{fileName}", fileContent);
                }
                catch (Exception ex)
                {
                    Log.AddError("Unable to save file", ex);
                }
        }

        /// <summary>
        /// Replacing template with values
        /// </summary>
        private string FillTemplate(string template, Code[] codes)
        {
            template = template.Replace("@DATE", DateTime.Now.ToString("yyyyMMdd"));
            template = template.Replace("@TIME", DateTime.Now.ToString("HHmmss"));

            foreach (Code c in codes)
                template = template.Replace(c);

            template = ApplySubstring(template);
            template = ApplyLast(template);

            return template;
        }

        /// <summary>
        /// Apply substring functions on codes e.g SUBSTRING(2,4,@CODE2)
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private string ApplySubstring(string template)
        {
            try
            {
                string[] substring = Regex.Matches(template, @"SUBSTRING\(\d+,\d+,\w+\)").Cast<Match>().Select(m => m.Value).Distinct().ToArray();
                for (int i = 0; i < substring.Length; i++)
                {
                    string[] tmp = substring[i].Replace("SUBSTRING(", "").TrimEnd(')').Split(',');
                    template = template.Replace(substring[i], tmp[2].Substring(int.Parse(tmp[0]), int.Parse(tmp[1])));
                }

                return template;
            }
            catch (Exception ex)
            {
                Log.AddError("Substring function error", ex);
            }
            return template;
        }

        /// <summary>
        /// Apply last functions on codes e.g LAST(6,@CODE2)
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private string ApplyLast(string template)
        {
            try
            {
                string[] last = Regex.Matches(template, @"LAST\(\d+,\w+\)").Cast<Match>().Select(m => m.Value).Distinct().ToArray();
                for (int i = 0; i < last.Length; i++)
                {
                    string[] tmp = last[i].Replace("LAST(", "").TrimEnd(')').Split(',');
                    template = template.Replace(last[i], tmp[1].Substring(tmp[1].Length - int.Parse(tmp[0])));
                }

                return template;
            }
            catch (Exception ex)
            {
                Log.AddError("Last function error", ex);
            }
            return template;
        }
    }
}
