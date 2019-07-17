using RDS.Logs;
using RDS.Sick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SickService
{
    public class DataParser
    {
        public DataRAWFormatProvider DataRAWFormatProvider = new DataRAWFormatProvider();

        /// <summary>
        /// List of configures to use
        /// </summary>
        public List<DataParserConfig> DataParserConfigs = new List<DataParserConfig>();

        private Log Log;

        /// <summary>
        /// Reg expression to find proper code on web page to save
        /// </summary>
        public string HistoricalCodePattern { get; set; }

        public event EventHandler<DataParserEventArgs> Parsed;
        virtual protected void OnParsed(DataParserEventArgs e) => Parsed?.Invoke(this, e);

        public DataParser(Log log) { Log = log; }

        /// <summary>
        /// Parse RAW data to codes
        /// </summary>
        public void Parse(object o, SickScannerEventArgs e)
        {
            if (e.DataRAW.Contains("NoRead"))
                return;
            string[] tmp = string.Format(DataRAWFormatProvider,"{0:CODE}", e.DataRAW).Split(DataRAWFormatProvider.NewSeparator);
            
            Code[] tmpCodes = new Code[DataParserConfigs.Count];
            int j = 0;
            foreach (DataParserConfig dpc in DataParserConfigs)
            {
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (Regex.IsMatch(tmp[i], dpc.Pattern))
                    {
                        tmpCodes[j++] = new Code(dpc.Name, tmp[i], e.IsHistorical);
                        break;
                    }
                }
            }
            OnParsed(new DataParserEventArgs(tmpCodes));
        }

        /// <summary>
        /// Parse HTML to codes
        /// </summary>
        public void ParseHistorical(object o , SickScannerEventArgs e)
        {
            string[] historicalCodes = Regex.Matches(e.DataRAW, HistoricalCodePattern).Cast<Match>().Select(m => m.Value).Distinct().ToArray();
            Log.Add($"Parsed {historicalCodes.Length} group codes from historical data");

            for (int i = 0; i < historicalCodes.Length; i++)
            {
                //input >020590007310033915190918370028<br/>412590000123501024058021810887762<br/>00059005710052701543<br/>
                //output \u0002020590007310033915190918370028\u0003\u0002412590000123501024058021810887762\u0003\u000200059005710052701529\u0003
                string dataRAW = string.Format("\u0002{0}", historicalCodes[i].Replace("<br/>", "\u0003\u0002").TrimEnd('\u0002').TrimStart('>'));
                Log.Add(string.Format("Historical Data RAW: {0}", dataRAW), LogLevel.Debug);
                Parse(o, new SickScannerEventArgs(dataRAW, true));               
            }        
        }
    }
}
