using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

using Medtronic.SummitAPI.Classes;
using Medtronic.TelemetryM;
using Medtronic.NeuroStim.Olympus.DataTypes.PowerManagement;
using Medtronic.NeuroStim.Olympus.DataTypes.DeviceManagement;
using Medtronic.NeuroStim.Olympus.DataTypes.Sensing;
using System.IO;
using Medtronic.NeuroStim.Olympus.DataTypes.Therapy;
using System.IO.Ports;
using Medtronic.NeuroStim.Olympus.DataTypes.Measurement;

namespace SummitTouretteTask
{

    public struct MontageSetting
    {
        public bool[] leadSelection;
        public decimal frameDuration;
        public decimal montageDuration;
        public TdSampleRates samplingRate;
    };

    public struct TdSensingSetting
    {
        public TdMuxInputs[,] channelMux;
        public TdSampleRates[] samplingRate;
        public TdEvokedResponseEnable[] evokedResponse;
        public TdLpfStage1[] stage1LPF;
        public TdLpfStage2[] stage2LPF;
        public TdHpfs[] stage1HPF;
        public bool[] enableStatus;
    };

    public struct FFTSensingSetting
    {
        public FftSizes fftSizes;
        public ushort interval;
        public FftWindowAutoLoads windowLoad;
        public bool enableWindow;
        public FftWeightMultiplies shiftBits;
        public SenseTimeDomainChannel fftChannel;

        public List<PowerChannel> powerbandSetting;
        public BandEnables powerbandEnables;

        public double binSize;
    };

    public struct MISCSensingSetting
    {
        public BridgingConfig bridging;
        public StreamingFrameRate streamRate;
        public LoopRecordingTriggers lrTrigger;
        public ushort lrBuffer;

        public AccelSampleRate accSampleRate;
    };

    public struct STIMSetting
    {
        public double[] frequency;
        public int[] pulsewidth;
        public double[] amplitude;
        public int[] plusContacts;
        public int[] minusContacts;
    }
    
    public partial class Mainpage : Form
    {

        // Montage Settings
        MontageSetting montageSetting;

        // Summit RC+S RDK
        string ORCA_ProjectString;
        SummitManager summitManager;
        SummitSystem summitSystem;

        // Real-time Data Streaming Settings
        TdSensingSetting sensingSetting;
        FFTSensingSetting fftSetting;
        MISCSensingSetting miscSetting;

        // Stimulation Setting
        STIMSetting stimSetting;
        TherapyGroup[] therapyGroups;
        AmplitudeLimits[] amplitudeLimits;
        double[] impedanceResults;

        // Configuration Check
        bool[] configurationCheck;
        bool[] streamingOptions;

        // Task Monitor Check
        Screen[] screens;
        Screen workingMonitor;

        DataManager dataManager;

        SerialPort serialPort;

        // Trigno Sensors
        Trigno trignoServer;

        // To Handle ORCA Window
        [DllImport("User32")]
        private static extern int ShowWindow(int windowHandler, int showCMD);
        private Thread ORCA_Thread = null;
        private int ORCA_Task;
        private bool ORCA_TaskComplete;
        private bool DisableORCA;

        // To Handle Configuration Files
        public byte[] GetByteArrayFromStruct(TdSensingSetting str)
        {
            byte[] rawBytes = new byte[44];

            // Populate the Headers - Magic Number BML + Global Sequence Byte
            rawBytes[0] = 66;
            rawBytes[1] = 77;
            rawBytes[2] = 76;

            // Starting on 5th byte - Configuration Type - 8 bytes total
            byte[] dataType = Encoding.ASCII.GetBytes("Td Conf ");
            Buffer.BlockCopy(dataType, 0, rawBytes, 4, dataType.Length);

            // The data format is all in bytes. All data are converted to single byte in set of 4, representing each of the 4 channels
            for (int i = 0; i < 4; i++)
            {
                rawBytes[12 + i] = (byte)str.channelMux[i, 0];
                rawBytes[16 + i] = (byte)str.channelMux[i, 1];
                rawBytes[20 + i] = (byte)str.samplingRate[i];
                rawBytes[24 + i] = (byte)str.evokedResponse[i];
                rawBytes[28 + i] = (byte)str.stage1LPF[i];
                rawBytes[32 + i] = (byte)str.stage2LPF[i];
                rawBytes[36 + i] = (byte)str.stage1HPF[i];
                rawBytes[40 + i] = Convert.ToByte(str.enableStatus[i]);
            }

            return rawBytes;
        }

        public TdSensingSetting GetTimeDomainSetting(byte[] array)
        {
            TdSensingSetting str = new TdSensingSetting();
            str.samplingRate = new TdSampleRates[4];
            str.channelMux = new TdMuxInputs[4, 2];
            str.evokedResponse = new TdEvokedResponseEnable[4];
            str.stage1LPF = new TdLpfStage1[4];
            str.stage2LPF = new TdLpfStage2[4];
            str.stage1HPF = new TdHpfs[4];
            str.enableStatus = new bool[4] { true, true, true, true };
            
            for (int i = 0; i < 4; i++)
            {
                str.channelMux[i,0] = (TdMuxInputs) array[12 + i];
                str.channelMux[i,1] = (TdMuxInputs)array[16 + i];
                str.samplingRate[i] = (TdSampleRates)array[20 + i];
                str.evokedResponse[i] = (TdEvokedResponseEnable) array[24 + i];
                str.stage1LPF[i] = (TdLpfStage1) array[28 + i];
                str.stage2LPF[i] = (TdLpfStage2)array[32 + i];
                str.stage1HPF[i] = (TdHpfs)array[36 + i];
                str.enableStatus[i] = Convert.ToBoolean(array[40 + i]); 
            }

            return str;
        }

        public byte[] GetByteArrayFromStruct(FFTSensingSetting str)
        {
            byte[] rawBytes = new byte[60];

            // Populate the Headers - Magic Number BML + Global Sequence Byte
            rawBytes[0] = 66;
            rawBytes[1] = 77;
            rawBytes[2] = 76;

            // Starting on 5th byte - Configuration Type - 8 bytes total
            byte[] dataType = Encoding.ASCII.GetBytes("FFTConf ");
            Buffer.BlockCopy(dataType, 0, rawBytes, 4, dataType.Length);

            // The data format is all in bytes. dumpping them in in order
            rawBytes[12] = (byte)str.fftSizes;
            rawBytes[13] = (byte)str.windowLoad;
            rawBytes[14] = (byte)str.shiftBits;
            rawBytes[15] = Convert.ToByte(str.enableWindow);

            // The bin size is actually calculable from other configurations. But it is kept here just to be safe
            byte[] detailedBinSize = BitConverter.GetBytes(str.binSize);
            Buffer.BlockCopy(detailedBinSize, 0, rawBytes, 16, detailedBinSize.Length);

            // This is the last information.
            byte[] intervalBytes = BitConverter.GetBytes(str.interval);
            Buffer.BlockCopy(intervalBytes, 0, rawBytes, 24, intervalBytes.Length);
            rawBytes[26] = (byte)str.fftChannel;
            rawBytes[27] = (byte)str.powerbandEnables;

            // This is the Power Band settings
            for (int i = 0; i < 4; i++)
            {
                byte[] tempBytes;
                tempBytes = BitConverter.GetBytes(str.powerbandSetting[i].Band0Start);
                Buffer.BlockCopy(tempBytes, 0, rawBytes, 28 + i*8, tempBytes.Length);
                tempBytes = BitConverter.GetBytes(str.powerbandSetting[i].Band0Stop);
                Buffer.BlockCopy(tempBytes, 0, rawBytes, 30 + i*8, tempBytes.Length);
                tempBytes = BitConverter.GetBytes(str.powerbandSetting[i].Band1Start);
                Buffer.BlockCopy(tempBytes, 0, rawBytes, 32 + i*8, tempBytes.Length);
                tempBytes = BitConverter.GetBytes(str.powerbandSetting[i].Band1Stop);
                Buffer.BlockCopy(tempBytes, 0, rawBytes, 34 + i*8, tempBytes.Length);
            }
            
            return rawBytes;
        }

        public FFTSensingSetting GetFFTSetting(byte[] array)
        {
            FFTSensingSetting str = new FFTSensingSetting();
            str.fftSizes = (FftSizes)array[12];
            str.windowLoad = (FftWindowAutoLoads)array[13];
            str.shiftBits = (FftWeightMultiplies)array[14];
            str.enableWindow = Convert.ToBoolean(array[15]);
            str.interval = BitConverter.ToUInt16(array, 24);
            str.binSize = BitConverter.ToDouble(array, 16);

            str.powerbandSetting = new List<PowerChannel>(4);
            for (int i = 0; i < 4; i++)
            {
                str.powerbandSetting.Add(new PowerChannel(
                    BitConverter.ToUInt16(array, 28 + i * 8),
                    BitConverter.ToUInt16(array, 30 + i * 8),
                    BitConverter.ToUInt16(array, 32 + i * 8),
                    BitConverter.ToUInt16(array, 34 + i * 8)
                    ));
            }
            str.fftChannel = (SenseTimeDomainChannel)array[26];
            str.powerbandEnables = (BandEnables)array[27];

            return str;
        }

        public byte[] GetByteArrayFromStruct(MISCSensingSetting str)
        {
            byte[] rawBytes = new byte[20];

            // Populate the Headers - Magic Number BML + Global Sequence Byte
            rawBytes[0] = 66;
            rawBytes[1] = 77;
            rawBytes[2] = 76;

            // Starting on 5th byte - Configuration Type - 8 bytes total
            byte[] dataType = Encoding.ASCII.GetBytes("MiscConf");
            Buffer.BlockCopy(dataType, 0, rawBytes, 4, dataType.Length);

            // The data format is all in bytes. dumpping them in in order
            rawBytes[12] = (byte)str.bridging;
            rawBytes[13] = (byte)str.streamRate;
            rawBytes[14] = (byte)str.lrTrigger;
            rawBytes[15] = (byte)str.accSampleRate;

            byte[] loopRecorderBufferBytes = BitConverter.GetBytes(str.lrBuffer);
            Buffer.BlockCopy(loopRecorderBufferBytes, 0, rawBytes, 16, loopRecorderBufferBytes.Length);

            return rawBytes;
        }

        public MISCSensingSetting GetMISCSetting(byte[] array)
        {
            MISCSensingSetting str = new MISCSensingSetting();

            str.bridging = (BridgingConfig)array[12];
            str.streamRate = (StreamingFrameRate)array[13];
            str.lrTrigger = (LoopRecordingTriggers)array[14];
            str.accSampleRate = (AccelSampleRate)array[15];
            str.lrBuffer = BitConverter.ToUInt16(array, 16);

            return str;
        }

        #region MainWindow Initialization

        public Mainpage()
        {
            InitializeComponent();

            // Initialize Required Variables
            this.dataManager = new DataManager();
            this.configurationCheck = new bool[4] { false, false, false, false };
            this.streamingOptions = new bool[8] { false, false, false, false, false, false, false, false };

            // Initialize Sensing Setting
            this.sensingSetting.samplingRate = new TdSampleRates[4];
            this.sensingSetting.channelMux = new TdMuxInputs[4, 2];
            this.sensingSetting.evokedResponse = new TdEvokedResponseEnable[4];
            this.sensingSetting.stage1LPF = new TdLpfStage1[4];
            this.sensingSetting.stage2LPF = new TdLpfStage2[4];
            this.sensingSetting.stage1HPF = new TdHpfs[4];
            this.sensingSetting.enableStatus = new bool[4] { true, true, true, true };

            // Initialize FFT Settings
            this.fftSetting.fftSizes = FftSizes.Size0064;
            this.fftSetting.interval = 50;
            this.fftSetting.windowLoad = FftWindowAutoLoads.Hann100;
            this.fftSetting.enableWindow = true;
            this.fftSetting.shiftBits = FftWeightMultiplies.Shift7;
            this.fftSetting.binSize = -1;
            this.fftSetting.powerbandSetting = new List<PowerChannel>(4);
            this.fftSetting.powerbandEnables = 0;

            // Initialize Theray Groups
            stimSetting.amplitude = new double[4];
            stimSetting.frequency = new double[4];
            stimSetting.pulsewidth = new int[4];
            stimSetting.plusContacts = new int[4];
            stimSetting.minusContacts = new int[4];

            therapyGroups = new TherapyGroup[4];
            amplitudeLimits = new AmplitudeLimits[4];
            impedanceResults = new double[40];

            // Initialize Accelerometer
            this.miscSetting.accSampleRate = AccelSampleRate.Disabled;

            // Initialize Montage Task
            this.montageSetting.leadSelection = new bool[4] { true , true, true, true };

            // Initialize Task Monitors
            screens = Screen.AllScreens;
            foreach (Screen screen in screens)
            {
                this.Task_MonitorPicker.Items.Add(screen.DeviceName);
            }
            workingMonitor = screens[0];
            this.Task_MonitorPicker.SelectedIndex = 0;
            this.MonitorSizeLabel.Text = screens[0].WorkingArea.Width.ToString() + " x " + screens[0].WorkingArea.Height.ToString();

            // Setup ORCA Repository
            this.ORCA_ProjectName.Text = "projectSummitTourette";
            this.ORCA_TaskComplete = false;
            this.ORCA_Task = 0;
            this.DisableORCA = true;

            // Setup Delsys
            trignoServer = new Trigno();
        }

