using Medtronic.NeuroStim.Olympus.DataTypes.Sensing;
using Medtronic.SummitAPI.Classes;
using Medtronic.SummitAPI.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SummitTouretteTask
{
    public partial class DataAcquisition : Form
    {
        public delegate void SafeCallDelegateString(string text);
        public delegate void SafeCallDelegateColor(Color color);
        public delegate void SafeCallDelegateImage(Image img, int width, int height);
        public Thread displayThread = null;
        public bool taskCondition = false;
        public DataManager dataManager;

        public SummitSystem summitSystem;
        public bool[] streamingOption;

        public string storedFileName;
        public Stopwatch stopWatch;

        public bool debugMode;
        
        public DataAcquisition()
        {
            InitializeComponent();
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "DataAccquisitionTest.dat";
            stopWatch = new Stopwatch();
        }
        
        public void DisplayTextSafe(string text)
        {
            if (this.taskCondition)
            {
                if (this.instruction.InvokeRequired)
                {
                    var d = new SafeCallDelegateString(DisplayTextSafe);
                    Invoke(d, new object[] { text });
                }
                else
                {
                    this.instruction.Text = text;
                    this.instruction.BringToFront();
                }
            }
        }
        
        public void DisplayImageSafe(Image img, int width, int height)
        {
            if (this.taskCondition)
            {
                if (this.instruction.InvokeRequired)
                {
                    var d = new SafeCallDelegateImage(DisplayImageSafe);
                    Invoke(d, new object[] { img, width, height });
                }
                else
                {
                    this.Task_Image.Image = img;
                    this.Task_Image.Left = Screen.GetWorkingArea(this.Task_Image).Width / 2 - width / 2;
                    this.Task_Image.Top = Screen.GetWorkingArea(this.Task_Image).Height / 2 - height / 2;
                    this.Task_Image.Width = width;
                    this.Task_Image.Height = height;
                    this.Task_Image.BringToFront();
                }
            }
        }
        
        public void Delay(float milliseconds)
        {
            System.Timers.Timer delay = new System.Timers.Timer(milliseconds);
            delay.Elapsed += TimerStop;
            delay.Enabled = true;
            while (delay.Enabled && this.taskCondition) { }
        }

        public void Delay(float milliseconds, bool forced)
        {
            System.Timers.Timer delay = new System.Timers.Timer(milliseconds);
            delay.Elapsed += TimerStop;
            delay.Enabled = true;
            while (delay.Enabled && (this.taskCondition || forced)) { }
        }

        public void TimerStop(Object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = (System.Timers.Timer)sender;
            timer.Stop();
        }

        public void AcquisitionStart()
        {
            // Setup Callback
            if (!debugMode)
            {
                if (streamingOption[0]) this.summitSystem.DataReceivedTDHandler += DataReceiver_TimeDomain;
                if (streamingOption[1]) this.summitSystem.DataReceivedFFTHandler += DataReceiver_FFT;
                if (streamingOption[2]) this.summitSystem.DataReceivedPowerHandler += DataReceiver_Power;
                if (streamingOption[3] && streamingOption[4]) this.summitSystem.DataReceivedDetectorHandler += DataReceiver_Detector;
                if (streamingOption[5]) this.summitSystem.DataReceivedAccelHandler += DataReceiver_Accelerometer;
            }

            // Start Stopwatch
            stopWatch.Start();

            // Enable Streaming
            if (!debugMode)
            {
                this.summitSystem.WriteSensingEnableStreams(
                    streamingOption[0],         // TD
                    streamingOption[1],         // FFT
                    streamingOption[2],         // Power
                    streamingOption[3],         // Detector State
                    streamingOption[4],         // Adaptive Stim
                    streamingOption[5],         // Accelerometer
                    streamingOption[6],         // Time 
                    streamingOption[7]);        // Loop Record Marker Echo
            }
        }

        public void AcquisitionEnd()
        {
            // Remove Callback
            if (!debugMode)
            {
                if (streamingOption[0]) this.summitSystem.DataReceivedTDHandler -= DataReceiver_TimeDomain;
                if (streamingOption[1]) this.summitSystem.DataReceivedFFTHandler -= DataReceiver_FFT;
                if (streamingOption[2]) this.summitSystem.DataReceivedPowerHandler -= DataReceiver_Power;
                if (streamingOption[3] && streamingOption[4]) this.summitSystem.DataReceivedDetectorHandler -= DataReceiver_Detector;
                if (streamingOption[5]) this.summitSystem.DataReceivedAccelHandler -= DataReceiver_Accelerometer;
            }

            // Disable Streaming
            if (!debugMode) this.summitSystem.WriteSensingDisableStreams(true);
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
            byte[] dataType = Encoding.ASCII.GetBytes("Td  ");
            Buffer.BlockCopy(dataType, 0, rawBytes, 4, dataType.Length);

            // Populate the Header #3 - Channels
            rawBytes[8] = (byte) tdSenseEvent.IncludedChannels;
            rawBytes[9] = (byte) dataLength;
            rawBytes[10] = (byte) tdSenseEvent.SampleRate;

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

        public void DataReceiver_FFT(object sender, SensingEventFFT fftSenseEvent)
        {
            double[] fftData = fftSenseEvent.FftOutput.ToArray();
            byte[] rawBytes = new byte[fftData.Length * sizeof(double) + 32];

            // Populate the Headers - Magic Number BML + Global Sequence Byte
            rawBytes[0] = 66;
            rawBytes[1] = 77;
            rawBytes[2] = 76;
            rawBytes[3] = fftSenseEvent.Header.GlobalSequence;

            // Populate the Headers #2 - Type of Data FFT 
            byte[] dataType = Encoding.ASCII.GetBytes("FFT ");
            Buffer.BlockCopy(dataType, 0, rawBytes, 4, dataType.Length);

            // Populate the Header #3 - Channels
            rawBytes[8] = (byte)fftSenseEvent.Channel;
            rawBytes[9] = (byte)fftData.Length;
            rawBytes[10] = (byte)fftSenseEvent.SampleRate;
            rawBytes[11] = (byte)fftSenseEvent.FftSize;

            // Populate the Headers #3 - RC+S 64-bit Ticks
            byte[] timeBytes = BitConverter.GetBytes(fftSenseEvent.Header.Timestamp.RealTime.Ticks);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 12, timeBytes.Length);

            // Populate the Headers #4 - PC 64-bit Ticks
            timeBytes = BitConverter.GetBytes(stopWatch.ElapsedMilliseconds);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 20, timeBytes.Length);

            // Copy Data
            Buffer.BlockCopy(fftData, 0, rawBytes, 32, fftData.Length * sizeof(double));

            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
        }

        public void DataReceiver_Power(object sender, SensingEventPower powerSenseEvent)
        {
            
            uint[] powerData = powerSenseEvent.Bands.ToArray();
            byte[] rawBytes = new byte[powerData.Length * sizeof(uint) + 32];

            // Populate the Headers - Magic Number BML + Global Sequence Byte
            rawBytes[0] = 66;
            rawBytes[1] = 77;
            rawBytes[2] = 76;
            rawBytes[3] = powerSenseEvent.Header.GlobalSequence;

            // Populate the Headers #2 - Type of Data Pwr  
            byte[] dataType = Encoding.ASCII.GetBytes("Pwr ");
            Buffer.BlockCopy(dataType, 0, rawBytes, 4, dataType.Length);

            // Populate the Header #3 - Channels
            rawBytes[8] = powerSenseEvent.IsPowerChannelOverrange ? (byte) 1 : (byte) 0;
            rawBytes[9] = (byte) powerData.Length;
            rawBytes[10] = (byte) powerSenseEvent.SampleRate;
            rawBytes[11] = (byte) powerSenseEvent.FftSize;

            // Populate the Headers #3 - RC+S 64-bit Ticks
            byte[] timeBytes = BitConverter.GetBytes(powerSenseEvent.Header.Timestamp.RealTime.Ticks);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 12, timeBytes.Length);

            // Populate the Headers #4 - PC 64-bit Ticks
            timeBytes = BitConverter.GetBytes(stopWatch.ElapsedMilliseconds);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 20, timeBytes.Length);

            // Copy Data
            Buffer.BlockCopy(powerData, 0, rawBytes, 32, powerData.Length * sizeof(uint));

            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);
            
        }
        
        public void DataReceiver_Detector(object sender,  AdaptiveDetectEvent detectorEvent)
        {
            byte[] rawBytes = new byte[82];

            // Populate the Headers - Magic Number BML + Global Sequence Byte
            rawBytes[0] = 66;
            rawBytes[1] = 77;
            rawBytes[2] = 76;
            rawBytes[3] = detectorEvent.Header.GlobalSequence;

            // Populate the Headers #2 - Type of Data Det  
            byte[] dataType = Encoding.ASCII.GetBytes("Det ");
            Buffer.BlockCopy(dataType, 0, rawBytes, 4, dataType.Length);

            // Populate the Header #3 - Channels
            rawBytes[8] = (byte)detectorEvent.CurrentAdaptiveState;
            rawBytes[9] = (byte)detectorEvent.PreviousAdaptiveState;
            rawBytes[10] = (byte)detectorEvent.SensingStatus;
            rawBytes[11] = (byte)detectorEvent.StateTime;

            // Populate the Headers #3 - RC+S 64-bit Ticks
            byte[] timeBytes = BitConverter.GetBytes(detectorEvent.Header.Timestamp.RealTime.Ticks);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 12, timeBytes.Length);

            // Populate the Headers #4 - PC 64-bit Ticks
            timeBytes = BitConverter.GetBytes(stopWatch.ElapsedMilliseconds);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 20, timeBytes.Length);

            // Manually Putting Detector Values (Current Program Amplitudes)
            Buffer.BlockCopy(detectorEvent.CurrentProgramAmplitudesInMilliamps, 0, rawBytes, 32, detectorEvent.CurrentProgramAmplitudesInMilliamps.Length * sizeof(double));

            // Stimulation Information in Hertz
            Buffer.BlockCopy(BitConverter.GetBytes(detectorEvent.StimRateInHz), 0, rawBytes, 64, sizeof(double));

            // How long has the detector been in Stimulation Mode
            Buffer.BlockCopy(BitConverter.GetBytes(detectorEvent.StateTime), 0, rawBytes, 72, sizeof(uint));
            
            // How many detections occurred
            Buffer.BlockCopy(BitConverter.GetBytes(detectorEvent.StateEntryCount), 0, rawBytes, 76, sizeof(uint));

            // Detector Status
            rawBytes[80] = (byte)detectorEvent.Ld0DetectionStatus;
            rawBytes[81] = (byte)detectorEvent.Ld1DetectionStatus;

            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);

        }

        public void DataReceiver_Accelerometer(object sender, SensingEventAccel accelEvent)
        {
            double[] xData = accelEvent.XSamples.ToArray();
            double[] yData = accelEvent.YSamples.ToArray();
            double[] zData = accelEvent.ZSamples.ToArray();
            byte[] rawBytes = new byte[(xData.Length + yData.Length + zData.Length) * sizeof(double) + 32];

            // Populate the Headers - Magic Number BML + Global Sequence Byte
            rawBytes[0] = 66;
            rawBytes[1] = 77;
            rawBytes[2] = 76;
            rawBytes[3] = accelEvent.Header.GlobalSequence;

            // Populate the Headers #2 - Type of Data Det  
            byte[] dataType = Encoding.ASCII.GetBytes("Acc ");
            Buffer.BlockCopy(dataType, 0, rawBytes, 4, dataType.Length);

            // Populate the Header #3 - Channels
            rawBytes[8] = (byte)xData.Length;
            rawBytes[9] = (byte)yData.Length;
            rawBytes[10] = (byte)zData.Length;
            rawBytes[11] = (byte)accelEvent.SampleRate;

            // Populate the Headers #3 - RC+S 64-bit Ticks
            byte[] timeBytes = BitConverter.GetBytes(accelEvent.Header.Timestamp.RealTime.Ticks);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 12, timeBytes.Length);

            // Populate the Headers #4 - PC 64-bit Ticks
            timeBytes = BitConverter.GetBytes(stopWatch.ElapsedMilliseconds);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 20, timeBytes.Length);

            // Write Data
            Buffer.BlockCopy(xData, 0, rawBytes, 32, xData.Length * sizeof(double));
            Buffer.BlockCopy(yData, 0, rawBytes, 32 + xData.Length, yData.Length * sizeof(double));
            Buffer.BlockCopy(zData, 0, rawBytes, 32 + xData.Length + yData.Length, zData.Length * sizeof(double));
            
            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);

        }

        public void Task_WriteTrigger(object sender, SensingEventAccel accelEvent)
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
            
            // Populate the Headers #4 - PC 64-bit Ticks
            byte[] timeBytes = BitConverter.GetBytes(stopWatch.ElapsedMilliseconds);
            Buffer.BlockCopy(timeBytes, 0, rawBytes, 20, timeBytes.Length);
            
            dataManager.WriteBinary_ThreadSafe(storedFileName, rawBytes);

        }
    }
}
