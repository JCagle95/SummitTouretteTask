using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using System.Diagnostics;

namespace SummitTouretteTask
{
    public partial class MontageTask : Form
    {
        private delegate void SafeCallDelegate(string text);
        private Thread displayThread = null;
        private bool taskCondition = false;

        public MontageSetting montageSetting;

        public MontageTask()
        {
            InitializeComponent();
        }

        private void MontageTask_Load(object sender, EventArgs e)
        {
            this.KeyPreview = true;
            this.KeyPress += new KeyPressEventHandler(MontageTask_KeyPress);
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
                this.displayThread = new Thread(new ThreadStart(InitializeMontageTaskDisplay));
                this.taskCondition = true;
                this.displayThread.Start();
            }
        }

        private void InitializeMontageTaskDisplay()
        {
            // Instruction - Prelude Waiting
            for (int i = 10; i > 0; i--)
            {
                DisplayTextSafe(string.Format("Baseline/Resting\n\nStarting in\n{0} sec", i));
                Delay(1000);
            }

            DisplayTextSafe("+");
            Acquisition(30000);

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

        private void Acquisition(float milliseconds)
        {
            System.Timers.Timer duration = new System.Timers.Timer(milliseconds);
            duration.Elapsed += TimerStop;
            duration.Enabled = true;
            while (duration.Enabled && this.taskCondition)
            {

            }
        }

        private void TimerStop(Object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Timers.Timer timer = (System.Timers.Timer)sender;
            timer.Stop();
        }

    }
}
