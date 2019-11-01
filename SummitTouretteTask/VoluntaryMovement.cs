using SummitTouretteTask.Properties;
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
    public partial class VoluntaryMovement : SummitTouretteTask.DataAcquisition
    {
        public VoluntaryMovement()
        {
            InitializeComponent();
            storedFileName = DateTime.Now.ToString("[yyyyMMdd-HH_mm_ss]") + " VoluntaryMovement.dat";
            configurationFileName = DateTime.Now.ToString("[yyyyMMdd-HH_mm_ss] ") + " VoluntaryMovement.config";
            stopWatch = new Stopwatch();
        }
        
        private void VoluntaryMovement_KeyPress(object sender, KeyPressEventArgs key)
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
                storedFileName = DateTime.Now.ToString("[yyyyMMdd-HH_mm_ss]") + " VoluntaryMovement.dat";
                configurationFileName = DateTime.Now.ToString("[yyyyMMdd-HH_mm_ss] ") + " VoluntaryMovement.config";
                trignoServer.storedFileName = DateTime.Now.ToString("[yyyyMMdd-HH_mm_ss]") + " VoluntaryMovement.mdat";
                this.displayThread = new Thread(new ThreadStart(this.InitializeTaskDisplay));
                this.taskCondition = true;
                Task_WriteConfigurations();
                this.displayThread.Start();
            }
        }

        private void InitializeTaskDisplay()
        {
            // Instruction - Prelude Waiting
            for (int i = 10; i > 0; i--)
            {
                DisplayTextSafe(string.Format("Voluntary Movement Task\n\nStarting in\n{0} sec", i));
                Delay(1000);
            }

            AcquisitionStart();
            
            for (int i = 0; i < 8; i++)
            {
                Task_WriteTrigger("REST");
                DisplayTextSafe("+");
                Delay(10000);

                for (int repeat = 0; repeat < 10; repeat++)
                {
                    Task_WriteTrigger("Move Start");
                    SerialPortMessage("Hello World");
                    DisplayImageSafe(Resources.HandOpen, Resources.HandOpen.Width, Resources.HandOpen.Height);
                    Delay(200);
                    DisplayImageSafe(Resources.HandClosing, Resources.HandClosing.Width, Resources.HandClosing.Height);
                    Delay(200);
                    DisplayImageSafe(Resources.HandClose, Resources.HandClose.Width, Resources.HandClose.Height);
                    Delay(200);
                    DisplayImageSafe(Resources.HandClosing, Resources.HandClosing.Width, Resources.HandClosing.Height);
                    Delay(200);
                    DisplayImageSafe(Resources.HandOpen, Resources.HandOpen.Width, Resources.HandOpen.Height);
                    Delay(200);
                }
            }
            
            DisplayTextSafe("Done");

            AcquisitionEnd();
        }
    }
}
