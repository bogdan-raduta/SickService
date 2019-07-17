using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SickService
{
    public struct DataWriterConfig
    {
        public readonly string FileNameTemplate;
        public readonly string SavePath;
        public readonly string FileContentTemplate;
        
        public DataWriterConfig(string fileNameTemplate, string savePath, string fileContentTemplate)
        {
            FileNameTemplate = fileNameTemplate;
            SavePath = savePath;
            FileContentTemplate = fileContentTemplate;
            PrepareDirectory();
        }

        private void PrepareDirectory()
        {
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);
        }
    }
}
