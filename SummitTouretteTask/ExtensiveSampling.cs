using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SummitTouretteTask
{
    public partial class ExtensiveSampling : DataAcquisition
    {
        public ExtensiveSampling()
        {
            InitializeComponent();
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "ExtensiveSampling.dat";
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
                this.displayThread.Start();
            }
        }

        protected virtual void InitializeTaskDisplay()
        {
            // Instruction - Prelude Waiting
            for (int i = 10; i > 0; i--)
            {
                DisplayTextSafe(string.Format("Extensive Sampling\n\nStarting in\n{0} sec", i));
                Delay(1000);
            }

            AcquisitionStart();

            for (int i = 0; i < 3600*5; i++)
            {
                DisplayTextSafe(string.Format("Sampled for {0} sec", i+1));
                Delay(1000);

                if (!this.taskCondition) break;
            }

            DisplayTextSafe("Done");

            AcquisitionEnd();
        }
    }
}
