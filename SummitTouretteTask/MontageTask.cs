using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Medtronic.SummitAPI.Classes;

using System.Diagnostics;
using Medtronic.NeuroStim.Olympus.DataTypes.Sensing;
using Medtronic.SummitAPI.Events;

namespace SummitTouretteTask
{
    public partial class MontageTask : Form
    {
        private delegate void SafeCallDelegate(string text);
        private Thread displayThread = null;
        private bool taskCondition = false;
        public DataManager dataManager;

        public MontageSetting montageSetting;
        public SummitSystem summitSystem;

        public string storedFileName;
        public Stopwatch stopWatch;

        public MontageTask()
        {
            InitializeComponent();
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "Montage.dat";
            stopWatch = new Stopwatch();
        }

        private void MontageTask_Load(object sender, EventArgs e)
        {
            this.KeyPreview = true;
            this.KeyPress += new KeyPressEventHandler(MontageTask_KeyPress);
        }

        private bool MontageTask_SensingConfiguration(MontageSetting montageSetting, TdMuxInputs plus, TdMuxInputs minus)
        {
            // Disable Sensing
            summitSystem.WriteSensingState(SenseStates.None, 0x00);

            // Create Time-Domain Sensing Configurations
            List<TimeDomainChannel> TimeDomainChannels = new List<TimeDomainChannel>(4);

            // Power Band Settings (TODO: Can we not configure it and simply use BandDisable?)
            List<PowerChannel> powerChannels = new List<PowerChannel>();

            // Populate the Configurations
            for (int i = 0; i < 4; i++)
            {
                if (montageSetting.leadSelection[i])
                {
                    if (i == 1 || i == 3)
                    {
                        TimeDomainChannels.Add(new TimeDomainChannel(montageSetting.samplingRate, (TdMuxInputs) ((int)plus << 4), (TdMuxInputs) ((int)minus << 4), TdEvokedResponseEnable.Standard, TdLpfStage1.Lpf450Hz, TdLpfStage2.Lpf160Hz, TdHpfs.Hpf0_85Hz));
                    }
                    else
                    {
                        TimeDomainChannels.Add(new TimeDomainChannel(montageSetting.samplingRate, plus, minus, TdEvokedResponseEnable.Standard, TdLpfStage1.Lpf450Hz, TdLpfStage2.Lpf160Hz, TdHpfs.Hpf0_85Hz));
                    }
                }
                else
                {
                    TimeDomainChannels.Add(new TimeDomainChannel(TdSampleRates.Disabled, plus, minus, TdEvokedResponseEnable.Standard, TdLpfStage1.Lpf450Hz, TdLpfStage2.Lpf160Hz, TdHpfs.Hpf0_85Hz));
                }
                powerChannels.Add(new PowerChannel());
            }

            // Standard FFT Settings (TODO: Can we disable it?)
            FftConfiguration fftChannel = new FftConfiguration(FftSizes.Size0064, 500, FftWindowAutoLoads.Hann100, true, FftWeightMultiplies.Shift7, 0, 0);

            // Disable Power Band
            BandEnables theBandEnables = 0;
            
            // Standard MISC Settings
            MiscellaneousSensing miscsettings = new MiscellaneousSensing();
            miscsettings.Bridging = BridgingConfig.None;
            miscsettings.StreamingRate = StreamingFrameRate.Frame50ms;
            miscsettings.LrTriggers = LoopRecordingTriggers.None;

            // Write Configurations (May return error. Check DEBUG Interface)
            APIReturnInfo returnInfoBuffer;
            Debug.WriteLine("Writing sense configuration...");

            returnInfoBuffer = this.summitSystem.WriteSensingTimeDomainChannels(TimeDomainChannels);
            Debug.WriteLine("Write TD Config Status: " + returnInfoBuffer.Descriptor);
            if (returnInfoBuffer.RejectCode != 0) return false;
            
            returnInfoBuffer = this.summitSystem.WriteSensingFftSettings(fftChannel);
            Debug.WriteLine("Write FFT Config Status: " + returnInfoBuffer.Descriptor);
            if (returnInfoBuffer.RejectCode != 0) return false;
            
            returnInfoBuffer = this.summitSystem.WriteSensingPowerChannels(theBandEnables, powerChannels);
            Debug.WriteLine("Write Power Config Status: " + returnInfoBuffer.Descriptor);
            if (returnInfoBuffer.RejectCode != 0) return false;

            returnInfoBuffer = this.summitSystem.WriteSensingMiscSettings(miscsettings);
            Debug.WriteLine("Write Misc Config Status: " + returnInfoBuffer.Descriptor);
            if (returnInfoBuffer.RejectCode != 0) return false;

            returnInfoBuffer = this.summitSystem.WriteSensingAccelSettings(AccelSampleRate.Sample32);
            Debug.WriteLine("Write Accel Config Status: " + returnInfoBuffer.Descriptor);
            if (returnInfoBuffer.RejectCode != 0) return false;

            // ******************* Turn on LFP, FFT, and Power Sensing Components *******************
            returnInfoBuffer = this.summitSystem.WriteSensingState(SenseStates.LfpSense, 0x00);
            Debug.WriteLine("Write Sensing Config Status: " + returnInfoBuffer.Descriptor);
            if (returnInfoBuffer.RejectCode != 0) return false;

            return true;
        }
        
