using SummitTouretteTask.Properties;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace SummitTouretteTask
{
    public partial class StimAndRecord : DataAcquisition
    {
        public StimAndRecord()
        {
            InitializeComponent();
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "StimAndRecord.dat";
            configurationFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "StimAndRecord.config";
            stopWatch = new Stopwatch();
        }


        protected virtual void KeyPressFunction(object sender, KeyPressEventArgs key)
        {
            if (key.KeyChar.ToString() == "q")
            {
                this.taskCondition = false;
                if (this.displayThread != null) this.displayThread.Join();
                this.Close();
            }

            if (key.KeyChar.ToString() == "n")
            {
                dataManager = new DataManager();
                this.displayThread = new Thread(new ThreadStart(this.InitializeTaskDisplay));
                this.taskCondition = true;
                Task_WriteConfigurations();
                this.displayThread.Start();
            }
        }

        protected virtual void InitializeTaskDisplay()
        {
            // Start Stimulation
            Stimulation_State(true);

            // Instruction - Prelude Waiting
            for (int i = 10; i > 0; i--)
            {

                DisplayTextSafe(string.Format("Extensive Sampling\n\nStarting in\n{0} sec", i));
                Delay(1000);
            }


            // Start Recording
            DisplayTextSafe("Task Ongoing");

            AcquisitionStart();
            Delay(30000);

            double? newStimAmplitude;
            for (int i = 0; i < 3; i++)
            {
                if (Stimulation_AmplitudeChange(0, -3, out newStimAmplitude))
                {
                    Task_WriteTrigger("RedAmp03");
                    DisplayTextSafe("No Stimulation");
                }
                Delay(30000);

                if (Stimulation_AmplitudeChange(0, 3, out newStimAmplitude))
                {
                    Task_WriteTrigger("IncAmp03");
                    DisplayTextSafe("Stimulation");
                }
                Delay(30000);
            }
            /*
            if (Stimulation_AmplitudeChange(0, -1, out newStimAmplitude) && Stimulation_AmplitudeChange(1, -1, out newStimAmplitude))
            {
                Task_WriteTrigger("RedAmp01");
                DisplayTextSafe("No Stimulation");
            }
            Delay(30000);

            if (Stimulation_AmplitudeChange(0, 1, out newStimAmplitude) && Stimulation_AmplitudeChange(1, 1, out newStimAmplitude))
            {
                Task_WriteTrigger("IncAmp01");
                DisplayTextSafe("Stimulation");
            }
            Delay(30000);

            if (Stimulation_AmplitudeChange(0, -1, out newStimAmplitude) && Stimulation_AmplitudeChange(1, -1, out newStimAmplitude))
            {
                Task_WriteTrigger("RedAmp01");
                DisplayTextSafe("No Stimulation");
            }
            Delay(30000);

            if (Stimulation_AmplitudeChange(0, 1, out newStimAmplitude) && Stimulation_AmplitudeChange(1, 1, out newStimAmplitude))
            {
                Task_WriteTrigger("IncAmp01");
                DisplayTextSafe("Stimulation");
            }
            Delay(30000);

            if (Stimulation_AmplitudeChange(0, -1, out newStimAmplitude) && Stimulation_AmplitudeChange(1, -1, out newStimAmplitude))
            {
                Task_WriteTrigger("RedAmp01");
                DisplayTextSafe("No Stimulation");
            }
            Delay(30000);

            if (Stimulation_AmplitudeChange(0, 1, out newStimAmplitude) && Stimulation_AmplitudeChange(1, 1, out newStimAmplitude))
            {
                Task_WriteTrigger("IncAmp01");
                DisplayTextSafe("Stimulation");
            }
            Delay(30000);
            Stimulation_State(false);
            */
            DisplayTextSafe("Done");
            AcquisitionEnd();
        }
    }
}