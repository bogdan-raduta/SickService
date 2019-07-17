using RDS.Logs;
using RDS.Sick;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Mail;
using System.ServiceProcess;
using System.Threading;

namespace SickService
{
    public partial class SickService : ServiceBase
    {
        private Log log = new Log();
        private List<SickScanner> Scanners = new List<SickScanner>();
        private List<Thread> Threads = new List<Thread>();

        public SickService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            log.Add("Service started");
            
            try
            {
                LoadConfig();
                StartThreads();
            }
            catch (Exception ex)
            {
                log.AddError("Unknown error", ex);
                OnStop();
            }
        }

        private void LoadConfig()
        {
            DataSet config = new DataSet();
            config.ReadXml(string.Format("{0}\\ScannersConfig.xml", AppDomain.CurrentDomain.BaseDirectory));

            int licence = 0;

            foreach (DataRow drScanner in config.Tables["Scanner"].Rows)
            {
#pragma warning disable
                SickScanner ss = new SickScanner((string)drScanner["Name"], LogLevel.Debug);
                ss.Address = (string)drScanner["Address"];
                ss.Port = int.Parse((string)drScanner["Port"]);
                ss.ReconnectTime = int.Parse((string)drScanner["ReconnectTime"]);
                ss.Log.Path = (string)drScanner["LogFilePath"];
                ss.Log.FileName = ((string)drScanner["LogFileName"]).Replace("@NAME", ss.Name);
#pragma warning restore
                switch ((string)drScanner["LogLevel"])
                {
                    case "Debug":
                        ss.Log.Level = LogLevel.Debug;
                        break;
                    case "Normal":
                        ss.Log.Level = LogLevel.Normal;
                        break;
                }

                if (bool.TryParse((string)drScanner["EnableHistoricalRead"], out bool enableHistoricalRead))
                    ss.EnableHistoricalRead = enableHistoricalRead;
                if (uint.TryParse((string)drScanner["MailOnConnectionTry"], out uint mailOnConnectionTry))
                    ss.MailOnConnectionTry = mailOnConnectionTry;
                string[] recipients = ((string)drScanner["MailRecipients"]).Split(';');
                foreach (string recipient in recipients)
                    ss.MailRecipients.Add(new MailAddress(recipient));

#pragma warning disable
                DataParser dp = new DataParser(ss.Log);
                dp.HistoricalCodePattern = (string)drScanner["HistoricalCodePattern"];
                ss.HistoricalReaded += dp.ParseHistorical;
#pragma warning restore
                ss.Readed += dp.Parse;
                foreach (DataRow dr in drScanner.GetChildRows(config.Relations["Scanner_CodeTemplate"]))
                    dp.DataParserConfigs.Add(new DataParserConfig((string)dr["Name"], (string)dr["Pattern"]));

                DataWriter dw = new DataWriter(ss.Log);
                dp.Parsed += dw.Write;
                foreach (DataRow dr in drScanner.GetChildRows(config.Relations["Scanner_FileTemplate"]))
                    dw.DataWriterConfigs.Add(new DataWriterConfig((string)dr["Name"], (string)dr["SavePath"], (string)dr["Content"]));

                if (licence < 2)
                    Scanners.Add(ss);
                licence++;
            }
        }

        private void StartThreads()
        {
            foreach (SickScanner ss in Scanners)
            {
                Thread t = new Thread(new ThreadStart(ss.StartWork)) { Name = ss.Name };
                Threads.Add(t);
                t.Start();
            }
        }

        protected override void OnStop()
        {
            foreach (SickScanner ss in Scanners)
            {
                ss.StopWork();
            }

            foreach (Thread t in Threads)
                while (t.ThreadState != ThreadState.Stopped)
                    Thread.Sleep(1000);
            log.Add("Service stopped");
        }
    }
}