        private void MontageTask_KeyPress(object sender, KeyPressEventArgs key)
        {
            if (key.KeyChar.ToString() == "q")
            {
                this.taskCondition = false;
                this.Close();
            }

            if (key.KeyChar.ToString() == "n")
            {
                dataManager = new DataManager();
                this.displayThread = new Thread(new ThreadStart(InitializeMontageTaskDisplay));
                this.taskCondition = true;
                this.displayThread.Start();
            }
        }

        private void InitializeMontageTaskDisplay()
        {

            // Setup Data Receiving Callbacks
            this.summitSystem.DataReceivedTDHandler += DataReceiver_TimeDomain;

            // Configure 0-1
            if (!MontageTask_SensingConfiguration(this.montageSetting, TdMuxInputs.Mux0, TdMuxInputs.Mux1))
            {
                return;
            }

            for (int i = 3; i > 0; i--)
            {
                DisplayTextSafe(string.Format("Baseline/Resting\n\nStarting in\n{0} sec", i));
                Delay(1000);
            }

            DisplayTextSafe("+");
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "Montage_01.dat";
            Acquisition(true);
            Delay(30000);
            Acquisition(false);

            if (!MontageTask_SensingConfiguration(this.montageSetting, TdMuxInputs.Mux1, TdMuxInputs.Mux2))
            {
                return;
            }

            for (int i = 3; i > 0; i--)
            {
                DisplayTextSafe(string.Format("Baseline/Resting\n\nStarting in\n{0} sec", i));
                Delay(1000);
            }

            DisplayTextSafe("+");
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "Montage_12dat";
            Acquisition(true);
            Delay(30000);
            Acquisition(false);

            if (!MontageTask_SensingConfiguration(this.montageSetting, TdMuxInputs.Mux2, TdMuxInputs.Mux3))
            {
                return;
            }

            for (int i = 3; i > 0; i--)
            {
                DisplayTextSafe(string.Format("Baseline/Resting\n\nStarting in\n{0} sec", i));
                Delay(1000);
            }

            DisplayTextSafe("+");
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "Montage_23dat";
            Acquisition(true);
            Delay(30000);
            Acquisition(false);

            if (!MontageTask_SensingConfiguration(this.montageSetting, TdMuxInputs.Mux0, TdMuxInputs.Mux2))
            {
                return;
            }

            for (int i = 3; i > 0; i--)
            {
                DisplayTextSafe(string.Format("Baseline/Resting\n\nStarting in\n{0} sec", i));
                Delay(1000);
            }

            DisplayTextSafe("+");
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "Montage_02dat";
            Acquisition(true);
            Delay(30000);
            Acquisition(false);

            if (!MontageTask_SensingConfiguration(this.montageSetting, TdMuxInputs.Mux1, TdMuxInputs.Mux3))
            {
                return;
            }

            for (int i = 3; i > 0; i--)
            {
                DisplayTextSafe(string.Format("Baseline/Resting\n\nStarting in\n{0} sec", i));
                Delay(1000);
            }

            DisplayTextSafe("+");
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "Montage_13dat";
            Acquisition(true);
            Delay(30000);
            Acquisition(false);

            if (!MontageTask_SensingConfiguration(this.montageSetting, TdMuxInputs.Mux0, TdMuxInputs.Mux3))
            {
                return;
            }

            for (int i = 3; i > 0; i--)
            {
                DisplayTextSafe(string.Format("Baseline/Resting\n\nStarting in\n{0} sec", i));
                Delay(1000);
            }

            DisplayTextSafe("+");
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "Montage_03dat";
            Acquisition(true);
            Delay(30000);
            Acquisition(false);

            // Setup Data Receiving Callbacks
            this.summitSystem.DataReceivedTDHandler -= DataReceiver_TimeDomain;

            DisplayTextSafe("Done");
        }