        /// <summary>
        /// Closing the Main Program. Dispose any Summit connection while the form is closing.
        /// </summary>
        private void Mainpage_FormClosing(object sender, FormClosingEventArgs e)
        {

            if (summitManager != null)
            {
                this.ORCA_TaskComplete = false;
                this.ORCA_Task = 1;
                this.ORCA_Thread = new Thread(new ThreadStart(this.ORCA_Handler));
                this.ORCA_Thread.Start();

                summitManager.Dispose();
                
                this.ORCA_TaskComplete = true;
                this.ORCA_Task = 0;
                this.ORCA_Thread.Join();
            }
        }

        #endregion

        #region Summit Initialization

        /// <summary>
        /// Establish Connection with Summit RC+S through USB or Bluetooth. 
        /// </summary>
        private void Summit_Connect_Click(object sender, EventArgs e)
        {
            this.Summit_Connect.Enabled = false;

            this.summitManager = new SummitManager(this.ORCA_ProjectString);
            Debug.WriteLine("Manager Created");

            // Retrive current USB information and past Telemetry for potential bluetooth connection
            this.summitManager.GetUsbTelemetry();
            Debug.WriteLine("Get USB Telemetry");
            List<InstrumentInfo> knownTelemetry = this.summitManager.GetKnownTelemetry();
            Debug.WriteLine("Get Known Telemetry");
            
            // Return Error Message if no telemetry item found.
            if (knownTelemetry.Count == 0)
            {
                MessageBox.Show("No bonded CTMs found, please plug a CTM in via USB...");
                this.Summit_Connect.Enabled = true;
                this.summitManager.Dispose();
                return;
            }
            Debug.WriteLine("Found CTMs");

            // Looping through all the known CTMs and try to connect to the first available device.
            this.summitSystem = null;
            for (int i = 0; i < knownTelemetry.Count; i++)
            {
                // Perform the connection and see if it is successful or not. If yes, break the loop.
                Debug.WriteLine(string.Format("Testing CTMs {0} of {1}", i+1, knownTelemetry.Count));
                ManagerConnectStatus connectReturn = this.summitManager.CreateSummit(out this.summitSystem, knownTelemetry[i]);
                if (connectReturn == ManagerConnectStatus.Success) break;
                Debug.WriteLine(string.Format("CTMs {0} of {1} Failed", i + 1, knownTelemetry.Count));
            }

            // Check to see if any successfull connection occurred or not.
            if (this.summitSystem == null)
            {
                MessageBox.Show("Cannot connect to known CTMs, please plug a CTM in via USB...");
                this.Summit_Connect.Enabled = true;
                this.summitManager.Dispose();
                return;
            }
            Debug.WriteLine("CTM Connected");

            // Enable Next Step: Discover INS
            this.Summit_DiscoverRCS.Enabled = true;
        }

        private void Summit_DiscoverRCS_Click(object sender, EventArgs e)
        {
            List<DiscoveredDevice> discoveredDevices;
            do
            {
                Debug.WriteLine("Discovered 0 Device...");
                this.summitSystem.OlympusDiscovery(out discoveredDevices);
                Debug.WriteLine("Complete Discovery...");
                if (discoveredDevices == null)
                {
                    discoveredDevices = new List<DiscoveredDevice>();
                }
                System.Threading.Thread.Sleep(1000);
            } while (discoveredDevices.Count == 0);

            // Connect to the INS with default parameters and ORCA annotations
            Debug.WriteLine("Creating Summit Interface.");

            // Start ORCA Handler
            this.ORCA_TaskComplete = false;
            this.ORCA_Task = 1;
            this.ORCA_Thread = new Thread(new ThreadStart(this.ORCA_Handler));
            this.ORCA_Thread.Start();

            // We can disable ORCA annotations because this is a non-human use INS (see disclaimer)
            // Human-use INS devices ignore the OlympusConnect disableAnnotation flag and always enable annotations.
            // Connect to a device
            ConnectReturn theWarnings;
            APIReturnInfo connectReturn;
            int i = 0;
            do
            {
                Debug.WriteLine("Initialization for Summit Interface");
                connectReturn = summitSystem.StartInsSession(discoveredDevices[0], out theWarnings, this.DisableORCA);
                Debug.WriteLine("Create Summit Result: " + connectReturn.Descriptor);
                Debug.WriteLine("Create Summit Result: " + connectReturn.CtmCommandCode);
                i++;
                if (i > 20)
                {
                    MessageBox.Show("More than 20 initialization error. Please retry later...");
                    return;
                }
            } while (theWarnings.HasFlag(ConnectReturn.InitializationError));

            this.ORCA_TaskComplete = true;
            this.ORCA_Task = 0;
            this.ORCA_Thread.Join();

            ORCA_Handler();

            if (connectReturn.RejectCode != 0)
            {
                MessageBox.Show("Discover INS Failure...");
                return;
            }

            this.Summit_DiscoverRCS.Enabled = false;
            this.Summit_GetStatusButton.Enabled = true;
            this.Summit_LoadConfigurations.Enabled = true;
        }

        /// <summary>
        /// Lock up the textbox for ORCA Project name to prevent mistake by the user. 
        /// Once ORCA Project Name is locked, the Summit Connection can be established.
        /// </summary>
        private void ORCA_Submit_Click(object sender, EventArgs e)
        {
            if (this.ORCA_ProjectName.Enabled)
            {
                this.ORCA_ProjectString = this.ORCA_ProjectName.Text;
                this.ORCA_ProjectName.Enabled = false;

                this.ORCA_Submit.Text = "Locked";
                this.ORCA_Submit.Enabled = false;

                this.Summit_Connect.Enabled = true;
            }
        }

        private void ORCA_Handler()
        {
            try
            {
                int actionCounter = 0;

                do
                {
                    Process[] processList = Process.GetProcesses();
                    foreach (Process myProcess in processList)
                    {
                        if (!String.IsNullOrEmpty(myProcess.MainWindowTitle))
                        {
                            if (myProcess.ProcessName == "Annotator")
                            {
                                if (ORCA_Task == 1)
                                {
                                    myProcess.CloseMainWindow();
                                    actionCounter++;
                                    Thread.Sleep(5000);
                                }
                                else
                                {
                                    ShowWindow(myProcess.MainWindowHandle.ToInt32(), 6);
                                }
                            }
                        }
                    }
                    Thread.Sleep(1000);
                } while (!ORCA_TaskComplete && actionCounter < 20);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
            }
        }

        #endregion

        #region Serial Port / Arduino Communications

        private void Task_SerialPortCheck_Click(object sender, EventArgs e)
        {
            SerialPortSearch();
        }

        private void SerialPortSearch()
        {
            string[] ports = SerialPort.GetPortNames();

            Console.WriteLine("The following serial ports were found:");

            // Display each port name to the console.
            foreach (string port in ports)
            {
                Debug.WriteLine(port);
                SerialPort _serialPort = new SerialPort(port, 9600, Parity.None, 8, StopBits.One);
                _serialPort.Handshake = Handshake.None;
                _serialPort.WriteTimeout = 20;
                try
                {
                    _serialPort.Open();
                    _serialPort.Write("Hello World");
                    Thread.Sleep(100);
                    if (_serialPort.BytesToRead > 0)
                    {
                        Int32 message = _serialPort.ReadChar();
                        if (message == 90)
                        {
                            this.serialPort = _serialPort;
                        }
                    }
                    else
                    {
                        _serialPort.Close();
                    }
                }
                catch (TimeoutException e)
                {
                    Debug.WriteLine("Timeout Exception Reach: " + e.Message);
                }
                catch (System.IO.IOException e)
                {
                    Debug.WriteLine("IO Exception Reach: " + e.Message);
                }
            }

            if (this.serialPort == null)
            {
                MessageBox.Show("No Arduino Found. Serial Port NULL. ");
            }
        }

        #endregion

        #region Delsys Communications

        private void Delsys_ConnectServer_Click(object sender, EventArgs e)
        {
            trignoServer.SetupServer();
            if (trignoServer.connected)
            {
                Delsys_UpdateSensors.Enabled = true;
                Delsys_StartRecording.Enabled = true;
                Delsys_StopRecording.Enabled = true;
            }
        }
        
        private void Delsys_UpdateSensors_Click(object sender, EventArgs e)
        {
            trignoServer.UpdateSensors();
            RadioButton[] sensorIndicators = { Sensor01_LED, Sensor02_LED, Sensor03_LED, Sensor04_LED, Sensor05_LED, Sensor06_LED, Sensor07_LED, Sensor08_LED, Sensor09_LED, Sensor10_LED, Sensor11_LED, Sensor12_LED, Sensor13_LED, Sensor14_LED, Sensor15_LED, Sensor16_LED };
            for (int i = 0; i < 16; i++)
            {
                sensorIndicators[i].Checked = trignoServer.connectedSensors[i] != Trigno.SensorTypes.NoSensor;
                trignoServer.sensorStatus[i] = sensorIndicators[i].Checked;
            }
        }

        #endregion

        #region Montage Recording

        /// <summary>
        /// Execute Montage recordings through another Windows Form Window. 
        /// </summary>
        private void Montage_Run_Click(object sender, EventArgs e)
        {
            MontageTask display = new MontageTask();
            montageSetting.montageDuration = Montage_Duration.Value;
            montageSetting.frameDuration = Montage_FrameDuration.Value;
            switch (Montage_SamplingRate.SelectedIndex)
            {
                case 0:
                    montageSetting.samplingRate = TdSampleRates.Sample0250Hz;
                    break;
                case 1:
                    montageSetting.samplingRate = TdSampleRates.Sample0500Hz;
                    break;
                case 2:
                    montageSetting.samplingRate = TdSampleRates.Sample1000Hz;
                    break;
            }
            display.montageSetting = montageSetting;
            display.FormBorderStyle = FormBorderStyle.None;
            //display.WindowState = FormWindowState.Maximized;
            display.summitSystem = this.summitSystem;
            display.BackColor = Color.DimGray;
            display.StartPosition = FormStartPosition.Manual;
            display.Show();
        }

        #endregion

        #region UI Declaration and Initialization

        /// <summary>
        /// Montage Leads Checkbox Callbacks
        /// </summary>
        private void LeftCh1_Checkbox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            montageSetting.leadSelection[0] = checkbox.CheckState == CheckState.Checked;
        }

