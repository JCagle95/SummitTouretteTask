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
            storedFileName = DateTime.Now.ToString("[yyyy-MM-dd-hh-mm-ss]") + "VoluntaryMovement.dat";
            stopWatch = new Stopwatch();
        }
        
        private void VoluntaryMovement_KeyPress(object sender, KeyPressEventArgs key)
        {
            if (key.KeyChar.ToString() == "q")
            {
                this.taskCondition = false;
                this.displayThread.Join();
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
                DisplayTextSafe("+");
                Delay(10000);
                DisplayTextSafe("Move Hand");
                Delay(10000);
            }
            
            DisplayTextSafe("Done");

            AcquisitionEnd();
        }
    }
}
