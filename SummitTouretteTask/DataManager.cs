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
    public struct TdSenseStruct
    {
        public ushort SequenceNumber;
        public double[] Channel1;
        public double[] Channel2;
        public double[] Channel3;
        public double[] Channel4;
    };

    public class DataManager
    {
        string storagePath;

        private readonly object threadLocker = new object();

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

        public void WriteBinary_ThreadSafe(string filename, byte[] data)
        {
            lock (threadLocker)
            {
                var stream = new FileStream(this.storagePath + filename, FileMode.Append);
                stream.Write(data, 0, data.Length);
                stream.Close();
                return;
            }
        }
    }
}