        private void RightCh1_Checkbox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            montageSetting.leadSelection[2] = checkbox.CheckState == CheckState.Checked;
        }

        private void LeftCh2_Checkbox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            montageSetting.leadSelection[1] = checkbox.CheckState == CheckState.Checked;
        }

        private void RightCh2_Checkbox_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            montageSetting.leadSelection[3] = checkbox.CheckState == CheckState.Checked;
        }

        #endregion

        #region Summit Methods

        private void Summit_GetStatusButton_Click(object sender, EventArgs e)
        {
            // Read the battery level
            GeneralInterrogateData generalData;
            APIReturnInfo commandReturn = this.summitSystem.ReadGeneralInfo(out generalData);

            // Ensure the command was successful before using the result
            ActiveGroup activeGroup;
            if (commandReturn.RejectCode == 0)
            {
                // Retrieve the battery level from the output buffer
                string batteryLevel = generalData.BatteryStatus.ToString();
                this.Summit_BatteryLevel.Text = "Battery Level: " + batteryLevel + "%";

                string untilEOS = generalData.DaysUntilEos.ToString();
                this.Summit_UntilEOS.Text = "Days until EOS: " + untilEOS + " Days";

                string serialNumber = "Serial Number: " + BitConverter.ToString(generalData.DeviceSerialNumber.ToArray());
                this.Summit_SerialNumber.Text = serialNumber;

                string stimStatus = generalData.TherapyStatusData.TherapyStatus.ToString();
                this.Summit_StimStatus.Text = "Stimulation Status: " + stimStatus;

                if (generalData.TherapyStatusData.TherapyStatus == InterrogateTherapyStatusTypes.TherapyOff)
                {
                    Stimulation_TherapyOn.Enabled = true;
                    Stimulation_TherapyOff.Enabled = false;
                }
                else
                {
                    Stimulation_TherapyOn.Enabled = false;
                    Stimulation_TherapyOff.Enabled = true;
                }

                activeGroup = generalData.TherapyStatusData.ActiveGroup;
            }
            else
            {
                MessageBox.Show("Read General Info Failed...");
                return;
            }
            
            // Disable Sensing
            SenseStates sensingState = SenseStates.None;

            APIReturnInfo returnInfo;
            returnInfo = this.summitSystem.WriteSensingState(sensingState, 0x00);
            if (returnInfo.RejectCode != 0)
            {
                MessageBox.Show("Error Setting Status: " + returnInfo.Descriptor);
                return;
            }
            Debug.WriteLine("Setting Sensing State Correctly: " + returnInfo.Descriptor);

            // Get Lead Impedances
            LeadIntegrityTestResult testResultBuffer;
            APIReturnInfo testReturnInfo = this.summitSystem.LeadIntegrityTest(
                    new List<Tuple<byte, byte>> {
                        // Electrode 1
                        new Tuple<byte, byte>(0, 1),
                        new Tuple<byte, byte>(0, 2),
                        new Tuple<byte, byte>(0, 3),
                        new Tuple<byte, byte>(1, 2),
                        new Tuple<byte, byte>(1, 3),
                        new Tuple<byte, byte>(2, 3),
                        new Tuple<byte, byte>(0, 16),
                        new Tuple<byte, byte>(1, 16),
                        new Tuple<byte, byte>(2, 16),
                        new Tuple<byte, byte>(3, 16)
                    },
                    out testResultBuffer);
            
            if (testReturnInfo.RejectCode == 0 && testResultBuffer != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    this.impedanceResults[i] = testResultBuffer.PairResults[i].Impedance;
                }
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    this.impedanceResults[i] = 0;
                }
            }
            
            testReturnInfo = this.summitSystem.LeadIntegrityTest(
                    new List<Tuple<byte, byte>> {
                        // Electrode 2
                        new Tuple<byte, byte>(4, 5),
                        new Tuple<byte, byte>(4, 6),
                        new Tuple<byte, byte>(4, 7),
                        new Tuple<byte, byte>(5, 6),
                        new Tuple<byte, byte>(5, 7),
                        new Tuple<byte, byte>(6, 7),
                        new Tuple<byte, byte>(4, 16),
                        new Tuple<byte, byte>(5, 16),
                        new Tuple<byte, byte>(6, 16),
                        new Tuple<byte, byte>(7, 16)

                    },
                    out testResultBuffer);

            if (testReturnInfo.RejectCode == 0 && testResultBuffer != null)
            {
                for (int i = 10; i < 20; i++)
                {
                    this.impedanceResults[i] = testResultBuffer.PairResults[i-10].Impedance;
                }
            }
            else
            {
                for (int i = 10; i < 20; i++)
                {
                    this.impedanceResults[i] = 0;
                }
            }

            testReturnInfo = this.summitSystem.LeadIntegrityTest(
                    new List<Tuple<byte, byte>> {
                        // Electrode 3
                        new Tuple<byte, byte>(8, 9),
                        new Tuple<byte, byte>(8, 10),
                        new Tuple<byte, byte>(8, 11),
                        new Tuple<byte, byte>(9, 10),
                        new Tuple<byte, byte>(9, 11),
                        new Tuple<byte, byte>(10, 11),
                        new Tuple<byte, byte>(8, 16),
                        new Tuple<byte, byte>(9, 16),
                        new Tuple<byte, byte>(10, 16),
                        new Tuple<byte, byte>(11, 16)

                    },
                    out testResultBuffer);

            if (testReturnInfo.RejectCode == 0 && testResultBuffer != null)
            {
                for (int i = 20; i < 30; i++)
                {
                    this.impedanceResults[i] = testResultBuffer.PairResults[i - 20].Impedance;
                }
            }
            else
            {
                for (int i = 20; i < 30; i++)
                {
                    this.impedanceResults[i] = 0;
                }
            }

            testReturnInfo = this.summitSystem.LeadIntegrityTest(
                    new List<Tuple<byte, byte>> {
                        // Electrode 4
                        new Tuple<byte, byte>(12, 13),
                        new Tuple<byte, byte>(12, 14),
                        new Tuple<byte, byte>(12, 15),
                        new Tuple<byte, byte>(13, 14),
                        new Tuple<byte, byte>(13, 15),
                        new Tuple<byte, byte>(14, 15),
                        new Tuple<byte, byte>(12, 16),
                        new Tuple<byte, byte>(13, 16),
                        new Tuple<byte, byte>(14, 16),
                        new Tuple<byte, byte>(15, 16)

                    },
                    out testResultBuffer);

            if (testReturnInfo.RejectCode == 0 && testResultBuffer != null)
            {
                for (int i = 30; i < 40; i++)
                {
                    this.impedanceResults[i] = testResultBuffer.PairResults[i - 30].Impedance;
                }
            }
            else
            {
                for (int i = 30; i < 40; i++)
                {
                    this.impedanceResults[i] = 0;
                }
            }

            // Get Stimulation Settings
            Label[] amplitude = { this.Status_StimSet1_Amp, this.Status_StimSet2_Amp, this.Status_StimSet3_Amp, this.Status_StimSet4_Amp };
            Label[] pulsewidth = { this.Status_StimSet1_PW, this.Status_StimSet2_PW, this.Status_StimSet3_PW, this.Status_StimSet4_PW };
            Label[] frequency = { this.Status_StimSet1_Freq, this.Status_StimSet2_Freq, this.Status_StimSet3_Freq, this.Status_StimSet4_Freq };
            Label[] contacts = { this.Status_StimSet1_Contacts, this.Status_StimSet2_Contacts, this.Status_StimSet3_Contacts, this.Status_StimSet4_Contacts };

            commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group0, out therapyGroups[0]);
            commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group1, out therapyGroups[1]);
            commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group2, out therapyGroups[2]);
            commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group3, out therapyGroups[3]);
            commandReturn = this.summitSystem.ReadStimAmplitudeLimits(GroupNumber.Group0, out amplitudeLimits[0]);
            commandReturn = this.summitSystem.ReadStimAmplitudeLimits(GroupNumber.Group1, out amplitudeLimits[1]);
            commandReturn = this.summitSystem.ReadStimAmplitudeLimits(GroupNumber.Group2, out amplitudeLimits[2]);
            commandReturn = this.summitSystem.ReadStimAmplitudeLimits(GroupNumber.Group3, out amplitudeLimits[3]);

            if (commandReturn.RejectCode == 0)
            {
                int i = 0;
                foreach(TherapyProgram therapyProgram in therapyGroups[(int)activeGroup].Programs)
                {
                    if (therapyProgram.IsEnabled == ProgramEnables.Version0Enabled)
                    {
                        amplitude[i].Text = therapyProgram.AmplitudeInMilliamps.ToString() + "mA";
                        pulsewidth[i].Text = therapyProgram.PulseWidthInMicroseconds.ToString() + "uS";
                        frequency[i].Text = therapyGroups[(int)activeGroup].RateInHz.ToString() + "Hz";
                        contacts[i].Text = "";
                        int electrodeID = 0;
                        foreach (Electrode electrode in therapyProgram.Electrodes)
                        {
                            if (!electrode.IsOff)
                            {
                                if (electrode.ElectrodeType == ElectrodeTypes.Anode)
                                {
                                    contacts[i].Text = contacts[i].Text + "E" + electrodeID + "+" + Environment.NewLine;
                                }
                                else
                                {
                                    contacts[i].Text = contacts[i].Text + "E" + electrodeID + "-" + Environment.NewLine;
                                }
                            }
                            electrodeID++;
                        }
                    }
                    else
                    {
                        amplitude[i].Text = "Disabled";
                        pulsewidth[i].Text = "Disabled";
                        frequency[i].Text = "Disabled";
                        contacts[i].Text = "Disabled";
                    }
                    i++;
                }
                Stimulation_ActiveGroupSelection.SelectedIndex = (int)activeGroup;
            }
            else
            {
                MessageBox.Show("Read Theray Group Status Failed...");
                return;
            }
        }

        // Load Previous Confiugrations
        private void Summit_LoadConfigurations_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileSelector = new OpenFileDialog();
            if (fileSelector.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Debug.WriteLine(fileSelector.FileName);
                    byte[] rawBytes = File.ReadAllBytes(fileSelector.FileName);
                    
                    if (rawBytes.Length != 248)
                    {
                        MessageBox.Show("Cannot Load Configuration. Incorrect data length");
                        return;
                    }

                    string magic = BitConverter.ToString(rawBytes.Skip(0).Take(3).ToArray());
                    Debug.WriteLine(magic);
                    if (string.Equals(magic,"BML"))
                    {
                        MessageBox.Show("Cannot Load Td Setting. Incorrect magic number");
                        return;
                    }
                    magic = BitConverter.ToString(rawBytes.Skip(44).Take(3).ToArray());
                    Debug.WriteLine(magic);
                    if (string.Equals(magic, "BML"))
                    {
                        MessageBox.Show("Cannot Load MISC Setting. Incorrect magic number");
                        return;
                    }
                    magic = BitConverter.ToString(rawBytes.Skip(104).Take(3).ToArray());
                    Debug.WriteLine(magic);
                    if (string.Equals(magic, "BML"))
                    {
                        MessageBox.Show("Cannot Load MISC Setting. Incorrect magic number");
                        return;
                    }

                    // Disable Sensing before loading data. 
                    SenseStates sensingState = SenseStates.None;

                    APIReturnInfo returnInfo;
                    returnInfo = this.summitSystem.WriteSensingState(sensingState, 0x00);
                    if (returnInfo.RejectCode != 0)
                    {
                        MessageBox.Show("Error Setting Status: " + returnInfo.Descriptor);
                        return;
                    }
                    Debug.WriteLine("Setting Sensing State Correctly: " + returnInfo.Descriptor);

                    this.sensingSetting = GetTimeDomainSetting(rawBytes.Skip(0).Take(44).ToArray());
                    // Time Channel Settings
                    List<TimeDomainChannel> TimeDomainChannels = new List<TimeDomainChannel>(4);
                    for (int channel = 0; channel < 4; channel++)
                    {
                        if (this.sensingSetting.enableStatus[channel])
                        {
                            Debug.WriteLine("Channel" + channel.ToString() + " State: Enable");
                            TimeDomainChannels.Add(new TimeDomainChannel(
                                this.sensingSetting.samplingRate[channel],
                                this.sensingSetting.channelMux[channel, 1],
                                this.sensingSetting.channelMux[channel, 0],
                                this.sensingSetting.evokedResponse[channel],
                                this.sensingSetting.stage1LPF[channel],
                                this.sensingSetting.stage2LPF[channel],
                                this.sensingSetting.stage1HPF[channel]));
                        }
                        else
                        {
                            Debug.WriteLine("Channel" + channel.ToString() + " State: Disable");
                            TimeDomainChannels.Add(new TimeDomainChannel(
                                TdSampleRates.Disabled,
                                this.sensingSetting.channelMux[channel, 1],
                                this.sensingSetting.channelMux[channel, 0],
                                this.sensingSetting.evokedResponse[channel],
                                this.sensingSetting.stage1LPF[channel],
                                this.sensingSetting.stage2LPF[channel],
                                this.sensingSetting.stage1HPF[channel]));
                        }
                    }

                    APIReturnInfo returnInfoBuffer = this.summitSystem.WriteSensingTimeDomainChannels(TimeDomainChannels);
                    if (returnInfoBuffer.RejectCode != 0)
                    {
                        MessageBox.Show("Fail to Update TD Configuration: " + returnInfoBuffer.Descriptor);
                        return;
                    }
                    this.configurationCheck[0] = true;
                    Debug.WriteLine("Configured TD Channels: " + returnInfoBuffer.Descriptor);

                    // FFT Configurations
                    this.fftSetting = GetFFTSetting(rawBytes.Skip(44).Take(60).ToArray());
                    FftConfiguration fftChannel = new FftConfiguration(
                        this.fftSetting.fftSizes,
                        this.fftSetting.interval,
                        this.fftSetting.windowLoad,
                        this.fftSetting.enableWindow,
                        this.fftSetting.shiftBits);

                    returnInfoBuffer = this.summitSystem.WriteSensingFftSettings(fftChannel);
                    if (returnInfoBuffer.RejectCode != 0)
                    {
                        MessageBox.Show("Fail to Update FFT Configuration: " + returnInfoBuffer.Descriptor);
                        return;
                    }
                    this.configurationCheck[1] = true;
                    Debug.WriteLine("Configured FFT Channels: " + returnInfoBuffer.Descriptor);
                    
                    returnInfoBuffer = this.summitSystem.WriteSensingPowerChannels(this.fftSetting.powerbandEnables, this.fftSetting.powerbandSetting);
                    if (returnInfoBuffer.RejectCode != 0)
                    {
                        MessageBox.Show("Fail to Update Power Configuration: " + returnInfoBuffer.Descriptor);
                        return;
                    }
                    this.configurationCheck[2] = true;
                    
                    this.miscSetting = GetMISCSetting(rawBytes.Skip(104).Take(20).ToArray());
                    MiscellaneousSensing miscSettings = new MiscellaneousSensing();
                    miscSettings.Bridging = this.miscSetting.bridging;
                    miscSettings.StreamingRate = this.miscSetting.streamRate;
                    miscSettings.LrTriggers = this.miscSetting.lrTrigger;
                    miscSettings.LrPostBufferTime = this.miscSetting.lrBuffer;

                    returnInfoBuffer = this.summitSystem.WriteSensingMiscSettings(miscSettings);
                    if (returnInfoBuffer.RejectCode != 0)
                    {
                        MessageBox.Show("Fail to Update MISC Configuration: " + returnInfoBuffer.Descriptor);
                        return;
                    }
                    Debug.WriteLine("Configured MISC Channels: " + returnInfoBuffer.Descriptor);

                    returnInfoBuffer = this.summitSystem.WriteSensingAccelSettings(this.miscSetting.accSampleRate);
                    if (returnInfoBuffer.RejectCode != 0)
                    {
                        MessageBox.Show("Fail to Update Accelerometer Configuration: " + returnInfoBuffer.Descriptor);
                        return;
                    }
                    Debug.WriteLine("Configured Accelerometer Channels: " + returnInfoBuffer.Descriptor);
                    this.configurationCheck[3] = true;
                    
                    Sensing_GetStatusButton_Click(null, null);
                    Sensing_GetFFTStatus_Click(null, null);
                    Sensing_GetMISCStatus_Click(null, null);

                    // This is specifically used because RC+S do not have sensing logs for Accelerometer
                    switch (this.miscSetting.accSampleRate)
                    {
                        case AccelSampleRate.Sample64:
                            this.Sensing_AccSampleRate.SelectedIndex = 0;
                            break;
                        case AccelSampleRate.Sample32:
                            this.Sensing_AccSampleRate.SelectedIndex = 1;
                            break;
                        case AccelSampleRate.Sample16:
                            this.Sensing_AccSampleRate.SelectedIndex = 2;
                            break;
                        case AccelSampleRate.Sample08:
                            this.Sensing_AccSampleRate.SelectedIndex = 3;
                            break;
                        case AccelSampleRate.Sample04:
                            this.Sensing_AccSampleRate.SelectedIndex = 4;
                            break;
                        case AccelSampleRate.Disabled:
                            this.Sensing_AccSampleRate.SelectedIndex = 5;
                            break;
                        default:
                            this.Sensing_AccSampleRate.SelectedIndex = 5;
                            break;
                    }
                    
                    switch (this.fftSetting.fftChannel)
                    {
                        case SenseTimeDomainChannel.Ch0:
                            this.Sensing_FFTStream.SelectedIndex = 0;
                            break;
                        case SenseTimeDomainChannel.Ch1:
                            this.Sensing_FFTStream.SelectedIndex = 1;
                            break;
                        case SenseTimeDomainChannel.Ch2:
                            this.Sensing_FFTStream.SelectedIndex = 2;
                            break;
                        case SenseTimeDomainChannel.Ch3:
                            this.Sensing_FFTStream.SelectedIndex = 3;
                            break;
                        default:
                            this.Sensing_FFTStream.SelectedIndex = -1;
                            break;
                    }
                    MessageBox.Show("Load Successfully");
                }
                catch (System.Security.SecurityException ex)
                {
                    MessageBox.Show("Security Error");
                }
            }
        }

        #endregion

        #region Stimulation Methods

        private UInt32 Stimulation_ElectrodeConfiguration(TherapyElectrodes electrodes, ElectrodeTypes type)
        {
            int electrodeID = 0;
            UInt32 contacts = 0;
            foreach (Electrode electrode in electrodes)
            {
                if (!electrode.IsOff)
                {
                    if (electrode.ElectrodeType == type)
                    {
                        contacts += (UInt32)Math.Pow(2, (double)electrodeID);
                    }
                }
                electrodeID++;
            }
            return contacts;
        }

        private void Stimulation_ActiveGroupSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            TherapyGroup therapyGroup = therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex];

            NumericUpDown[] amplitude = { Stimulation_Param1_Amplitude, Stimulation_Param2_Amplitude, Stimulation_Param3_Amplitude, Stimulation_Param4_Amplitude };
            NumericUpDown[] pulsewidth = { Stimulation_Param1_Pulsewidth, Stimulation_Param2_Pulsewidth, Stimulation_Param3_Pulsewidth, Stimulation_Param4_Pulsewidth };
            NumericUpDown[] frequency = { Stimulation_Param1_Frequency, Stimulation_Param2_Frequency, Stimulation_Param3_Frequency, Stimulation_Param4_Frequency };
            Label[] amplitudeRange = { Stimulation_Param1_AmplitudeRange, Stimulation_Param2_AmplitudeRange, Stimulation_Param3_AmplitudeRange, Stimulation_Param4_AmplitudeRange };
            Label[] pulsewidthRange = { Stimulation_Param1_PulsewidthRange, Stimulation_Param2_PulsewidthRange, Stimulation_Param3_PulsewidthRange, Stimulation_Param4_PulsewidthRange };
            Label[] frequencyRange = { Stimulation_Param1_FrequencyRange, Stimulation_Param2_FrequencyRange, Stimulation_Param3_FrequencyRange, Stimulation_Param4_FrequencyRange };
            Label[] contacts = { Stimulation_Param1_Contacts, Stimulation_Param2_Contacts, Stimulation_Param3_Contacts, Stimulation_Param4_Contacts };

            for (int i = 0; i < 4; i++)
            {
                if (therapyGroup.Programs[i].IsEnabled == ProgramEnables.Version0Enabled)
                {
                    amplitude[i].Enabled = false;
                    switch (i)
                    {
                        case 0:
                            amplitude[i].Minimum = (decimal)amplitudeLimits[Stimulation_ActiveGroupSelection.SelectedIndex].Prog0LowerInMilliamps;
                            amplitude[i].Maximum = (decimal)amplitudeLimits[Stimulation_ActiveGroupSelection.SelectedIndex].Prog0UpperInMilliamps;
                            amplitude[i].Value = (decimal)therapyGroup.Programs[i].AmplitudeInMilliamps;
                            break;
                        case 1:
                            amplitude[i].Minimum = (decimal)amplitudeLimits[Stimulation_ActiveGroupSelection.SelectedIndex].Prog1LowerInMilliamps;
                            amplitude[i].Maximum = (decimal)amplitudeLimits[Stimulation_ActiveGroupSelection.SelectedIndex].Prog1UpperInMilliamps;
                            amplitude[i].Value = (decimal)therapyGroup.Programs[i].AmplitudeInMilliamps;
                            break;
                        case 2:
                            amplitude[i].Minimum = (decimal)amplitudeLimits[Stimulation_ActiveGroupSelection.SelectedIndex].Prog2LowerInMilliamps;
                            amplitude[i].Maximum = (decimal)amplitudeLimits[Stimulation_ActiveGroupSelection.SelectedIndex].Prog2UpperInMilliamps;
                            amplitude[i].Value = (decimal)therapyGroup.Programs[i].AmplitudeInMilliamps;
                            break;
                        case 3:
                            amplitude[i].Minimum = (decimal)amplitudeLimits[Stimulation_ActiveGroupSelection.SelectedIndex].Prog3LowerInMilliamps;
                            amplitude[i].Maximum = (decimal)amplitudeLimits[Stimulation_ActiveGroupSelection.SelectedIndex].Prog3UpperInMilliamps;
                            amplitude[i].Value = (decimal)therapyGroup.Programs[i].AmplitudeInMilliamps;
                            break;
                    }
                    this.stimSetting.amplitude[i] = (double)amplitude[i].Value;
                    amplitudeRange[i].Text = "( " + amplitude[i].Minimum + " - " + amplitude[i].Maximum + " )";

                    pulsewidth[i].Enabled = false;
                    pulsewidth[i].Minimum = therapyGroup.PulseWidthLowerLimitInMicroseconds;
                    pulsewidth[i].Maximum = therapyGroup.PulseWidthUpperLimitInMicroseconds;
                    pulsewidth[i].Value = therapyGroup.Programs[i].PulseWidthInMicroseconds;
                    this.stimSetting.pulsewidth[i] = (int)pulsewidth[i].Value;
                    pulsewidthRange[i].Text = "( " + pulsewidth[i].Minimum + " - " + pulsewidth[i].Maximum + " )";

                    frequency[i].Enabled = false;
                    frequency[i].Minimum = (decimal)therapyGroup.RateLowerLimitInHz;
                    frequency[i].Maximum = (decimal)therapyGroup.RateUpperLimitInHz;
                    frequency[i].Value = (decimal)therapyGroup.RateInHz;
                    this.stimSetting.frequency[i] = (double)frequency[i].Value;
                    frequencyRange[i].Text = "( " + frequency[i].Minimum + " - " + frequency[i].Maximum + " )";

                    contacts[i].Text = "Contacts: (";
                    contacts[i].Text += Stimulation_ElectrodeConfiguration(therapyGroup.Programs[i].Electrodes, ElectrodeTypes.Anode).ToString();
                    contacts[i].Text += ",";
                    contacts[i].Text += Stimulation_ElectrodeConfiguration(therapyGroup.Programs[i].Electrodes, ElectrodeTypes.Cathode).ToString();
                    contacts[i].Text += ")";
                    this.stimSetting.plusContacts[i] = (int)Stimulation_ElectrodeConfiguration(therapyGroup.Programs[i].Electrodes, ElectrodeTypes.Anode);
                    this.stimSetting.minusContacts[i] = (int)Stimulation_ElectrodeConfiguration(therapyGroup.Programs[i].Electrodes, ElectrodeTypes.Cathode);
                }
                else
                {
                    amplitude[i].Enabled = false;
                    amplitude[i].Minimum = 0.0M;
                    amplitude[i].Value = 0.0M;
                    amplitude[i].Maximum = 0.0M;
                    amplitudeRange[i].Text = "( 00 - 00 )";
                    this.stimSetting.amplitude[i] = 0;

                    pulsewidth[i].Enabled = false;
                    pulsewidth[i].Minimum = 0.0M;
                    pulsewidth[i].Value = 0.0M;
                    pulsewidth[i].Maximum = 0.0M;
                    pulsewidthRange[i].Text = "( 00 - 00 )";
                    this.stimSetting.pulsewidth[i] = 0;

                    frequency[i].Enabled = false;
                    frequency[i].Minimum = 0.0M;
                    frequency[i].Value = 0.0M;
                    frequency[i].Maximum = 0.0M;
                    frequencyRange[i].Text = "( 00 - 00 )";
                    this.stimSetting.frequency[i] = 0;

                    contacts[i].Text = "Stim Contacts: (+/-)";
                    this.stimSetting.plusContacts[i] = 0;
                    this.stimSetting.minusContacts[i] = 0;
                }

            }
        }

        private void Stimulation_ActivateGroup_Click(object sender, EventArgs e)
        {
            APIReturnInfo commandReturn = this.summitSystem.StimChangeActiveGroup((ActiveGroup)Stimulation_ActiveGroupSelection.SelectedIndex);
            if (commandReturn.RejectCode == 0)
            {
                NumericUpDown[] amplitude = { Stimulation_Param1_Amplitude, Stimulation_Param2_Amplitude, Stimulation_Param3_Amplitude, Stimulation_Param4_Amplitude };
                NumericUpDown[] pulsewidth = { Stimulation_Param1_Pulsewidth, Stimulation_Param2_Pulsewidth, Stimulation_Param3_Pulsewidth, Stimulation_Param4_Pulsewidth };
                NumericUpDown[] frequency = { Stimulation_Param1_Frequency, Stimulation_Param2_Frequency, Stimulation_Param3_Frequency, Stimulation_Param4_Frequency };
                Label[] amplitudeRange = { Stimulation_Param1_AmplitudeRange, Stimulation_Param2_AmplitudeRange, Stimulation_Param3_AmplitudeRange, Stimulation_Param4_AmplitudeRange };
                Label[] pulsewidthRange = { Stimulation_Param1_PulsewidthRange, Stimulation_Param2_PulsewidthRange, Stimulation_Param3_PulsewidthRange, Stimulation_Param4_PulsewidthRange };
                Label[] frequencyRange = { Stimulation_Param1_FrequencyRange, Stimulation_Param2_FrequencyRange, Stimulation_Param3_FrequencyRange, Stimulation_Param4_FrequencyRange };

                for (int i = 0; i < 4; i++)
                {
                    amplitude[i].Enabled = true;
                    pulsewidth[i].Enabled = true;
                    frequency[0].Enabled = true;
                }
            }
            else
            {
                MessageBox.Show("Change Stimulation Group Failed...");
                return;
            }
        }
        
        private void Stimulation_Param1_Amplitude_ValueChanged(object sender, EventArgs e)
        {
            if (Stimulation_Param1_Amplitude.Enabled)
            {
                double? newStimAmplitude;
                double deltaAmplitude = (double)Stimulation_Param1_Amplitude.Value - therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].Programs[0].AmplitudeInMilliamps;
                APIReturnInfo commandReturn = this.summitSystem.StimChangeStepAmp(0, deltaAmplitude, out newStimAmplitude);
                if (commandReturn.RejectCode == 0)
                {
                    Stimulation_Param1_Amplitude.Enabled = false;
                    Stimulation_Param1_Amplitude.Value = (decimal)newStimAmplitude;
                    Stimulation_Param1_Amplitude.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Cannot modify the stimulation amplitude" + commandReturn.Descriptor);
                    Stimulation_Param1_Amplitude.Enabled = false;
                    Stimulation_Param1_Amplitude.Value = (decimal)therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].Programs[0].AmplitudeInMilliamps;
                    Stimulation_Param1_Amplitude.Enabled = true;
                }

                switch (Stimulation_ActiveGroupSelection.SelectedIndex)
                {
                    case 0:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group0, out therapyGroups[0]);
                        break;
                    case 1:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group1, out therapyGroups[1]);
                        break;
                    case 2:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group2, out therapyGroups[2]);
                        break;
                    case 3:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group3, out therapyGroups[3]);
                        break;
                }
                if (commandReturn.RejectCode == 0)
                {
                }
                else
                {
                    MessageBox.Show("Cannot read back the updated amplitude" + commandReturn.Descriptor);
                }
            }
        }

        private void Stimulation_Param2_Amplitude_ValueChanged(object sender, EventArgs e)
        {
            if (Stimulation_Param2_Amplitude.Enabled)
            {
                double? newStimAmplitude;
                double deltaAmplitude = (double)Stimulation_Param2_Amplitude.Value - therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].Programs[1].AmplitudeInMilliamps;
                APIReturnInfo commandReturn = this.summitSystem.StimChangeStepAmp(1, deltaAmplitude, out newStimAmplitude);
                if (commandReturn.RejectCode == 0)
                {
                    Stimulation_Param2_Amplitude.Enabled = false;
                    Stimulation_Param2_Amplitude.Value = (decimal)newStimAmplitude;
                    Stimulation_Param2_Amplitude.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Cannot modify the stimulation amplitude" + commandReturn.Descriptor);
                    Stimulation_Param2_Amplitude.Enabled = false;
                    Stimulation_Param2_Amplitude.Value = (decimal)therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].Programs[1].AmplitudeInMilliamps;
                    Stimulation_Param2_Amplitude.Enabled = true;
                }

                switch (Stimulation_ActiveGroupSelection.SelectedIndex)
                {
                    case 0:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group0, out therapyGroups[0]);
                        break;
                    case 1:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group1, out therapyGroups[1]);
                        break;
                    case 2:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group2, out therapyGroups[2]);
                        break;
                    case 3:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group3, out therapyGroups[3]);
                        break;
                }
                if (commandReturn.RejectCode == 0)
                {
                }
                else
                {
                    MessageBox.Show("Cannot read back the updated amplitude" + commandReturn.Descriptor);
                }
            }
        }

        private void Stimulation_Param3_Amplitude_ValueChanged(object sender, EventArgs e)
        {
            if (Stimulation_Param3_Amplitude.Enabled)
            {
                double? newStimAmplitude;
                double deltaAmplitude = (double)Stimulation_Param3_Amplitude.Value - therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].Programs[2].AmplitudeInMilliamps;
                APIReturnInfo commandReturn = this.summitSystem.StimChangeStepAmp(2, deltaAmplitude, out newStimAmplitude);
                if (commandReturn.RejectCode == 0)
                {
                    Stimulation_Param3_Amplitude.Enabled = false;
                    Stimulation_Param3_Amplitude.Value = (decimal)newStimAmplitude;
                    Stimulation_Param3_Amplitude.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Cannot modify the stimulation amplitude" + commandReturn.Descriptor);
                    Stimulation_Param3_Amplitude.Enabled = false;
                    Stimulation_Param3_Amplitude.Value = (decimal)therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].Programs[2].AmplitudeInMilliamps;
                    Stimulation_Param3_Amplitude.Enabled = true;
                }

                switch (Stimulation_ActiveGroupSelection.SelectedIndex)
                {
                    case 0:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group0, out therapyGroups[0]);
                        break;
                    case 1:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group1, out therapyGroups[1]);
                        break;
                    case 2:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group2, out therapyGroups[2]);
                        break;
                    case 3:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group3, out therapyGroups[3]);
                        break;
                }
                if (commandReturn.RejectCode == 0)
                {
                }
                else
                {
                    MessageBox.Show("Cannot read back the updated amplitude" + commandReturn.Descriptor);
                }
            }
        }

        private void Stimulation_Param4_Amplitude_ValueChanged(object sender, EventArgs e)
        {
            if (Stimulation_Param4_Amplitude.Enabled)
            {
                double? newStimAmplitude;
                double deltaAmplitude = (double)Stimulation_Param4_Amplitude.Value - therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].Programs[3].AmplitudeInMilliamps;
                APIReturnInfo commandReturn = this.summitSystem.StimChangeStepAmp(3, deltaAmplitude, out newStimAmplitude);
                if (commandReturn.RejectCode == 0)
                {
                    Stimulation_Param4_Amplitude.Enabled = false;
                    Stimulation_Param4_Amplitude.Value = (decimal)newStimAmplitude;
                    Stimulation_Param4_Amplitude.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Cannot modify the stimulation amplitude" + commandReturn.Descriptor);
                    Stimulation_Param4_Amplitude.Enabled = false;
                    Stimulation_Param4_Amplitude.Value = (decimal)therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].Programs[3].AmplitudeInMilliamps;
                    Stimulation_Param4_Amplitude.Enabled = true;
                }

                switch (Stimulation_ActiveGroupSelection.SelectedIndex)
                {
                    case 0:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group0, out therapyGroups[0]);
                        break;
                    case 1:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group1, out therapyGroups[1]);
                        break;
                    case 2:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group2, out therapyGroups[2]);
                        break;
                    case 3:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group3, out therapyGroups[3]);
                        break;
                }
                if (commandReturn.RejectCode == 0)
                {
                }
                else
                {
                    MessageBox.Show("Cannot read back the updated amplitude" + commandReturn.Descriptor);
                }
            }
        }

        private void Stimulation_Param1_Frequency_ValueChanged(object sender, EventArgs e)
        {
            if (Stimulation_Param1_Frequency.Enabled)
            {
                double? newFrequency;
                double deltaFrequency = (double)Stimulation_Param1_Frequency.Value - therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].RateInHz;
                APIReturnInfo commandReturn = this.summitSystem.StimChangeStepFrequency(deltaFrequency, true, out newFrequency);
                if (commandReturn.RejectCode == 0)
                {
                    Stimulation_Param1_Frequency.Enabled = false;
                    Stimulation_Param1_Frequency.Value = (decimal)newFrequency;
                    Stimulation_Param2_Frequency.Value = (decimal)newFrequency;
                    Stimulation_Param3_Frequency.Value = (decimal)newFrequency;
                    Stimulation_Param4_Frequency.Value = (decimal)newFrequency;
                    Stimulation_Param1_Frequency.Enabled = true;
                }
                else
                {
                    MessageBox.Show("Cannot modify the stimulation amplitude" + commandReturn.Descriptor);
                    Stimulation_Param1_Frequency.Enabled = false;
                    Stimulation_Param1_Frequency.Value = (decimal)therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].RateInHz;
                    Stimulation_Param2_Frequency.Value = (decimal)therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].RateInHz;
                    Stimulation_Param3_Frequency.Value = (decimal)therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].RateInHz;
                    Stimulation_Param4_Frequency.Value = (decimal)therapyGroups[Stimulation_ActiveGroupSelection.SelectedIndex].RateInHz;
                    Stimulation_Param1_Frequency.Enabled = true;
                }

                switch (Stimulation_ActiveGroupSelection.SelectedIndex)
                {
                    case 0:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group0, out therapyGroups[0]);
                        break;
                    case 1:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group1, out therapyGroups[1]);
                        break;
                    case 2:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group2, out therapyGroups[2]);
                        break;
                    case 3:
                        commandReturn = this.summitSystem.ReadStimGroup(GroupNumber.Group3, out therapyGroups[3]);
                        break;
                }
                if (commandReturn.RejectCode == 0)
                {
                }
                else
                {
                    MessageBox.Show("Cannot read back the updated amplitude" + commandReturn.Descriptor);
                }
            }
        }

        private void Stimulation_TherapyOn_Click(object sender, EventArgs e)
        {
            APIReturnInfo commandReturn = this.summitSystem.StimChangeTherapyOn();
            if (commandReturn.RejectCode != 0)
            {
                MessageBox.Show("Cannot turn on therapy...");
                return;
            }
            Stimulation_TherapyOn.Enabled = false;
            Stimulation_TherapyOff.Enabled = true;
        }

        private void Stimulation_TherapyOff_Click(object sender, EventArgs e)
        {
            APIReturnInfo commandReturn = this.summitSystem.StimChangeTherapyOff(false);
            if (commandReturn.RejectCode != 0)
            {
                MessageBox.Show("Cannot turn off therapy...");
                return;
            }
            Stimulation_TherapyOn.Enabled = true;
            Stimulation_TherapyOff.Enabled = false;
        }

        #endregion

        #region Sensing Methods

        private void Sensing_GetStatusButton_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Read Sensing Configuration Failed... Summit System Not initialized");
                return;
            }

            SensingConfiguration theSensingConfig;
            APIReturnInfo commandReturn = this.summitSystem.ReadSensingSettings(out theSensingConfig);
            if (commandReturn.RejectCode != 0)
            {
                MessageBox.Show("Reading Sensing Configuration Failed... Return Command Code: " + commandReturn.RejectCode);
                return;
            }

            ComboBox[] sampleRate = { this.Sensing_SamplingRate01, this.Sensing_SamplingRate02, this.Sensing_SamplingRate03, this.Sensing_SamplingRate04 };
            ComboBox[] posMux = { this.Sensing_PosMux01, this.Sensing_PosMux02, this.Sensing_PosMux03, this.Sensing_PosMux04 };
            ComboBox[] negMux = { this.Sensing_NegMux01, this.Sensing_NegMux02, this.Sensing_NegMux03, this.Sensing_NegMux04 };
            ComboBox[] evokedRes = { this.Sensing_EvokedRes01, this.Sensing_EvokedRes02, this.Sensing_EvokedRes03, this.Sensing_EvokedRes04 };
            ComboBox[] lpf1 = { this.Sensing_S1LPF01, this.Sensing_S1LPF02, this.Sensing_S1LPF03, this.Sensing_S1LPF04 };
            ComboBox[] lpf2 = { this.Sensing_S2LPF01, this.Sensing_S2LPF02, this.Sensing_S2LPF03, this.Sensing_S2LPF04 };
            ComboBox[] hpf = { this.Sensing_S1HPF01, this.Sensing_S1HPF02, this.Sensing_S1HPF03, this.Sensing_S1HPF04 };
            CheckBox[] enableStatus = { this.Sensing_Enable01, this.Sensing_Enable02, this.Sensing_Enable03, this.Sensing_Enable04 };

            int channelID = 0;
            foreach (TimeDomainChannel tdChannel in theSensingConfig.TimeDomainChannels)
            {
                this.sensingSetting.samplingRate[channelID] = tdChannel.SampleRate;
                this.sensingSetting.channelMux[channelID, 0] = tdChannel.PlusInput;
                this.sensingSetting.channelMux[channelID, 1] = tdChannel.MinusInput;
                this.sensingSetting.evokedResponse[channelID] = tdChannel.EvokedMode;
                this.sensingSetting.stage1LPF[channelID] = tdChannel.Lpf1;
                this.sensingSetting.stage2LPF[channelID] = tdChannel.Lpf2;
                this.sensingSetting.stage1HPF[channelID] = tdChannel.Hpf;

                switch (tdChannel.SampleRate)
                {
                    case (TdSampleRates.Sample0250Hz):
                        sampleRate[channelID].SelectedIndex = 0;
                        enableStatus[channelID].Checked = true;
                        break;
                    case (TdSampleRates.Sample0500Hz):
                        sampleRate[channelID].SelectedIndex = 1;
                        enableStatus[channelID].Checked = true;
                        break;
                    case (TdSampleRates.Sample1000Hz):
                        sampleRate[channelID].SelectedIndex = 2;
                        enableStatus[channelID].Checked = true;
                        break;
                    case (TdSampleRates.Disabled):
                        enableStatus[channelID].Checked = false;
                        break;
                }

                if (tdChannel.PlusInput == TdMuxInputs.Floating)
                {
                    posMux[channelID].SelectedIndex = 0;
                }
                else
                {
                    posMux[channelID].SelectedIndex = (int)Math.Log((double)tdChannel.PlusInput, 2) + 1;
                }

                if (tdChannel.MinusInput == TdMuxInputs.Floating)
                {
                    negMux[channelID].SelectedIndex = 0;
                }
                else
                {
                    negMux[channelID].SelectedIndex = (int)Math.Log((double)tdChannel.MinusInput, 2) + 1;
                }
                
                switch (tdChannel.EvokedMode)
                {
                    case (TdEvokedResponseEnable.Standard):
                        evokedRes[channelID].SelectedIndex = 0;
                        break;
                    case (TdEvokedResponseEnable.Evoked0Input):
                        evokedRes[channelID].SelectedIndex = 1;
                        break;
                    case (TdEvokedResponseEnable.Evoked1Input):
                        evokedRes[channelID].SelectedIndex = 2;
                        break;
                }

                switch (tdChannel.Lpf1)
                {
                    case (TdLpfStage1.Lpf450Hz):
                        lpf1[channelID].SelectedIndex = 0;
                        break;
                    case (TdLpfStage1.Lpf100Hz):
                        lpf1[channelID].SelectedIndex = 1;
                        break;
                    case (TdLpfStage1.Lpf50Hz):
                        lpf1[channelID].SelectedIndex = 2;
                        break;
                }

                switch (tdChannel.Lpf2)
                {
                    case (TdLpfStage2.Lpf1700Hz):
                        lpf2[channelID].SelectedIndex = 0;
                        break;
                    case (TdLpfStage2.Lpf350Hz):
                        lpf2[channelID].SelectedIndex = 1;
                        break;
                    case (TdLpfStage2.Lpf160Hz):
                        lpf2[channelID].SelectedIndex = 2;
                        break;
                    case (TdLpfStage2.Lpf100Hz):
                        lpf2[channelID].SelectedIndex = 3;
                        break;
                }

                switch (tdChannel.Hpf)
                {
                    case (TdHpfs.Hpf0_85Hz):
                        hpf[channelID].SelectedIndex = 0;
                        break;
                    case (TdHpfs.Hpf1_2Hz):
                        hpf[channelID].SelectedIndex = 1;
                        break;
                    case (TdHpfs.Hpf3_3Hz):
                        hpf[channelID].SelectedIndex = 2;
                        break;
                    case (TdHpfs.Hpf8_6Hz):
                        hpf[channelID].SelectedIndex = 3;
                        break;
                }

                channelID++;
            }
        }

        private void Sensing_UpdateStatusButton_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Update Sensing Configuration Failed... Summit System Not initialized");
                return;
            }

            // Disable Sensing
            APIReturnInfo returnInfoBuffer = this.summitSystem.WriteSensingState(SenseStates.None, 0x00);
            if (returnInfoBuffer.RejectCode != 0)
            {
                MessageBox.Show("Fail to disable sensing: " + returnInfoBuffer.Descriptor);
                return;
            }

            ComboBox[] sampleRate = { this.Sensing_SamplingRate01, this.Sensing_SamplingRate02, this.Sensing_SamplingRate03, this.Sensing_SamplingRate04 };
            ComboBox[] posMux = { this.Sensing_PosMux01, this.Sensing_PosMux02, this.Sensing_PosMux03, this.Sensing_PosMux04 };
            ComboBox[] negMux = { this.Sensing_NegMux01, this.Sensing_NegMux02, this.Sensing_NegMux03, this.Sensing_NegMux04 };
            ComboBox[] evokedRes = { this.Sensing_EvokedRes01, this.Sensing_EvokedRes02, this.Sensing_EvokedRes03, this.Sensing_EvokedRes04 };
            ComboBox[] lpf1 = { this.Sensing_S1LPF01, this.Sensing_S1LPF02, this.Sensing_S1LPF03, this.Sensing_S1LPF04 };
            ComboBox[] lpf2 = { this.Sensing_S2LPF01, this.Sensing_S2LPF02, this.Sensing_S2LPF03, this.Sensing_S2LPF04 };
            ComboBox[] hpf = { this.Sensing_S1HPF01, this.Sensing_S1HPF02, this.Sensing_S1HPF03, this.Sensing_S1HPF04 };
            CheckBox[] enableStatus = { this.Sensing_Enable01, this.Sensing_Enable02, this.Sensing_Enable03, this.Sensing_Enable04 };

            // Check all input to make sure it is configured
            for (int channel = 0; channel < 4; channel++)
            {
                if (sampleRate[channel].SelectedIndex == -1 ||
                    posMux[channel].SelectedIndex == -1     ||
                    negMux[channel].SelectedIndex == -1     ||
                    evokedRes[channel].SelectedIndex == -1  ||
                    lpf1[channel].SelectedIndex == -1       ||
                    lpf2[channel].SelectedIndex == -1       ||
                    hpf[channel].SelectedIndex == -1)
                {
                    MessageBox.Show("Not All configuration are set. Please configure everything before updating sensing configuration.");
                    return;
                }
            }

            // Now populate the configuration struct with the selection
            for (int channel = 0; channel < 4; channel++)
            {
                this.sensingSetting.enableStatus[channel] = enableStatus[channel].Checked;
                Debug.WriteLine("Channel" + channel.ToString() + " State: " + this.sensingSetting.enableStatus[channel].ToString());

                switch (sampleRate[channel].SelectedIndex)
                {
                    case 0:
                        this.sensingSetting.samplingRate[channel] = TdSampleRates.Sample0250Hz;
                        break;
                    case 1:
                        this.sensingSetting.samplingRate[channel] = TdSampleRates.Sample0500Hz;
                        break;
                    case 2:
                        this.sensingSetting.samplingRate[channel] = TdSampleRates.Sample1000Hz;
                        break;
                }

                if (posMux[channel].SelectedIndex == 0)
                {
                    this.sensingSetting.channelMux[channel, 0] = TdMuxInputs.Floating;
                }
                else
                {
                    this.sensingSetting.channelMux[channel, 0] = (TdMuxInputs)Math.Pow(2, posMux[channel].SelectedIndex - 1);
                }

                if (negMux[channel].SelectedIndex == 0)
                {
                    this.sensingSetting.channelMux[channel, 1] = TdMuxInputs.Floating;
                }
                else
                {
                    this.sensingSetting.channelMux[channel, 1] = (TdMuxInputs)Math.Pow(2, negMux[channel].SelectedIndex - 1);
                }

                switch (evokedRes[channel].SelectedIndex)
                {
                    case 0:
                        this.sensingSetting.evokedResponse[channel] = TdEvokedResponseEnable.Standard;
                        break;
                    case 1:
                        this.sensingSetting.evokedResponse[channel] = TdEvokedResponseEnable.Evoked0Input;
                        break;
                    case 2:
                        this.sensingSetting.evokedResponse[channel] = TdEvokedResponseEnable.Evoked1Input;
                        break;
                }
                
                switch (lpf1[channel].SelectedIndex)
                {
                    case 0:
                        this.sensingSetting.stage1LPF[channel] = TdLpfStage1.Lpf450Hz;
                        break;
                    case 1:
                        this.sensingSetting.stage1LPF[channel] = TdLpfStage1.Lpf100Hz;
                        break;
                    case 2:
                        this.sensingSetting.stage1LPF[channel] = TdLpfStage1.Lpf50Hz;
                        break;
                }

                switch (lpf2[channel].SelectedIndex)
                {
                    case 0:
                        this.sensingSetting.stage2LPF[channel] = TdLpfStage2.Lpf1700Hz;
                        break;
                    case 1:
                        this.sensingSetting.stage2LPF[channel] = TdLpfStage2.Lpf350Hz;
                        break;
                    case 2:
                        this.sensingSetting.stage2LPF[channel] = TdLpfStage2.Lpf160Hz;
                        break;
                    case 3:
                        this.sensingSetting.stage2LPF[channel] = TdLpfStage2.Lpf100Hz;
                        break;
                }
                
                switch (hpf[channel].SelectedIndex)
                {
                    case 0:
                        this.sensingSetting.stage1HPF[channel] = TdHpfs.Hpf0_85Hz;
                        break;
                    case 1:
                        this.sensingSetting.stage1HPF[channel] = TdHpfs.Hpf1_2Hz;
                        break;
                    case 2:
                        this.sensingSetting.stage1HPF[channel] = TdHpfs.Hpf3_3Hz;
                        break;
                    case 3:
                        this.sensingSetting.stage1HPF[channel] = TdHpfs.Hpf8_6Hz;
                        break;
                }
            }

            List<TimeDomainChannel> TimeDomainChannels = new List<TimeDomainChannel>(4);
            for (int channel = 0; channel < 4; channel++)
            {
                if (this.sensingSetting.enableStatus[channel])
                {
                    Debug.WriteLine("Channel" + channel.ToString() + " State: Enable");
                    TimeDomainChannels.Add(new TimeDomainChannel(
                        this.sensingSetting.samplingRate[channel],
                        this.sensingSetting.channelMux[channel, 1],
                        this.sensingSetting.channelMux[channel, 0],
                        this.sensingSetting.evokedResponse[channel],
                        this.sensingSetting.stage1LPF[channel],
                        this.sensingSetting.stage2LPF[channel],
                        this.sensingSetting.stage1HPF[channel]));
                }
                else
                {
                    Debug.WriteLine("Channel" + channel.ToString() + " State: Disable");
                    TimeDomainChannels.Add(new TimeDomainChannel(
                        TdSampleRates.Disabled,
                        this.sensingSetting.channelMux[channel, 1],
                        this.sensingSetting.channelMux[channel, 0],
                        this.sensingSetting.evokedResponse[channel],
                        this.sensingSetting.stage1LPF[channel],
                        this.sensingSetting.stage2LPF[channel],
                        this.sensingSetting.stage1HPF[channel]));
                }
            }

            returnInfoBuffer = this.summitSystem.WriteSensingTimeDomainChannels(TimeDomainChannels);
            if (returnInfoBuffer.RejectCode != 0)
            {
                MessageBox.Show("Fail to Update TD Configuration: " + returnInfoBuffer.Descriptor);
                return;
            }
            this.configurationCheck[0] = true;
            for (int i = 1; i < 4; i++)
            {
                this.configurationCheck[i] = false;
            }
            Debug.WriteLine("Configured TD Channels: " + returnInfoBuffer.Descriptor);
        }

        private void Sensing_MatchComplete_Click(object sender, EventArgs e)
        {
            if (this.Sensing_SamplingRate01.SelectedIndex > -1)
            {
                this.Sensing_SamplingRate02.SelectedIndex = this.Sensing_SamplingRate01.SelectedIndex;
                this.Sensing_SamplingRate03.SelectedIndex = this.Sensing_SamplingRate01.SelectedIndex;
                this.Sensing_SamplingRate04.SelectedIndex = this.Sensing_SamplingRate01.SelectedIndex;
            }
            if (this.Sensing_EvokedRes01.SelectedIndex > -1)
            {
                this.Sensing_EvokedRes02.SelectedIndex = this.Sensing_EvokedRes01.SelectedIndex;
                this.Sensing_EvokedRes03.SelectedIndex = this.Sensing_EvokedRes01.SelectedIndex;
                this.Sensing_EvokedRes04.SelectedIndex = this.Sensing_EvokedRes01.SelectedIndex;
            }
            if (this.Sensing_S1LPF01.SelectedIndex > -1)
            {
                this.Sensing_S1LPF02.SelectedIndex = this.Sensing_S1LPF01.SelectedIndex;
                this.Sensing_S1LPF03.SelectedIndex = this.Sensing_S1LPF01.SelectedIndex;
                this.Sensing_S1LPF04.SelectedIndex = this.Sensing_S1LPF01.SelectedIndex;
            }
            if (this.Sensing_S2LPF01.SelectedIndex > -1)
            {
                this.Sensing_S2LPF02.SelectedIndex = this.Sensing_S2LPF01.SelectedIndex;
                this.Sensing_S2LPF03.SelectedIndex = this.Sensing_S2LPF01.SelectedIndex;
                this.Sensing_S2LPF04.SelectedIndex = this.Sensing_S2LPF01.SelectedIndex;
            }
            if (this.Sensing_S1HPF01.SelectedIndex > -1)
            {
                this.Sensing_S1HPF02.SelectedIndex = this.Sensing_S1HPF01.SelectedIndex;
                this.Sensing_S1HPF03.SelectedIndex = this.Sensing_S1HPF01.SelectedIndex;
                this.Sensing_S1HPF04.SelectedIndex = this.Sensing_S1HPF01.SelectedIndex;
            }
        }

        private void Sensing_GetFFTStatus_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Read Sensing Configuration Failed... Summit System Not initialized");
                return;
            }

            SensingConfiguration theSensingConfig;
            APIReturnInfo commandReturn = this.summitSystem.ReadSensingSettings(out theSensingConfig);
            if (commandReturn.RejectCode != 0)
            {
                MessageBox.Show("Reading Sensing Configuration Failed: " + commandReturn.Descriptor);
                return;
            }

            this.fftSetting.fftSizes = theSensingConfig.FftConfig.Size;
            switch (theSensingConfig.FftConfig.Size)
            {
                case FftSizes.Size0064:
                    this.Sensing_FFTSize.SelectedIndex = 0;
                    break;
                case FftSizes.Size0256:
                    this.Sensing_FFTSize.SelectedIndex = 1;
                    break;
                case FftSizes.Size1024:
                    this.Sensing_FFTSize.SelectedIndex = 2;
                    break;
            }

            this.fftSetting.enableWindow = theSensingConfig.FftConfig.WindowEnabled;
            this.fftSetting.windowLoad = theSensingConfig.FftConfig.WindowLoad;
            if (!theSensingConfig.FftConfig.WindowEnabled)
            {
                this.Sensing_FFTWindow.SelectedIndex = 3;
            }
            else
            {
                switch (theSensingConfig.FftConfig.WindowLoad)
                {
                    case FftWindowAutoLoads.Hann100:
                        this.Sensing_FFTWindow.SelectedIndex = 0;
                        break;
                    case FftWindowAutoLoads.Hann50:
                        this.Sensing_FFTWindow.SelectedIndex = 1;
                        break;
                    case FftWindowAutoLoads.Hann25:
                        this.Sensing_FFTWindow.SelectedIndex = 2;
                        break;
                }
            }

            this.fftSetting.binSize = -1;
            int i = 0;
            do
            {
                this.fftSetting.binSize = Sensing_GetFFTBinSize(theSensingConfig.TimeDomainChannels[i++].SampleRate, theSensingConfig.FftConfig.Size);
                if (i > 3) break;
            } while (this.fftSetting.binSize < 0);
            Debug.WriteLine("The FFT Bin Size is " + this.fftSetting.binSize.ToString() + " Hz.");

            this.fftSetting.shiftBits = theSensingConfig.FftConfig.BandFormationConfig;
            this.Sensing_FFTBitShift.SelectedIndex = (int)theSensingConfig.FftConfig.BandFormationConfig - 8;

            this.fftSetting.interval = theSensingConfig.FftConfig.Interval;
            this.Sensing_FFTInterval.Value = theSensingConfig.FftConfig.Interval;

            this.fftSetting.powerbandEnables = theSensingConfig.BandEnable;
            this.fftSetting.powerbandSetting = theSensingConfig.PowerChannels;

            if ((theSensingConfig.BandEnable & BandEnables.Ch0Band0Enabled) > 0) this.Sensing_FFTCh1Band1Enable.Checked = true;
            else this.Sensing_FFTCh1Band1Enable.Checked = false;
            if ((theSensingConfig.BandEnable & BandEnables.Ch0Band1Enabled) > 0) this.Sensing_FFTCh1Band2Enable.Checked = true;
            else this.Sensing_FFTCh1Band2Enable.Checked = false;
            if ((theSensingConfig.BandEnable & BandEnables.Ch1Band0Enabled) > 0) this.Sensing_FFTCh2Band1Enable.Checked = true;
            else this.Sensing_FFTCh2Band1Enable.Checked = false;
            if ((theSensingConfig.BandEnable & BandEnables.Ch1Band1Enabled) > 0) this.Sensing_FFTCh2Band2Enable.Checked = true;
            else this.Sensing_FFTCh2Band2Enable.Checked = false;
            if ((theSensingConfig.BandEnable & BandEnables.Ch2Band0Enabled) > 0) this.Sensing_FFTCh3Band1Enable.Checked = true;
            else this.Sensing_FFTCh3Band1Enable.Checked = false;
            if ((theSensingConfig.BandEnable & BandEnables.Ch2Band1Enabled) > 0) this.Sensing_FFTCh3Band2Enable.Checked = true;
            else this.Sensing_FFTCh3Band2Enable.Checked = false;
            if ((theSensingConfig.BandEnable & BandEnables.Ch3Band0Enabled) > 0) this.Sensing_FFTCh4Band1Enable.Checked = true;
            else this.Sensing_FFTCh4Band1Enable.Checked = false;
            if ((theSensingConfig.BandEnable & BandEnables.Ch3Band1Enabled) > 0) this.Sensing_FFTCh4Band2Enable.Checked = true;
            else this.Sensing_FFTCh4Band2Enable.Checked = false;

            i = 0;
            foreach (PowerChannel powerChan in theSensingConfig.PowerChannels)
            {
                switch (i)
                {
                    case 0:
                        this.Sensing_FFTCh1Band1.Value = (decimal)powerChan.Band0Start * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh1Band2.Value = (decimal)powerChan.Band0Stop * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh1Band3.Value = (decimal)powerChan.Band1Start * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh1Band4.Value = (decimal)powerChan.Band1Stop * (decimal)this.fftSetting.binSize;
                        break;
                    case 1:
                        this.Sensing_FFTCh2Band1.Value = (decimal)powerChan.Band0Start * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh2Band2.Value = (decimal)powerChan.Band0Stop * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh2Band3.Value = (decimal)powerChan.Band1Start * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh2Band4.Value = (decimal)powerChan.Band1Stop * (decimal)this.fftSetting.binSize;
                        break;
                    case 2:
                        this.Sensing_FFTCh3Band1.Value = (decimal)powerChan.Band0Start * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh3Band2.Value = (decimal)powerChan.Band0Stop * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh3Band3.Value = (decimal)powerChan.Band1Start * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh3Band4.Value = (decimal)powerChan.Band1Stop * (decimal)this.fftSetting.binSize;
                        break;
                    case 3:
                        this.Sensing_FFTCh4Band1.Value = (decimal)powerChan.Band0Start * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh4Band2.Value = (decimal)powerChan.Band0Stop * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh4Band3.Value = (decimal)powerChan.Band1Start * (decimal)this.fftSetting.binSize;
                        this.Sensing_FFTCh4Band4.Value = (decimal)powerChan.Band1Stop * (decimal)this.fftSetting.binSize;
                        break;
                }
                i++;
            }
        }

        private void Sensing_UpdateFFTStatus_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Read Sensing Configuration Failed... Summit System Not initialized");
                return;
            }

            APIReturnInfo returnInfoBuffer = this.summitSystem.WriteSensingState(SenseStates.None, 0x00);
            if (returnInfoBuffer.RejectCode != 0)
            {
                MessageBox.Show("Fail to disable sensing: " + returnInfoBuffer.Descriptor);
            }

            if (this.Sensing_FFTBitShift.SelectedIndex == -1 ||
                this.Sensing_FFTSize.SelectedIndex == -1     ||
                this.Sensing_FFTWindow.SelectedIndex == -1)
            {
                MessageBox.Show("FFT Settings are not configured.");
                return;
            }

            switch (this.Sensing_FFTSize.SelectedIndex)
            {
                case 0:
                    this.fftSetting.fftSizes = FftSizes.Size0064;
                    break;
                case 1:
                    this.fftSetting.fftSizes = FftSizes.Size0256;
                    break;
                case 2:
                    this.fftSetting.fftSizes = FftSizes.Size1024;
                    break;
            }

            this.fftSetting.interval = (ushort) this.Sensing_FFTInterval.Value;

            switch (this.Sensing_FFTWindow.SelectedIndex)
            {
                case 0:
                    this.fftSetting.windowLoad = FftWindowAutoLoads.Hann100;
                    this.fftSetting.enableWindow = true;
                    break;
                case 1:
                    this.fftSetting.windowLoad = FftWindowAutoLoads.Hann50;
                    this.fftSetting.enableWindow = true;
                    break;
                case 2:
                    this.fftSetting.windowLoad = FftWindowAutoLoads.Hann25;
                    this.fftSetting.enableWindow = true;
                    break;
                case 3:
                    this.fftSetting.windowLoad = FftWindowAutoLoads.Hann100;
                    this.fftSetting.enableWindow = false;
                    break;
            }

            this.fftSetting.shiftBits = (FftWeightMultiplies) (this.Sensing_FFTBitShift.SelectedIndex + 8);

            FftConfiguration fftChannel = new FftConfiguration(
                this.fftSetting.fftSizes, 
                this.fftSetting.interval, 
                this.fftSetting.windowLoad, 
                this.fftSetting.enableWindow,
                this.fftSetting.shiftBits);
            
            returnInfoBuffer = this.summitSystem.WriteSensingFftSettings(fftChannel);
            if (returnInfoBuffer.RejectCode != 0)
            {
                MessageBox.Show("Fail to Update FFT Configuration: " + returnInfoBuffer.Descriptor);
                return;
            }
            this.configurationCheck[1] = true;
            Debug.WriteLine("Configured FFT Channels: " + returnInfoBuffer.Descriptor);

            // Power Band Settings
            List<PowerChannel> PowerChannels = new List<PowerChannel>();

            PowerChannels.Add(new PowerChannel(
                (ushort)((double)this.Sensing_FFTCh1Band1.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh1Band2.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh1Band3.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh1Band4.Value / (double)this.fftSetting.binSize)));

            PowerChannels.Add(new PowerChannel(
                (ushort)((double)this.Sensing_FFTCh2Band1.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh2Band2.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh2Band3.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh2Band4.Value / (double)this.fftSetting.binSize)));

            PowerChannels.Add(new PowerChannel(
                (ushort)((double)this.Sensing_FFTCh3Band1.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh3Band2.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh3Band3.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh3Band4.Value / (double)this.fftSetting.binSize)));

            PowerChannels.Add(new PowerChannel(
                (ushort)((double)this.Sensing_FFTCh4Band1.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh4Band2.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh4Band3.Value / (double)this.fftSetting.binSize),
                (ushort)((double)this.Sensing_FFTCh4Band4.Value / (double)this.fftSetting.binSize)));
            
            BandEnables theBandEnables = 0;
            if (this.Sensing_FFTCh1Band1Enable.Checked) theBandEnables |= BandEnables.Ch0Band0Enabled;
            if (this.Sensing_FFTCh1Band2Enable.Checked) theBandEnables |= BandEnables.Ch0Band1Enabled;
            if (this.Sensing_FFTCh2Band1Enable.Checked) theBandEnables |= BandEnables.Ch1Band0Enabled;
            if (this.Sensing_FFTCh2Band2Enable.Checked) theBandEnables |= BandEnables.Ch1Band1Enabled;
            if (this.Sensing_FFTCh3Band1Enable.Checked) theBandEnables |= BandEnables.Ch2Band0Enabled;
            if (this.Sensing_FFTCh3Band2Enable.Checked) theBandEnables |= BandEnables.Ch2Band1Enabled;
            if (this.Sensing_FFTCh4Band1Enable.Checked) theBandEnables |= BandEnables.Ch3Band0Enabled;
            if (this.Sensing_FFTCh4Band2Enable.Checked) theBandEnables |= BandEnables.Ch3Band1Enabled;

            returnInfoBuffer = this.summitSystem.WriteSensingPowerChannels(theBandEnables, PowerChannels);
            if (returnInfoBuffer.RejectCode != 0)
            {
                MessageBox.Show("Fail to Update Power Configuration: " + returnInfoBuffer.Descriptor);
                return;
            }
            this.configurationCheck[2] = true;

            for (int i = 3; i < 4; i++)
            {
                this.configurationCheck[i] = false;
            }
            Debug.WriteLine("Configured Power Channels: " + returnInfoBuffer.Descriptor);

        }

        private void Sensing_FFTSize_Changed(object sender, EventArgs e)
        {
            if (!this.configurationCheck[0])
            {
                Debug.WriteLine("The Time Channel configuration is not set");
                return;
            }

            this.fftSetting.binSize = -1;
            int i = 0;
            do
            {
                switch (this.Sensing_FFTSize.SelectedIndex)
                {
                    case 0:
                        this.fftSetting.binSize = Sensing_GetFFTBinSize(this.sensingSetting.samplingRate[i++], FftSizes.Size0064);
                        break;
                    case 1:
                        this.fftSetting.binSize = Sensing_GetFFTBinSize(this.sensingSetting.samplingRate[i++], FftSizes.Size0256);
                        break;
                    case 2:
                        this.fftSetting.binSize = Sensing_GetFFTBinSize(this.sensingSetting.samplingRate[i++], FftSizes.Size1024);
                        break;
                }
                
                if (i > 3)
                {
                    break;
                }
            } while (this.fftSetting.binSize < 0);
            Debug.WriteLine("The FFT Bin Size is " + this.fftSetting.binSize.ToString() + " Hz.");
        }

        private double Sensing_GetFFTBinSize(TdSampleRates sampleRate, FftSizes fftSizes)
        {
            double samplingRate = 1000;
            switch (sampleRate)
            {
                case TdSampleRates.Sample0250Hz:
                    samplingRate = 250;
                    break;
                case TdSampleRates.Sample0500Hz:
                    samplingRate = 500;
                    break;
                case TdSampleRates.Sample1000Hz:
                    samplingRate = 1000;
                    break;
                case TdSampleRates.Disabled:
                    return -1;
            }

            switch (fftSizes)
            {
                case FftSizes.Size0064:
                    return samplingRate / 64.0;
                case FftSizes.Size0256:
                    return samplingRate / 256.0;
                case FftSizes.Size1024:
                    return samplingRate / 1024.0;
            }
            return -2;
        }

        private void Sensing_SamplingRate_Changed(object sender, EventArgs e)
        {
            ComboBox selectedBox = (ComboBox)sender;
            this.Sensing_SamplingRate01.SelectedIndex = selectedBox.SelectedIndex;
            this.Sensing_SamplingRate02.SelectedIndex = selectedBox.SelectedIndex;
            this.Sensing_SamplingRate03.SelectedIndex = selectedBox.SelectedIndex;
            this.Sensing_SamplingRate04.SelectedIndex = selectedBox.SelectedIndex;
        }

        private void Streaming_GetStatus_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Read Sensing Configuration Failed... Summit System Not initialized");
                return;
            }
            
            SensingState sensingStatus;
            APIReturnInfo commandReturn = this.summitSystem.ReadSensingState(out sensingStatus);

            if (commandReturn.RejectCode == 0)
            {
                if ((sensingStatus.State & SenseStates.LfpSense) > 0)
                {
                    this.Streaming_LFP.Checked = true;
                    this.streamingOptions[0] = true;
                }
                else
                {
                    this.Streaming_LFP.Checked = false;
                    this.streamingOptions[0] = false;
                }
                if ((sensingStatus.State & SenseStates.Fft) > 0)
                {
                    this.Streaming_FFT.Checked = true;
                    this.streamingOptions[1] = true;
                }
                else
                {
                    this.Streaming_FFT.Checked = false;
                    this.streamingOptions[1] = false;
                }
                if ((sensingStatus.State & SenseStates.Power) > 0)
                {
                    this.Streaming_Power.Checked = true;
                    this.streamingOptions[2] = true;
                }
                else
                {
                    this.Streaming_Power.Checked = false;
                    this.streamingOptions[2] = false;
                }
                if ((sensingStatus.State & SenseStates.DetectionLd0 ) > 0 || (sensingStatus.State & SenseStates.DetectionLd1) > 0)
                {
                    this.Streaming_Detection.Checked = true;
                    this.streamingOptions[3] = true;
                }
                else
                {
                    this.Streaming_Detection.Checked = false;
                    this.streamingOptions[3] = false;
                }
                if ((sensingStatus.State & SenseStates.AdaptiveStim) > 0)
                {
                    this.Streaming_Adaptive.Checked = true;
                    this.streamingOptions[4] = true;
                }
                else
                {
                    this.Streaming_Adaptive.Checked = false;
                    this.streamingOptions[4] = false;
                }
                if (sensingStatus.AccelRate != AccelSampleRate.Disabled)
                {
                    this.Streaming_Accelerometer.Checked = true;
                    this.streamingOptions[5] = true;
                }
                else
                {
                    this.Streaming_Adaptive.Checked = false;
                    this.streamingOptions[5] = false;
                }

                // Time is configured separatly. Cannot be read from sensing state.
                //this.Streaming_Time.Checked = true;
                //this.streamingOptions[6] = true;
                
                if ((sensingStatus.State & SenseStates.LoopRecording) > 0)
                {
                    this.Streaming_LoopRecorder.Checked = true;
                    this.streamingOptions[7] = true;
                }
                else
                {
                    this.Streaming_LoopRecorder.Checked = false;
                    this.streamingOptions[7] = false;
                }
            }
            else
            {
                MessageBox.Show("Error Reading Status.");
                return;
            }
        }

        private void Streaming_SetStatus_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Read Sensing Configuration Failed... Summit System Not initialized");
                return;
            }
            
            APIReturnInfo returnInfo;
            SenseStates sensingState = SenseStates.None;

            if (this.Streaming_LFP.Checked)
            {
                sensingState |= SenseStates.LfpSense;
                streamingOptions[0] = true;
            }
            else
            {
                streamingOptions[0] = false;
            }

            if (this.Streaming_FFT.Checked)
            {
                sensingState |= SenseStates.Fft;
                streamingOptions[1] = true;
            }
            else
            {
                streamingOptions[1] = false;
            }

            if (this.Streaming_Power.Checked)
            {
                sensingState |= SenseStates.Fft;
                sensingState |= SenseStates.Power;
                streamingOptions[2] = true;
            }
            else
            {
                streamingOptions[2] = false;
            }

            if (this.Streaming_Detection.Checked)
            {
                sensingState |= SenseStates.DetectionLd0;
                sensingState |= SenseStates.DetectionLd1;
                streamingOptions[3] = true;
            }
            else
            {
                streamingOptions[3] = false;
            }

            if (this.Streaming_Adaptive.Checked)
            {
                sensingState |= SenseStates.AdaptiveStim;
                streamingOptions[4] = true;
            }
            else
            {
                streamingOptions[4] = false;
            }

            if (this.Streaming_Accelerometer.Checked && (this.miscSetting.accSampleRate != AccelSampleRate.Disabled))
            {
                streamingOptions[5] = true;
            }
            else
            {
                this.Streaming_Accelerometer.Checked = false;
                streamingOptions[5] = false;
            }

            if (this.Streaming_Time.Checked)
            {
                streamingOptions[6] = true;
            }
            else
            {
                streamingOptions[6] = false;
            }

            if (this.Streaming_LoopRecorder.Checked)
            {
                sensingState |= SenseStates.LoopRecording;
                streamingOptions[7] = true;
            }
            else
            {
                streamingOptions[7] = false;
            }

            SenseTimeDomainChannel FFTChannel;
            switch (this.Sensing_FFTStream.SelectedIndex)
            {
                case 0:
                    FFTChannel = SenseTimeDomainChannel.Ch0;
                    break;
                case 1:
                    FFTChannel = SenseTimeDomainChannel.Ch1;
                    break;
                case 2:
                    FFTChannel = SenseTimeDomainChannel.Ch2;
                    break;
                case 3:
                    FFTChannel = SenseTimeDomainChannel.Ch3;
                    break;
                default:
                    FFTChannel = SenseTimeDomainChannel.Ch0;
                    break;
            }

            returnInfo = this.summitSystem.WriteSensingState(sensingState, FFTChannel);
            if (returnInfo.RejectCode != 0)
            {
                MessageBox.Show("Error Setting Status: " + returnInfo.Descriptor);
                return;
            }
            Debug.WriteLine("Setting Sensing State Correctly: " + returnInfo.Descriptor);
        }

        private void Sensing_GetMISCStatus_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Read Sensing Configuration Failed... Summit System Not initialized");
                return;
            }

            SensingConfiguration theSensingConfig;
            APIReturnInfo commandReturn = this.summitSystem.ReadSensingSettings(out theSensingConfig);
            if (commandReturn.RejectCode != 0)
            {
                MessageBox.Show("Reading Sensing Configuration Failed: " + commandReturn.Descriptor);
                return;
            }

            this.miscSetting.bridging = theSensingConfig.MiscSensing.Bridging;
            this.miscSetting.lrBuffer = theSensingConfig.MiscSensing.LrPostBufferTime;
            this.miscSetting.lrTrigger = theSensingConfig.MiscSensing.LrTriggers;
            this.miscSetting.streamRate = theSensingConfig.MiscSensing.StreamingRate;

            switch (theSensingConfig.MiscSensing.Bridging)
            {
                case BridgingConfig.None:
                    this.Sensing_BridgingSetting.SelectedIndex = 0;
                    break;
                case BridgingConfig.Bridge0to2Enabled:
                    this.Sensing_BridgingSetting.SelectedIndex = 1;
                    break;
                case BridgingConfig.Bridge1to3Enabled:
                    this.Sensing_BridgingSetting.SelectedIndex = 2;
                    break;
            }

            this.Sensing_FrameRate.SelectedIndex = (int)theSensingConfig.MiscSensing.StreamingRate - 3;
            this.Sensing_LoopRecorderBuffer.Value = theSensingConfig.MiscSensing.LrPostBufferTime;
            
            if (theSensingConfig.MiscSensing.LrTriggers == LoopRecordingTriggers.None)
            {
                this.Sensing_LoopRecorderTrg.SelectedIndex = 0;
            }
            else
            {
                this.Sensing_LoopRecorderTrg.SelectedIndex = (int)Math.Log((double)theSensingConfig.MiscSensing.LrTriggers, 2) + 1;
            }

            // NOTE: Accelerometer Setting Cannot Be Read
        }

        private void Sensing_UpdateMISCStatus_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Update Sensing Configuration Failed... Summit System Not initialized");
                return;
            }
            
            if (this.Sensing_AccSampleRate.SelectedIndex == -1 ||
                this.Sensing_BridgingSetting.SelectedIndex == -1 ||
                this.Sensing_LoopRecorderTrg.SelectedIndex == -1 ||
                this.Sensing_FrameRate.SelectedIndex == -1)
            {
                MessageBox.Show("Update Sensing Confiugration Failed... The MISC Options are not set");
                return;
            }

            if (this.Sensing_AccSampleRate.SelectedIndex == 5)
            {
                this.miscSetting.accSampleRate = AccelSampleRate.Disabled;
            }
            else
            {
                this.miscSetting.accSampleRate = (AccelSampleRate)this.Sensing_AccSampleRate.SelectedIndex;
            }

            switch (this.Sensing_BridgingSetting.SelectedIndex)
            {
                case 0:
                    this.miscSetting.bridging = BridgingConfig.None;
                    break;
                case 1:
                    this.miscSetting.bridging = BridgingConfig.Bridge0to2Enabled;
                    break;
                case 2:
                    this.miscSetting.bridging = BridgingConfig.Bridge1to3Enabled;
                    break;
            }

            this.miscSetting.streamRate = (StreamingFrameRate)(this.Sensing_FrameRate.SelectedIndex + 3);

            if (this.Sensing_LoopRecorderTrg.SelectedIndex != 0)
            {
                this.miscSetting.lrTrigger = (LoopRecordingTriggers) (1 << (this.Sensing_LoopRecorderTrg.SelectedIndex - 1));
            }
            else
            {
                this.miscSetting.lrTrigger = LoopRecordingTriggers.None;
            }

            this.miscSetting.lrBuffer = (ushort) this.Sensing_LoopRecorderBuffer.Value;

            APIReturnInfo returnInfo;
            MiscellaneousSensing miscSettings = new MiscellaneousSensing();
            miscSettings.Bridging = this.miscSetting.bridging;
            miscSettings.StreamingRate = this.miscSetting.streamRate;
            miscSettings.LrTriggers = this.miscSetting.lrTrigger;
            miscSettings.LrPostBufferTime = this.miscSetting.lrBuffer;
            
            returnInfo = this.summitSystem.WriteSensingMiscSettings(miscSettings);
            if (returnInfo.RejectCode != 0)
            {
                MessageBox.Show("Fail to Update MISC Configuration: " + returnInfo.Descriptor);
                return;
            }
            Debug.WriteLine("Configured MISC Channels: " + returnInfo.Descriptor);

            returnInfo = this.summitSystem.WriteSensingAccelSettings(this.miscSetting.accSampleRate);
            if (returnInfo.RejectCode != 0)
            {
                MessageBox.Show("Fail to Update Accelerometer Configuration: " + returnInfo.Descriptor);
                return;
            }
            Debug.WriteLine("Configured Accelerometer Channels: " + returnInfo.Descriptor);
            
            this.configurationCheck[3] = true;
        }

        #endregion

        #region Streaming Methods

        private void Streaming_Disable_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Read Sensing Configuration Failed... Summit System Not initialized");
                return;
            }

            SenseStates sensingState = SenseStates.None;

            APIReturnInfo returnInfo;
            returnInfo = this.summitSystem.WriteSensingState(sensingState, 0x00);
            if (returnInfo.RejectCode != 0)
            {
                MessageBox.Show("Error Setting Status: " + returnInfo.Descriptor);
                return;
            }
            Debug.WriteLine("Setting Sensing State Correctly: " + returnInfo.Descriptor);
        }

        private void Task_MonitorPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.workingMonitor = screens[this.Task_MonitorPicker.SelectedIndex];
            this.MonitorSizeLabel.Text = this.workingMonitor.WorkingArea.Width.ToString() + " x " + this.workingMonitor.WorkingArea.Height.ToString();
        }

        #endregion 

        /*************************************/
        /******* Data Acquisition Code *******/
        /*************************************/
        private void DataAcquisition_Run_Click(object sender, EventArgs e)
        {
            VoluntaryMovement display = new VoluntaryMovement();

            display.tdSettings = this.sensingSetting;
            display.fftSettings = this.fftSetting;
            display.miscSettings = this.miscSetting;
            display.stimSetting = this.stimSetting;
            display.impedance = this.impedanceResults;

            display.FormBorderStyle = FormBorderStyle.None;
            display.WindowState = FormWindowState.Maximized;
            display.summitSystem = this.summitSystem;
            display.streamingOption = this.streamingOptions;
            display.StartPosition = FormStartPosition.Manual;
            display.Location = this.workingMonitor.WorkingArea.Location;

            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Entering Debug Mode");

                display.debugMode = true;
                display.Show();

                return;
            }

            if (!this.configurationCheck.All(configuration => configuration == true))
            {
                MessageBox.Show("Not all configuration checked.");
                return;
            }
            
            display.debugMode = false;
            display.Show();
        }

        private void ExtensiveSampling_Run_Click(object sender, EventArgs e)
        {
            ExtensiveSampling display = new ExtensiveSampling();

            display.tdSettings = this.sensingSetting;
            display.fftSettings = this.fftSetting;
            display.miscSettings = this.miscSetting;
            display.stimSetting = this.stimSetting;
            display.impedance = this.impedanceResults;

            display.FormBorderStyle = FormBorderStyle.None;
            display.WindowState = FormWindowState.Maximized;
            display.summitSystem = this.summitSystem;
            display.streamingOption = this.streamingOptions;
            display.StartPosition = FormStartPosition.Manual;
            display.Location = this.workingMonitor.WorkingArea.Location;
            display.serialPort = this.serialPort;

            display.trignoServer = this.trignoServer;

            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Entering Debug Mode");

                display.debugMode = true;
                display.Show();

                return;
            }

            if (!this.configurationCheck.All(configuration => configuration == true))
            {
                MessageBox.Show("Not all configuration checked.");
                return;
            }
            
            display.debugMode = false;
            display.Show();

        }

        private void StimAndRecord_Run_Click(object sender, EventArgs e)
        {
            StimAndRecord display = new StimAndRecord();

            display.tdSettings = this.sensingSetting;
            display.fftSettings = this.fftSetting;
            display.miscSettings = this.miscSetting;
            display.stimSetting = this.stimSetting;
            display.impedance = this.impedanceResults;

            display.FormBorderStyle = FormBorderStyle.None;
            display.WindowState = FormWindowState.Maximized;
            display.summitSystem = this.summitSystem;
            display.streamingOption = this.streamingOptions;
            display.StartPosition = FormStartPosition.Manual;
            display.Location = this.workingMonitor.WorkingArea.Location;
            display.serialPort = this.serialPort;

            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Entering Debug Mode");

                display.debugMode = true;
                display.Show();

                return;
            }

            if (!this.configurationCheck.All(configuration => configuration == true))
            {
                MessageBox.Show("Not all configuration checked.");
                return;
            }

            display.debugMode = false;
            display.Show();

        }

        private void Delsys_StartRecording_Click(object sender, EventArgs e)
        {
            trignoServer.StartAcquisition();
            trignoServer.StartDataWriter();
        }

        private void Delsys_StopRecording_Click(object sender, EventArgs e)
        {
            trignoServer.StopAcquisition();
        }
    }
}