        private void DisplayTextSafe(string text)
        {
            if (this.taskCondition)
            {
                if (this.instruction.InvokeRequired)
                {
                    var d = new SafeCallDelegate(DisplayTextSafe);
                    Invoke(d, new object[] { text });
                }
                else
                {
                    this.instruction.Text = text;
                }
            }
        }

        private void Delay(float milliseconds)
        {
            System.Timers.Timer delay = new System.Timers.Timer(milliseconds);
            delay.Elapsed += TimerStop;
            delay.Enabled = true;
            while (delay.Enabled && this.taskCondition) { }
        }
        
        public void Acquisition(bool start)
        {
            // Start Stopwatch
            stopWatch.Start();
            if (start)
                this.summitSystem.WriteSensingEnableStreams(true, false, false, false, false, false, false, false);        // Loop Record Marker Echo
            else
                this.summitSystem.WriteSensingDisableStreams(true);
        }
        
        public void DataReceiver_TimeDomain(object sender, SensingEventTD tdSenseEvent)
        {
            TdSenseStruct dataStruct;
            List<double> eventData;
            int dataLength = 0;

            tdSenseEvent.ChannelSamples.TryGetValue(SenseTimeDomainChannel.Ch0, out eventData);
            if (eventData != null)
            {
                dataStruct.Channel1 = eventData.ToArray();
                dataLength = dataStruct.Channel1.Length;
            }
            else
            {
                dataStruct.Channel1 = new double[0];
            }
            tdSenseEvent.ChannelSamples.TryGetValue(SenseTimeDomainChannel.Ch1, out eventData);
            if (eventData != null)
            {
                dataStruct.Channel2 = eventData.ToArray();
                dataLength = dataStruct.Channel2.Length;
            }
            else
            {
                dataStruct.Channel2 = new double[0];
            }
            tdSenseEvent.ChannelSamples.TryGetValue(SenseTimeDomainChannel.Ch2, out eventData);
            if (eventData != null)
            {
                dataStruct.Channel3 = eventData.ToArray();
                dataLength = dataStruct.Channel3.Length;
            }
            else
            {
                dataStruct.Channel3 = new double[0];
            }
            tdSenseEvent.ChannelSamples.TryGetValue(SenseTimeDomainChannel.Ch3, out eventData);
            if (eventData != null)
            {
                dataStruct.Channel4 = eventData.ToArray();
                dataLength = dataStruct.Channel4.Length;
            }
            else
            {
                dataStruct.Channel4 = new double[0];
            }

            double[] rawData = new double[dataStruct.Channel1.Length + dataStruct.Channel2.Length + dataStruct.Channel3.Length + dataStruct.Channel4.Length];
            dataStruct.Channel1.CopyTo(rawData, 0);
            dataStruct.Channel2.CopyTo(rawData, dataStruct.Channel1.Length);
            dataStruct.Channel3.CopyTo(rawData, dataStruct.Channel1.Length + dataStruct.Channel2.Length);
            dataStruct.Channel4.CopyTo(rawData, dataStruct.Channel1.Length + dataStruct.Channel2.Length + dataStruct.Channel3.Length);

            byte[] rawBytes = new byte[rawData.Length * sizeof(double) + 32];

            // Populate the Headers - Magic Number BML + Global Sequence Byte
            rawBytes[0] = 66;
            rawBytes[1] = 77;
            rawBytes[2] = 76;
            rawBytes[3] = tdSenseEvent.Header.GlobalSequence;

            // Populate the Headers #2 - Type of Data Td  
            rawBytes[4] = 84;
            rawBytes[5] = 100;
            rawBytes[6] = 32;
            rawBytes[7] = 32;

            // Populate the Header #3 - Channels
            rawBytes[8] = (byte)tdSenseEvent.IncludedChannels;
            rawBytes[9] = (byte)dataLength;
            rawBytes[10] = (byte)tdSenseEvent.SampleRate;

            // Populate the Headers #3 - RC+S 64-bit Ticks
            byte[] timeBytes = BitConverter.GetBytes(tdSenseEvent.Header.Timestamp.RealTime.Ticks);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 12, timeBytes.Length);

            // Populate the Headers #4 - PC 64-bit Ticks
            timeBytes = BitConverter.GetBytes(stopWatch.ElapsedMilliseconds);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 20, timeBytes.Length);

            // Copy Data
            Buffer.BlockCopy(rawData, 0, rawBytes, 32, rawData.Length * sizeof(double));

            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
        }

        private void TimerStop(Object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = (System.Timers.Timer)sender;
            timer.Stop();
        }

    }
}
