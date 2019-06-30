using RDS.Logs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RDS.Sick
{
    public class SickScanner
    {
        /// <summary>
        /// IP or hostname
        /// </summary>
        public string Address { get; set; }

        private int port = 2112;
        /// <summary>
        /// TCP Port
        /// </summary>
        public int Port
        {
            get => port;
            set => port = (value > 65535) || (value < 0) ? throw new ArgumentOutOfRangeException("Port", "Port can be from 0 to 65535") : value;
        }

        private string name;
        /// <summary>
        /// Scanner name
        /// </summary>
        public string Name
        {
            get => name;
            set => name = string.IsNullOrEmpty(value) ? "Scanner" : value;
        }

        /// <summary>
        /// Returns last read RAW data
        /// </summary>
        public string DataRAW { get; private set; }

        /// <summary>
        /// Reconect to scanner in milisecounds
        /// </summary>
        public int ReconnectTime { get; set; } = 30000;

        /// <summary>
        /// Enable historical data read via http
        /// </summary>
        public bool EnableHistoricalRead { get; set; } = true; 

        /// <summary>
        /// When send mail about disconnection
        /// </summary>
        public uint MailOnConnectionTry { get; set; } = 2;

        /// <summary>
        /// List mail recipients
        /// </summary>
        public List<MailAddress> MailRecipients = new List<MailAddress>();

        /// <summary>
        /// Scanner log
        /// </summary>
        public Log Log;

        private TcpClient TcpClient = new TcpClient();
        private byte[] Buffer;
        private NetworkStream ReadStream;
        private StringBuilder StringBuilder = new StringBuilder();
        private SmtpClient SmtpClient = new SmtpClient();
        private DateTime DisconnectDateTime;
        private DateTime ConnectDateTime;
        private bool ContinueWork = true;

        public event EventHandler<SickScannerEventArgs> Readed;
        protected virtual void OnReaded(SickScannerEventArgs e) { Readed?.Invoke(this, e); }

        public event EventHandler<SickScannerEventArgs> HistoricalReaded;
        protected virtual void OnHistoricalReaded(SickScannerEventArgs e) { HistoricalReaded?.Invoke(this, e); }

        public SickScanner(string name, LogLevel logLevel)
        {
            Name = name;
            Log = new Log(logLevel, AppDomain.CurrentDomain.BaseDirectory, $"{Name}.txt");
        }

        /// <summary>
        /// Connect
        /// </summary>
        /// <returns>If connected returns true</returns>
        private bool Connect()
        {
            if (!TcpClient.Connected)
            {
                try
                {
                    TcpClient.Dispose();
                    TcpClient = new TcpClient();
                    TcpClient.Connect(Address, Port);
                    Buffer = new byte[TcpClient.ReceiveBufferSize];
                    TcpClient.ReceiveTimeout = 100;
                    TcpClient.SendTimeout = 100;
                    ReadStream = TcpClient.GetStream();

                    if (TcpClient.Connected)
                    {
                        Log.Add("Connected");

                        try
                        {
                            if (EnableHistoricalRead)
                                ReadHistoricalData();
                        }
                        catch (Exception ex)
                        {
                            Log.AddError("Unknown error", ex);
                        }

                        return TcpClient.Connected;
                    }
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    Log.AddError("Unable to connect", ex);
                    return false;
                }
            }
            else
                return true;
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        public void Disconnect()
        {
            TcpClient.Close();
            Log.Add("Disconnected");
        }

        /// <summary>
        /// Read data
        /// </summary>
        /// <returns>True if read was successful</returns>
        public bool Read()
        {
            if (TcpClient.Connected)
            {
                try
                {
                    ReadStream.Write(new byte[1] { 0 }, 0, 1);
                }
                catch
                {
                    Log.Add("Disconnection detected");
                }
            }

            int i = 1;

            while (!TcpClient.Connected)
            {

                if (i == 1)
                    DisconnectDateTime = DateTime.Now;
                Log.AddError(string.Format("Scanner is not connected. Connection attempt {0}", i));
                Connect();

                if (!TcpClient.Connected)
                {
                    if (i == MailOnConnectionTry)
                    {
                        SendMail(false);
                    }
                    Thread.Sleep(ReconnectTime);
                }
                else
                {
                    ConnectDateTime = DateTime.Now;
                    if (i > MailOnConnectionTry)
                    {
                        if (EnableHistoricalRead)
                            ReadHistoricalData();
                        SendMail(true);
                    }
                }

                if (!ContinueWork)
                    return false;
                i++;
            }

            if (ReadStream.CanRead & ReadStream.DataAvailable)
            {
                try
                {
                    StringBuilder.Clear();

                    int numberOfBytesRead = 0;

                    // Incoming message may be larger than the buffer size.
                    do
                    {
                        numberOfBytesRead = ReadStream.Read(Buffer, 0, Buffer.Length);

                        StringBuilder.AppendFormat("{0}", Encoding.ASCII.GetString(Buffer, 0, numberOfBytesRead));
                    }
                    while (ReadStream.DataAvailable);

                    DataRAW = StringBuilder.ToString();

                    Log.Add("Read");
                    Log.Add(string.Format("Data RAW: {0}", DataRAW), LogLevel.Debug);

                    Readed(this, new SickScannerEventArgs(DataRAW));

                    return true;
                }
                catch (Exception ex)
                {
                    Log.AddError("Unknow error", ex);
                }
            }
            return false;
        }

        /// <summary>
        /// Send mail
        /// </summary>
        /// <param name="Connected">true mail connected, false mail disconnected</param>
        private void SendMail(bool Connected)
        {
            if (MailRecipients.Count == 0)
                return;
            try
            {
                DataSet templates = new DataSet();
                templates.ReadXml(string.Format("{0}\\MailTemplates.xml", AppDomain.CurrentDomain.BaseDirectory));

                MailMessage mm = new MailMessage() { IsBodyHtml = true };

                foreach (MailAddress ma in MailRecipients)
                    mm.To.Add(ma);

                if (Connected)
                {
                    mm.Subject = (string)templates.Tables["Connected"].Rows[0]["Subject"];
                    mm.Body = (string)templates.Tables["Connected"].Rows[0]["Body"];
                }
                else
                {
                    mm.Subject = (string)templates.Tables["Disconnected"].Rows[0]["Subject"];
                    mm.Body = (string)templates.Tables["Disconnected"].Rows[0]["Body"];
                }

                mm.Subject = mm.Subject.Replace("@NAME", Name);
                mm.Body = mm.Body.Replace("@DISCONNECTDATETIME", DisconnectDateTime.ToString());
                mm.Body = mm.Body.Replace("@CONNECTDATETIME", ConnectDateTime.ToString());

                SmtpClient.Send(mm);
                if (Connected)
                    Log.Add("Connected mail sended", LogLevel.Debug);
                else
                    Log.Add("Disconnected mail sended", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                if (Connected)
                    Log.AddError("Connected mail error", ex);
                else
                    Log.AddError("Disconnected mail error", ex);
            }
        }

        public void StopWork()
        {
            ContinueWork = false;
        }

        public void StartWork()
        {
            while (ContinueWork)
            {
                Read();
                Thread.Sleep(1000);
            }
            Log.Add("Thread exited", LogLevel.Debug);
        }

        /// <summary>
        /// Read historical codes from web page. Max 20 scans
        /// </summary>
        public void ReadHistoricalData()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("http://{0}/Statistics/current/last20", Address));
                request.Method = "GET";
                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string result = reader.ReadToEnd();
                
                OnHistoricalReaded(new SickScannerEventArgs(result));
            }
            catch (Exception ex)
            {
                Log.AddError("Unable to read historical data", ex);
            }
        }
    }
}
