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

        // Configuration Check
        bool[] configurationCheck;
        bool[] streamingOptions;

        // Task Monitor Check
        Screen[] screens;
        Screen workingMonitor;

        DataManager dataManager;

        // To Handle ORCA Window
        [DllImport("User32")]
        private static extern int ShowWindow(int windowHandler, int showCMD);
        private Thread ORCA_Thread = null;
        private int ORCA_Task;
        private bool ORCA_TaskComplete;

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
                connectReturn = summitSystem.StartInsSession(discoveredDevices[0], out theWarnings);
                Debug.WriteLine("Create Summit Result: " + connectReturn.Descriptor);
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
        }

        private void Summit_GetStatusButton_Click(object sender, EventArgs e)
        {
            // Read the battery level
            GeneralInterrogateData generalData;
            APIReturnInfo commandReturn = this.summitSystem.ReadGeneralInfo(out generalData);
            
            // Ensure the command was successful before using the result
            if (commandReturn.RejectCode == 0)
            {
                // Retrieve the battery level from the output buffer
                string batteryLevel = generalData.BatteryStatus.ToString();
                this.Summit_BatteryLevel.Text = "Battery Level: " + batteryLevel + "%";

                string untilEOS = generalData.DaysUntilEos.ToString();
                this.Summit_UntilEOS.Text = "Days until EOS: " + untilEOS + " Days";

                string serialNumber = generalData.DeviceSerialNumber[0].ToString();
                this.Summit_SerialNumber.Text = "Serial Number: " + serialNumber;

                string stimStatus = generalData.TherapyStatusData.TherapyStatus.ToString();
                this.Summit_StimStatus.Text = "Stimulation Status: " + stimStatus;
            }
            else
            {
                MessageBox.Show("Read General Info Failed...");
            }

            SensingState sensingStatus;
            commandReturn = this.summitSystem.ReadSensingState(out sensingStatus);

            if (commandReturn.RejectCode == 0)
            {
                string stateValue = sensingStatus.State.ToString();
                this.Summit_SenseStatus.Text = "Sensing Status: " + stateValue;
            }
            else
            {
                MessageBox.Show("Read Sensing Status Failed...");
            }
            
        }

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

                posMux[channelID].SelectedIndex = (int)tdChannel.PlusInput;
                negMux[channelID].SelectedIndex = (int)tdChannel.MinusInput;
                
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

                this.sensingSetting.channelMux[channel, 0] = (TdMuxInputs) posMux[channel].SelectedIndex;
                this.sensingSetting.channelMux[channel, 1] = (TdMuxInputs) negMux[channel].SelectedIndex;

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

        private void DataAcquisition_Run_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Entering Debug Mode");

                VoluntaryMovement displayTest = new VoluntaryMovement();

                displayTest.FormBorderStyle = FormBorderStyle.None;
                displayTest.WindowState = FormWindowState.Maximized;
                displayTest.summitSystem = this.summitSystem;
                displayTest.streamingOption = this.streamingOptions;
                displayTest.StartPosition = FormStartPosition.Manual;
                displayTest.Location = this.workingMonitor.WorkingArea.Location;
                displayTest.debugMode = true;
                displayTest.Show();

                return;
            }

            if (!this.configurationCheck.All(configuration => configuration == true))
            {
                MessageBox.Show("Not all configuration checked.");
                return;
            }

            VoluntaryMovement display = new VoluntaryMovement();

            display.FormBorderStyle = FormBorderStyle.None;
            display.WindowState = FormWindowState.Maximized;
            display.summitSystem = this.summitSystem;
            display.streamingOption = this.streamingOptions;
            display.StartPosition = FormStartPosition.Manual;
            display.Location = this.workingMonitor.WorkingArea.Location;
            display.debugMode = false;
            display.Show();
        }

        private void ExtensiveSampling_Run_Click(object sender, EventArgs e)
        {
            if (this.summitManager == null || this.summitSystem == null)
            {
                MessageBox.Show("Entering Debug Mode");
                
                ExtensiveSampling displayTest = new ExtensiveSampling();
                
                displayTest.summitSystem = this.summitSystem;
                displayTest.streamingOption = this.streamingOptions;
                displayTest.StartPosition = FormStartPosition.Manual;
                displayTest.Location = this.workingMonitor.WorkingArea.Location;
                displayTest.WindowState = FormWindowState.Maximized;
                displayTest.FormBorderStyle = FormBorderStyle.None;
                displayTest.debugMode = true;
                displayTest.Show();

                return;
            }

            if (!this.configurationCheck.All(configuration => configuration == true))
            {
                MessageBox.Show("Not all configuration checked.");
                return;
            }

            ExtensiveSampling display = new ExtensiveSampling();

            display.FormBorderStyle = FormBorderStyle.None;
            display.WindowState = FormWindowState.Maximized;
            display.summitSystem = this.summitSystem;
            display.streamingOption = this.streamingOptions;
            display.StartPosition = FormStartPosition.Manual;
            display.Location = this.workingMonitor.WorkingArea.Location;
            display.debugMode = false;
            display.Show();

        }

        private void ORCA_Handler()
        {
            try
            {
                int actionCounter = 0;

                do
                {
                    Debug.WriteLine("Start Process Handler");
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

        private void Task_MonitorPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.workingMonitor = screens[this.Task_MonitorPicker.SelectedIndex];
            this.MonitorSizeLabel.Text = this.workingMonitor.WorkingArea.Width.ToString() + " x " + this.workingMonitor.WorkingArea.Height.ToString();
        }
    }
}
