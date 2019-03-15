using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SummitTouretteTask
{
    class DataManager
    {
        string storagePath;

        public DataManager()
        {
            this.storagePath = "E:/Summit/Data/";
        }

        public DataManager(string path)
        {
            this.storagePath = path;
        }

        public void WriteTdConfiguration(string filename, TdSensingSetting config)
        {

            int size = Marshal.SizeOf(config);
            byte[] rawBytes = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(config, ptr, false);
            Marshal.Copy(ptr, rawBytes, 0, size);
            Marshal.FreeHGlobal(ptr);

            string newConfigName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]");
            newConfigName = this.storagePath + "TdConfig/" + newConfigName + filename + ".dat";
            Debug.WriteLine(newConfigName);
            File.WriteAllBytes(newConfigName, rawBytes);

            string mostRecentConfig = this.storagePath + "TdConfig/" + "[new]" + filename + ".dat";
            Debug.WriteLine(mostRecentConfig);
            File.WriteAllBytes(mostRecentConfig, rawBytes);

            return;
        }

        public byte[] ReadTdConfiguration(string filename)
        {
            if (File.Exists(filename))
            {
                return File.ReadAllBytes(filename);
            }
            return null;
        }
    }
}
