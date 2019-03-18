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
        public int samplingRate;
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

        public List<PowerChannel> powerChannels;
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

        // Configuration Check
        bool[] configurationCheck;

        DataManager dataManager;

        public Mainpage()
        {
            InitializeComponent();

            // Initialize Required Variables
            this.dataManager = new DataManager();
            this.configurationCheck = new bool[4] { false, false, false, false };

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
            this.fftSetting.powerChannels = new List<PowerChannel>(4);
            for (int i = 0; i < 4; i++)
            {
                this.fftSetting.powerChannels.Add(new PowerChannel());
            }

            // Initialize Montage Task
            this.montageSetting.leadSelection = new bool[4] { true , true, true, true };

            // Setup ORCA Repository
            this.ORCA_ProjectName.Text = "projecTourette"

        }

        /// <summary>
        /// Closing the Main Program. Dispose any Summit connection while the form is closing.
        /// </summary>
        private void Mainpage_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (summitManager != null)
            {
                summitManager.Dispose();
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
                    montageSetting.samplingRate = 250;
                    break;
                case 1:
                    montageSetting.samplingRate = 500;
                    break;
                case 2:
                    montageSetting.samplingRate = 1000;
                    break;
            }
            display.montageSetting = montageSetting;
            display.FormBorderStyle = FormBorderStyle.None;
            display.WindowState = FormWindowState.Maximized;
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
            } while (discoveredDevices.Count == 0);
            
            // Connect to the INS with default parameters and ORCA annotations
            Debug.WriteLine("Creating Summit Interface.");

            // We can disable ORCA annotations because this is a non-human use INS (see disclaimer)
            // Human-use INS devices ignore the OlympusConnect disableAnnotation flag and always enable annotations.
            // Connect to a device
            ConnectReturn theWarnings;
            APIReturnInfo connectReturn;
            int i = 0;
            do
            {
                connectReturn = summitSystem.StartInsSession(discoveredDevices[0], out theWarnings, true);
                i++;
                if (i > 20)
                {
                    MessageBox.Show("More than 20 initialization error. Please retry later...");
                    return;
                }
            } while (theWarnings.HasFlag(ConnectReturn.InitializationError));

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

                string serialNumber = generalData.DeviceSerialNumber.ToString();
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
                this.sensingSetting.channelMux[channelID, 1] = tdChannel.PlusInput;
                this.sensingSetting.channelMux[channelID, 2] = tdChannel.MinusInput;
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
                        sampleRate[channelID].SelectedIndex = -1;
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
                MessageBox.Show("Reading Sensing Configuration Failed... Return Command Code: " + commandReturn.RejectCode);
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

    }
}
