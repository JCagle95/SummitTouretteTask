using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SummitTouretteTask
{
    public class Trigno
    {
        //The following are used for TCP/IP connections
        private TcpClient commandSocket;
        private TcpClient emgSocket;
        private TcpClient accSocket;
        private TcpClient imuEmgSocket;
        private TcpClient imuAuxSocket;

        private const int commandPort = 50040;  //server command port
        private const int emgPort = 50041;  //port for EMG data
        private const int accPort = 50042;  //port for acc data
        private const int ImuEmgDataPort = 50043;
        private const int ImuAuxDataPort = 50044;

        //Sensor List
        public enum SensorTypes { SensorTrigno, SensorTrignoImu, SensorTrignoMiniHead, SensorTrignoAnalog, NoSensor };
        public List<SensorTypes> connectedSensors = new List<SensorTypes>();
        private Dictionary<string, SensorTypes> sensorList = new Dictionary<string, SensorTypes>();
        public bool[] sensorStatus;

        //The following are streams and readers/writers for communication
        private NetworkStream commandStream;
        private NetworkStream emgStream;
        private NetworkStream accStream;
        private NetworkStream imuEmgStream;
        private NetworkStream imuAuxStream;
        private StreamReader commandReader;
        private StreamWriter commandWriter;

        //Server commands
        private const string COMMAND_QUIT = "QUIT";
        private const string COMMAND_GETTRIGGERS = "TRIGGER?";
        private const string COMMAND_SETSTARTTRIGGER = "TRIGGER START";
        private const string COMMAND_SETSTOPTRIGGER = "TRIGGER STOP";
        private const string COMMAND_START = "START";
        private const string COMMAND_STOP = "STOP";
        private const string COMMAND_SENSOR_TYPE = "TYPE?";
        
        private List<float>[] emgDataList = new List<float>[16];
        private List<float>[] accXDataList = new List<float>[16];
        private List<float>[] accYDataList = new List<float>[16];
        private List<float>[] accZDataList = new List<float>[16];

        private List<float>[] imuEmgDataList = new List<float>[16];
        private List<float>[] imuAxDataList = new List<float>[16];
        private List<float>[] imuAyDataList = new List<float>[16];
        private List<float>[] imuAzDataList = new List<float>[16];
        private List<float>[] imuGxDataList = new List<float>[16];
        private List<float>[] imuGyDataList = new List<float>[16];
        private List<float>[] imuGzDataList = new List<float>[16];
        private List<float>[] imuMxDataList = new List<float>[16];
        private List<float>[] imuMyDataList = new List<float>[16];
        private List<float>[] imuMzDataList = new List<float>[16];

        public StringBuilder emg_data_string = new StringBuilder();
        public StringBuilder accx_data_string = new StringBuilder();
        public StringBuilder accy_data_string = new StringBuilder();
        public StringBuilder accz_data_string = new StringBuilder();
        public StringBuilder im_emg_data_string = new StringBuilder();
        public StringBuilder im_accx_data_string = new StringBuilder();
        public StringBuilder im_accy_data_string = new StringBuilder();
        public StringBuilder im_accz_data_string = new StringBuilder();
        public StringBuilder im_gyrx_data_string = new StringBuilder();
        public StringBuilder im_gyry_data_string = new StringBuilder();
        public StringBuilder im_gyrz_data_string = new StringBuilder();
        public StringBuilder im_magx_data_string = new StringBuilder();
        public StringBuilder im_magy_data_string = new StringBuilder();
        public StringBuilder im_magz_data_string = new StringBuilder();
        
        //The following are storage for acquired data
        private float[] emgData = new float[16];
        private float[] imuEmgData = new float[16];
        private float[] accXData = new float[16];
        private float[] accYData = new float[16];
        private float[] accZData = new float[16];
        private float[] imuAccXData = new float[16];
        private float[] imuAccYData = new float[16];
        private float[] imuAccZData = new float[16];
        private float[] gyroXData = new float[16];
        private float[] gyroYData = new float[16];
        private float[] gyroZData = new float[16];
        private float[] magXData = new float[16];
        private float[] magYData = new float[16];
        private float[] magZData = new float[16];

        //Threads for acquiring emg and acc data
        private Thread emgThread;
        private Thread accThread;
        private Thread imuEmgThread;
        private Thread imuAuxThread;
        private readonly object EMGObject = new object();
        private readonly object AuxObject = new object();
        private readonly object imuEMGObject = new object();
        private readonly object imuAuxObject = new object();
        private System.Timers.Timer AcquisitionTimer;

        // Overall Variables
        public bool status;
        public bool connected;

        // Data Writer
        public DataManager dataManager;
        public string storedFileName = "";
        public Stopwatch stopWatch;
        public int bytesPerWrite = 3000;
        public int EMGbytesPerWrite = 40000;

        public Trigno()
        {
            status = false;
            connected = false;

            sensorList.Add("A", SensorTypes.SensorTrigno);
            sensorList.Add("D", SensorTypes.SensorTrigno);
            sensorList.Add("L", SensorTypes.SensorTrignoImu);
            sensorList.Add("J", SensorTypes.SensorTrignoMiniHead);
            sensorList.Add("K", SensorTypes.SensorTrignoAnalog);

            sensorStatus = new bool[16];

            stopWatch = new Stopwatch();
        }

        // Send command to base station
        private string SendCommand(string command)
        {
            string response = "";

            //Check if connected
            if (connected)
            {
                //Send the command
                commandWriter.WriteLine(command);
                commandWriter.WriteLine();  //terminate command
                commandWriter.Flush();  //make sure command is sent immediately

                //Read the response line and display    
                response = commandReader.ReadLine();
                commandReader.ReadLine();   //get extra line terminator
            }
            return response;    //return the response we got
        }

        // Setup the network sockets
        public void SetupServer()
        {
            try
            {
                //Establish TCP/IP connection to server using URL entered
                commandSocket = new TcpClient("localhost", commandPort);

                //Set up communication streams
                commandStream = commandSocket.GetStream();
                commandReader = new StreamReader(commandStream, Encoding.ASCII);
                commandWriter = new StreamWriter(commandStream, Encoding.ASCII);

                //Get initial response from server and display
                MessageBox.Show("Delsys Connected.\n" + commandReader.ReadLine());
                commandReader.ReadLine();   //get extra line terminator
                connected = true;
            }
            catch (Exception connectException)
            {
                //connection failed, display error message
                MessageBox.Show("Could not connect.\n" + connectException.Message);
            }
        }

        public void DisconnectServer()
        {
            if (connected)
            {
                //send QUIT command
                SendCommand(COMMAND_QUIT);
                connected = false;  //no longer connected

                //Close all streams and connections
                commandReader.Close();
                commandWriter.Close();
                commandStream.Close();
                commandSocket.Close();
                emgStream.Close();
                emgSocket.Close();
                accStream.Close();
                accSocket.Close();
                imuEmgStream.Close();
                imuEmgSocket.Close();
                imuAuxStream.Close();
                imuAuxSocket.Close();

            }
        }

        public void UpdateSensors()
        {
            if (connected)
            {
                //build a list of connected sensor types
                connectedSensors = new List<SensorTypes>();
                for (int i = 1; i <= 16; i++)
                {
                    string query = "SENSOR " + i + " " + COMMAND_SENSOR_TYPE;
                    string response = SendCommand(query);
                    connectedSensors.Add(response.Contains("INVALID") ? SensorTypes.NoSensor : sensorList[response]);
                }

                SendCommand("UPSAMPLE OFF");
            }
        }

        public void StartAcquisition()
        {
            if (!connected)
            {
                Debug.WriteLine("Trigno has not been initialized.");
                return;
            }

            for (int i = 0; i < 16; i++)
            {
                imuEmgDataList[i] = new List<float>();
                imuAxDataList[i] = new List<float>();
                imuAyDataList[i] = new List<float>();
                imuAzDataList[i] = new List<float>();
                imuGxDataList[i] = new List<float>();
                imuGyDataList[i] = new List<float>();
                imuGzDataList[i] = new List<float>();
                imuMxDataList[i] = new List<float>();
                imuMyDataList[i] = new List<float>();
                imuMzDataList[i] = new List<float>();
                emgDataList[i] = new List<float>();
                accXDataList[i] = new List<float>();
                accYDataList[i] = new List<float>();
                accZDataList[i] = new List<float>();
            }
            
            //Establish data connections and creat streams
            
            /*
            emgSocket = new TcpClient("localhost", emgPort);
            emgStream = emgSocket.GetStream();
            emgThread = new Thread(EMGAcquisition);
            emgThread.IsBackground = true;

            accSocket = new TcpClient("localhost", accPort);
            accStream = accSocket.GetStream();
            accThread = new Thread(AccAcquisition);
            accThread.IsBackground = true;
            */

            imuEmgSocket = new TcpClient("localhost", ImuEmgDataPort);
            imuEmgStream = imuEmgSocket.GetStream();
            imuEmgThread = new Thread(ImuEmgAcquisition);
            imuEmgThread.IsBackground = true;

            imuAuxSocket = new TcpClient("localhost", ImuAuxDataPort);
            imuAuxStream = imuAuxSocket.GetStream();
            imuAuxThread = new Thread(ImuAuxAcquisition);
            imuAuxThread.IsBackground = true;
            
            status = true;
            if (emgThread != null) emgThread.Start();
            if (accThread != null) accThread.Start();
            if (imuEmgThread != null) imuEmgThread.Start();
            if (imuAuxThread != null) imuAuxThread.Start();

            string a = SendCommand("RATE?");
            Debug.WriteLine("EMG Data Rate: " + a.ToString());

            //Send start command to server to stream data
            string response = SendCommand(COMMAND_START);

            if (response.StartsWith("OK"))
            {
                //Start the UI update timer
                dataManager = new DataManager();
                if (storedFileName == "") storedFileName = "test.mdat";
                stopWatch.Start();
            }
            else
            {
                status = false;
            }
        }

        public void StartDataWriter()
        {
            AcquisitionTimer = new System.Timers.Timer();
            AcquisitionTimer.Interval = 5000;
            AcquisitionTimer.Elapsed += DataWriter_Tick;
            AcquisitionTimer.Start();
        }

        public void StopAcquisition()
        {
            status = false;    //no longer running
            if (emgThread != null) emgThread.Join();
            if (accThread != null) accThread.Join();
            if (imuEmgThread != null) imuEmgThread.Join();
            if (imuAuxThread != null) imuAuxThread.Join();

            string response = SendCommand(COMMAND_STOP);
            AcquisitionTimer.Stop();

            for (int i = 0; i < 16; i++)
            {
                if (emgDataList[i].Count > 0)
                {
                    if (sensorStatus[i])
                    {
                        HeaderWriter("EMG" + i.ToString());
                        Debug.WriteLine("EMG " + i.ToString() + " has " + emgDataList[i].Count().ToString() + " number");
                        float[] rawData = emgDataList[i].ToArray();
                        byte[] rawBytes = new byte[rawData.Length * 4];
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                    }
                    emgDataList[i].RemoveRange(0, emgDataList[i].Count());
                }
            }

            for (int i = 0; i < 16; i++)
            {
                if (accXDataList[i].Count > 0)
                {
                    if (sensorStatus[i])
                    {
                        HeaderWriter("AUX" + i.ToString());
                        Debug.WriteLine("AUX " + i.ToString() + " has " + accXDataList[i].Count().ToString() + " number");
                        float[] rawData = accXDataList[i].ToArray();
                        byte[] rawBytes = new byte[rawData.Length * 4];
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);

                        rawData = accYDataList[i].ToArray();
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);

                        rawData = accZDataList[i].ToArray();
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                    }
                    accXDataList[i].RemoveRange(0, accXDataList[i].Count());
                    accYDataList[i].RemoveRange(0, accYDataList[i].Count());
                    accZDataList[i].RemoveRange(0, accZDataList[i].Count());

                }
            }

            for (int i = 0; i < 16; i++)
            {
                if (imuEmgDataList[i].Count > 0)
                {
                    if (sensorStatus[i])
                    {
                        HeaderWriter("imuEMG" + i.ToString());
                        Debug.WriteLine("imuEMG " + i.ToString() + " has " + imuEmgDataList[i].Count().ToString() + " number");
                        float[] rawData = imuEmgDataList[i].ToArray();
                        byte[] rawBytes = new byte[rawData.Length * 4];
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                    }
                    imuEmgDataList[i].RemoveRange(0, imuEmgDataList[i].Count());
                }
            }

            for (int i = 0; i < 16; i++)
            {
                if (imuAxDataList[i].Count > 0)
                {
                    if (sensorStatus[i])
                    {
                        HeaderWriter("imuAUX" + i.ToString());
                        Debug.WriteLine("imuAUX " + i.ToString() + " has " + imuAxDataList[i].Count().ToString() + " number");
                        float[] rawData = imuAxDataList[i].ToArray();
                        byte[] rawBytes = new byte[rawData.Length * 4];
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                        rawData = imuAyDataList[i].ToArray();
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                        rawData = imuAzDataList[i].ToArray();
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);

                        rawData = imuGxDataList[i].ToArray();
                        rawBytes = new byte[rawData.Length * 4];
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                        rawData = imuGyDataList[i].ToArray();
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                        rawData = imuGzDataList[i].ToArray();
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);

                        rawData = imuMxDataList[i].ToArray();
                        rawBytes = new byte[rawData.Length * 4];
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                        rawData = imuMyDataList[i].ToArray();
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                        rawData = imuMzDataList[i].ToArray();
                        Buffer.BlockCopy(rawData, 0, rawBytes, 0, rawData.Length * 4);
                        dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                    }

                    imuAxDataList[i].RemoveRange(0, imuAxDataList[i].Count());
                    imuAyDataList[i].RemoveRange(0, imuAyDataList[i].Count());
                    imuAzDataList[i].RemoveRange(0, imuAzDataList[i].Count());

                    imuGxDataList[i].RemoveRange(0, imuGxDataList[i].Count());
                    imuGyDataList[i].RemoveRange(0, imuGyDataList[i].Count());
                    imuGzDataList[i].RemoveRange(0, imuGzDataList[i].Count());

                    imuMxDataList[i].RemoveRange(0, imuMxDataList[i].Count());
                    imuMyDataList[i].RemoveRange(0, imuMyDataList[i].Count());
                    imuMzDataList[i].RemoveRange(0, imuMzDataList[i].Count());
                }
            }
            stopWatch.Stop();
        }

        private void EMGAcquisition()
        {
            emgStream.ReadTimeout = 1000;    //set timeout

            //Create a binary reader to read the data
            BinaryReader reader = new BinaryReader(emgStream);

            while (status)
            {
                lock (EMGObject)
                {
                    try
                    {
                        for (int sn = 0; sn < 16; sn++)
                        {
                            emgDataList[sn].Add(reader.ReadSingle());
                        }
                    }
                    catch (IOException e)
                    {
                    }
                }
            }
            reader.Close(); //close the reader. This also disconnects
        }

        private void AccAcquisition()
        {
            accStream.ReadTimeout = 1000;    //set timeout

            //Create a binary reader to read the data
            BinaryReader reader = new BinaryReader(accStream);

            while (status)
            {
                lock (AuxObject)
                {
                    try
                    {
                        //Demultiplex the data and save for UI display
                        for (int sn = 0; sn < 16; sn++)
                        {
                            accXDataList[sn].Add(reader.ReadSingle());
                            accYDataList[sn].Add(reader.ReadSingle());
                            accZDataList[sn].Add(reader.ReadSingle());
                        }
                    }
                    catch (IOException e)
                    {
                    }
                }
            }
            reader.Close(); //close the reader. This also disconnects
        }

        private void ImuEmgAcquisition()
        {
            imuEmgStream.ReadTimeout = 1000;    //set timeout

            BinaryReader reader = new BinaryReader(imuEmgStream);
            while (status)
            {
                lock(imuEMGObject)
                {
                    try
                    {
                        for (int sn = 0; sn < 16; sn++)
                        {
                            imuEmgDataList[sn].Add(reader.ReadSingle());
                        }
                    }
                    catch (IOException e)
                    {
                    }
                }
            }
            reader.Close(); //close the reader. This also disconnects
        }

        private void ImuAuxAcquisition()
        {
            imuAuxStream.ReadTimeout = 1000;    //set timeout

            //Create a binary reader to read the data
            BinaryReader reader = new BinaryReader(imuAuxStream);

            while (status)
            {
                lock (imuAuxObject)
                {
                    try
                    {
                        for (int sn = 0; sn < 16; sn++)
                        {
                            imuAxDataList[sn].Add(reader.ReadSingle());
                            imuAyDataList[sn].Add(reader.ReadSingle());
                            imuAzDataList[sn].Add(reader.ReadSingle());
                            imuGxDataList[sn].Add(reader.ReadSingle());
                            imuGyDataList[sn].Add(reader.ReadSingle());
                            imuGzDataList[sn].Add(reader.ReadSingle());
                            imuMxDataList[sn].Add(reader.ReadSingle());
                            imuMyDataList[sn].Add(reader.ReadSingle());
                            imuMzDataList[sn].Add(reader.ReadSingle());
                        }
                    }
                    catch (IOException e)
                    {
                    }
                }
            }
            reader.Close(); //close the reader. This also disconnects 
        }

        private void HeaderWriter(string header)
        {
            byte[] rawBytes = new byte[32];

            // Populate the Headers - Magic Number BML + Global Sequence Byte
            rawBytes[0] = 66;
            rawBytes[1] = 77;
            rawBytes[2] = 76;
            rawBytes[3] = 0;

            // Populate the Headers #2 - Type of Data Det  
            byte[] dataType = Encoding.ASCII.GetBytes("Trg ");
            Buffer.BlockCopy(dataType, 0, rawBytes, 4, dataType.Length);

            // Write Trigger Values
            byte[] triggerBytes = Encoding.ASCII.GetBytes(header);
            Buffer.BlockCopy(triggerBytes, 0, rawBytes, 8, triggerBytes.Length);

            // Populate the Headers #4 - PC 64-bit Ticks
            byte[] timeBytes = BitConverter.GetBytes(stopWatch.ElapsedMilliseconds);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 20, timeBytes.Length);

            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
        }

        private void DataWriter_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("Tick");
            HeaderWriter("Tick");
            lock (EMGObject)
            {
                byte[] rawBytes = new byte[EMGbytesPerWrite * 4];
                float[] rawData = new float[EMGbytesPerWrite];

                for (int i = 0; i < 16; i++)
                {
                    if (sensorStatus[i])
                    {
                        if (emgDataList[i].Count > EMGbytesPerWrite)
                        {
                            HeaderWriter("EMG" + i.ToString());
                            rawData = emgDataList[i].Take(EMGbytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, EMGbytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            emgDataList[i].RemoveRange(0, EMGbytesPerWrite);
                        }
                    }
                    else
                    {
                        emgDataList[i].RemoveRange(0, emgDataList[i].Count());
                    }
                }
            }

            lock (AuxObject)
            {
                byte[] rawBytes = new byte[bytesPerWrite * 4];
                float[] rawData = new float[bytesPerWrite];

                for (int i = 0; i < 16; i++)
                {
                    if (sensorStatus[i])
                    {
                        if (accXDataList[i].Count > bytesPerWrite)
                        {
                            HeaderWriter("AUX" + i.ToString());
                            rawData = accXDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            rawData = accYDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            rawData = accZDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            accXDataList[i].RemoveRange(0, bytesPerWrite);
                            accYDataList[i].RemoveRange(0, bytesPerWrite);
                            accZDataList[i].RemoveRange(0, bytesPerWrite);

                        }
                    }
                    else
                    {
                        accXDataList[i].RemoveRange(0, accXDataList[i].Count());
                        accYDataList[i].RemoveRange(0, accYDataList[i].Count());
                        accZDataList[i].RemoveRange(0, accZDataList[i].Count());
                    }
                }
            }

            lock (imuEMGObject)
            {
                byte[] rawBytes = new byte[EMGbytesPerWrite * 4];
                float[] rawData = new float[EMGbytesPerWrite];

                for (int i = 0; i < 16; i++)
                {
                    if (sensorStatus[i])
                    {
                        if (imuEmgDataList[i].Count > EMGbytesPerWrite)
                        {
                            HeaderWriter("imuEMG" + i.ToString());
                            rawData = imuEmgDataList[i].Take(EMGbytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, EMGbytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            imuEmgDataList[i].RemoveRange(0, EMGbytesPerWrite);
                        }
                    }
                    else
                    {
                        imuEmgDataList[i].RemoveRange(0, imuEmgDataList[i].Count());
                    }
                }
            }

            lock (imuAuxObject)
            {
                byte[] rawBytes = new byte[bytesPerWrite*4];
                float[] rawData = new float[bytesPerWrite];

                for (int i = 0; i < 16; i++)
                {
                    if (sensorStatus[i])
                    {
                        if (imuAxDataList[i].Count > bytesPerWrite)
                        {
                            HeaderWriter("imuAUX" + i.ToString());
                            rawData = imuAxDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            rawData = imuAyDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            rawData = imuAzDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            imuAxDataList[i].RemoveRange(0, bytesPerWrite);
                            imuAyDataList[i].RemoveRange(0, bytesPerWrite);
                            imuAzDataList[i].RemoveRange(0, bytesPerWrite);

                            rawData = imuGxDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            rawData = imuGyDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            rawData = imuGzDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            imuGxDataList[i].RemoveRange(0, bytesPerWrite);
                            imuGyDataList[i].RemoveRange(0, bytesPerWrite);
                            imuGzDataList[i].RemoveRange(0, bytesPerWrite);

                            rawData = imuMxDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            rawData = imuMyDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            rawData = imuMzDataList[i].Take(bytesPerWrite).ToArray();
                            Buffer.BlockCopy(rawData, 0, rawBytes, 0, bytesPerWrite * 4);
                            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
                            imuMxDataList[i].RemoveRange(0, bytesPerWrite);
                            imuMyDataList[i].RemoveRange(0, bytesPerWrite);
                            imuMzDataList[i].RemoveRange(0, bytesPerWrite);
                        }
                    }
                    else
                    {
                        imuAxDataList[i].RemoveRange(0, imuAxDataList[i].Count());
                        imuAyDataList[i].RemoveRange(0, imuAyDataList[i].Count());
                        imuAzDataList[i].RemoveRange(0, imuAzDataList[i].Count());

                        imuGxDataList[i].RemoveRange(0, imuGxDataList[i].Count());
                        imuGyDataList[i].RemoveRange(0, imuGyDataList[i].Count());
                        imuGzDataList[i].RemoveRange(0, imuGzDataList[i].Count());

                        imuMxDataList[i].RemoveRange(0, imuMxDataList[i].Count());
                        imuMyDataList[i].RemoveRange(0, imuMyDataList[i].Count());
                        imuMzDataList[i].RemoveRange(0, imuMzDataList[i].Count());
                    }
                }
            }
        }
    }
}
