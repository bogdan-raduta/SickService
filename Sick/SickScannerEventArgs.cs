using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.Sick
{
    public class SickScannerEventArgs : EventArgs
    {
        public readonly string DataRAW;
        public SickScannerEventArgs(string dataRAW) { DataRAW = dataRAW; }
    }
}
