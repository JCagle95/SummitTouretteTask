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

using Medtronic.SummitAPI.Classes;
using Medtronic.TelemetryM;
using Medtronic.NeuroStim.Olympus.DataTypes.PowerManagement;
using Medtronic.NeuroStim.Olympus.DataTypes.DeviceManagement;

namespace SummitTouretteTask
{

    public struct MontageSetting
    {
        public bool[] leadSelection;
        public decimal frameDuration;
        public decimal montageDuration;
        public int samplingRate;
    };

    public partial class Mainpage : Form
    {

        // Montage Settings
        MontageSetting montageSetting;

        // Summit RC+S RDK
        string ORCA_ProjectString;
        SummitManager summitManager;
        SummitSystem summitSystem;

        public Mainpage()
        {
            InitializeComponent();

            // Initialize Required Variables
            montageSetting.leadSelection = new bool[] { true , true, true, true };
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

                // The result of the battery read is automatically logged to the device settings file, 
                // but the application can log its own information if they desire. Example shown-
                this.summitSystem.LogCustomEvent(commandInfo.TxTime, commandInfo.RxTime, "BatteryLevel", batteryLevel);

                // Write the result out to the console
                this.
                Console.WriteLine("Current Battery Level: " + batteryLevel);
            }
            else
            {

            }
        }
    }
}
