namespace SummitTouretteTask
{
    partial class Mainpage
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Mainpage));
            this.Mainpage_TaskTab = new System.Windows.Forms.TabControl();
            this.Welcome_Tab = new System.Windows.Forms.TabPage();
            this.Summit_SenseStatus = new System.Windows.Forms.Label();
            this.Summit_StimStatus = new System.Windows.Forms.Label();
            this.Summit_UntilEOS = new System.Windows.Forms.Label();
            this.Summit_SerialNumber = new System.Windows.Forms.Label();
            this.Summit_BatteryLevel = new System.Windows.Forms.Label();
            this.Summit_GetStatusButton = new System.Windows.Forms.Button();
            this.Summit_DiscoverRCS = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.ORCA_Submit = new System.Windows.Forms.Button();
            this.ORCA_ProjectName = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.Summit_Connect = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.Montage_Tab = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.Montage_FrameDuration = new System.Windows.Forms.NumericUpDown();
            this.Montage_SamplingRate = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.Montage_Duration = new System.Windows.Forms.NumericUpDown();
            this.LeadSelection_Panel = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.RightCh2_Checkbox = new System.Windows.Forms.CheckBox();
            this.RightCh1_Checkbox = new System.Windows.Forms.CheckBox();
            this.LeftCh2_Checkbox = new System.Windows.Forms.CheckBox();
            this.LeftCh1_Checkbox = new System.Windows.Forms.CheckBox();
            this.Montage_Run = new System.Windows.Forms.Button();
            this.ConfigSensing_Tab = new System.Windows.Forms.TabPage();
            this.Task_Tab = new System.Windows.Forms.TabPage();
            this.Sensing_SamplingRate = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.Sensing_GetStatusButton = new System.Windows.Forms.Button();
            this.Mainpage_TaskTab.SuspendLayout();
            this.Welcome_Tab.SuspendLayout();
            this.Montage_Tab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Montage_FrameDuration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.Montage_Duration)).BeginInit();
            this.LeadSelection_Panel.SuspendLayout();
            this.ConfigSensing_Tab.SuspendLayout();
            this.SuspendLayout();
            // 
            // Mainpage_TaskTab
            // 
            this.Mainpage_TaskTab.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Mainpage_TaskTab.Controls.Add(this.Welcome_Tab);
            this.Mainpage_TaskTab.Controls.Add(this.ConfigSensing_Tab);
            this.Mainpage_TaskTab.Controls.Add(this.Montage_Tab);
            this.Mainpage_TaskTab.Controls.Add(this.Task_Tab);
            this.Mainpage_TaskTab.Font = new System.Drawing.Font("Segoe UI Semibold", 13.875F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Mainpage_TaskTab.Location = new System.Drawing.Point(80, 45);
            this.Mainpage_TaskTab.Name = "Mainpage_TaskTab";
            this.Mainpage_TaskTab.Padding = new System.Drawing.Point(30, 3);
            this.Mainpage_TaskTab.SelectedIndex = 0;
            this.Mainpage_TaskTab.Size = new System.Drawing.Size(1760, 900);
            this.Mainpage_TaskTab.TabIndex = 0;
            // 
            // Welcome_Tab
            // 
            this.Welcome_Tab.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.Welcome_Tab.BackColor = System.Drawing.Color.LightGray;
            this.Welcome_Tab.Controls.Add(this.Summit_SenseStatus);
            this.Welcome_Tab.Controls.Add(this.Summit_StimStatus);
            this.Welcome_Tab.Controls.Add(this.Summit_UntilEOS);
            this.Welcome_Tab.Controls.Add(this.Summit_SerialNumber);
            this.Welcome_Tab.Controls.Add(this.Summit_BatteryLevel);
            this.Welcome_Tab.Controls.Add(this.Summit_GetStatusButton);
            this.Welcome_Tab.Controls.Add(this.Summit_DiscoverRCS);
            this.Welcome_Tab.Controls.Add(this.label8);
            this.Welcome_Tab.Controls.Add(this.ORCA_Submit);
            this.Welcome_Tab.Controls.Add(this.ORCA_ProjectName);
            this.Welcome_Tab.Controls.Add(this.label7);
            this.Welcome_Tab.Controls.Add(this.Summit_Connect);
            this.Welcome_Tab.Controls.Add(this.label6);
            this.Welcome_Tab.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Welcome_Tab.Location = new System.Drawing.Point(8, 64);
            this.Welcome_Tab.Name = "Welcome_Tab";
            this.Welcome_Tab.Padding = new System.Windows.Forms.Padding(3);
            this.Welcome_Tab.Size = new System.Drawing.Size(1744, 828);
            this.Welcome_Tab.TabIndex = 0;
            this.Welcome_Tab.Text = "Welcome";
            this.Welcome_Tab.UseVisualStyleBackColor = true;
            // 
            // Summit_SenseStatus
            // 
            this.Summit_SenseStatus.AutoSize = true;
            this.Summit_SenseStatus.BackColor = System.Drawing.Color.Transparent;
            this.Summit_SenseStatus.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.Summit_SenseStatus.Location = new System.Drawing.Point(74, 608);
            this.Summit_SenseStatus.Name = "Summit_SenseStatus";
            this.Summit_SenseStatus.Size = new System.Drawing.Size(240, 45);
            this.Summit_SenseStatus.TabIndex = 15;
            this.Summit_SenseStatus.Text = "Sensing Status:";
            // 
            // Summit_StimStatus
            // 
            this.Summit_StimStatus.AutoSize = true;
            this.Summit_StimStatus.BackColor = System.Drawing.Color.Transparent;
            this.Summit_StimStatus.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.Summit_StimStatus.Location = new System.Drawing.Point(74, 544);
            this.Summit_StimStatus.Name = "Summit_StimStatus";
            this.Summit_StimStatus.Size = new System.Drawing.Size(293, 45);
            this.Summit_StimStatus.TabIndex = 14;
            this.Summit_StimStatus.Text = "Stimulation Status:";
            // 
            // Summit_UntilEOS
            // 
            this.Summit_UntilEOS.AutoSize = true;
            this.Summit_UntilEOS.BackColor = System.Drawing.Color.Transparent;
            this.Summit_UntilEOS.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.Summit_UntilEOS.Location = new System.Drawing.Point(74, 479);
            this.Summit_UntilEOS.Name = "Summit_UntilEOS";
            this.Summit_UntilEOS.Size = new System.Drawing.Size(240, 45);
            this.Summit_UntilEOS.TabIndex = 13;
            this.Summit_UntilEOS.Text = "Days until EOS:";
            // 
            // Summit_SerialNumber
            // 
            this.Summit_SerialNumber.AutoSize = true;
            this.Summit_SerialNumber.BackColor = System.Drawing.Color.Transparent;
            this.Summit_SerialNumber.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.Summit_SerialNumber.Location = new System.Drawing.Point(74, 355);
            this.Summit_SerialNumber.Name = "Summit_SerialNumber";
            this.Summit_SerialNumber.Size = new System.Drawing.Size(237, 45);
            this.Summit_SerialNumber.TabIndex = 12;
            this.Summit_SerialNumber.Text = "Serial Number:";
            // 
            // Summit_BatteryLevel
            // 
            this.Summit_BatteryLevel.AutoSize = true;
            this.Summit_BatteryLevel.BackColor = System.Drawing.Color.Transparent;
            this.Summit_BatteryLevel.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.Summit_BatteryLevel.Location = new System.Drawing.Point(74, 419);
            this.Summit_BatteryLevel.Name = "Summit_BatteryLevel";
            this.Summit_BatteryLevel.Size = new System.Drawing.Size(217, 45);
            this.Summit_BatteryLevel.TabIndex = 11;
            this.Summit_BatteryLevel.Text = "Battery Level:";
            // 
            // Summit_GetStatusButton
            // 
            this.Summit_GetStatusButton.Enabled = false;
            this.Summit_GetStatusButton.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.Summit_GetStatusButton.Location = new System.Drawing.Point(82, 256);
            this.Summit_GetStatusButton.Name = "Summit_GetStatusButton";
            this.Summit_GetStatusButton.Size = new System.Drawing.Size(214, 59);
            this.Summit_GetStatusButton.TabIndex = 10;
            this.Summit_GetStatusButton.Text = "Get Status";
            this.Summit_GetStatusButton.UseVisualStyleBackColor = true;
            this.Summit_GetStatusButton.Click += new System.EventHandler(this.Summit_GetStatusButton_Click);
            // 
            // Summit_DiscoverRCS
            // 
            this.Summit_DiscoverRCS.Enabled = false;
            this.Summit_DiscoverRCS.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.Summit_DiscoverRCS.Location = new System.Drawing.Point(1461, 157);
            this.Summit_DiscoverRCS.Name = "Summit_DiscoverRCS";
            this.Summit_DiscoverRCS.Size = new System.Drawing.Size(214, 59);
            this.Summit_DiscoverRCS.TabIndex = 8;
            this.Summit_DiscoverRCS.Text = "Discover";
            this.Summit_DiscoverRCS.UseVisualStyleBackColor = true;
            this.Summit_DiscoverRCS.Click += new System.EventHandler(this.Summit_DiscoverRCS_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Font = new System.Drawing.Font("Segoe UI Semibold", 16F, System.Drawing.FontStyle.Bold);
            this.label8.Location = new System.Drawing.Point(920, 157);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(333, 59);
            this.label8.TabIndex = 7;
            this.label8.Text = "Discover RC+S: ";
            // 
            // ORCA_Submit
            // 
            this.ORCA_Submit.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.ORCA_Submit.Location = new System.Drawing.Point(1462, 70);
            this.ORCA_Submit.Name = "ORCA_Submit";
            this.ORCA_Submit.Size = new System.Drawing.Size(214, 57);
            this.ORCA_Submit.TabIndex = 6;
            this.ORCA_Submit.Text = "Lock";
            this.ORCA_Submit.UseVisualStyleBackColor = true;
            this.ORCA_Submit.Click += new System.EventHandler(this.ORCA_Submit_Click);
            // 
            // ORCA_ProjectName
            // 
            this.ORCA_ProjectName.Font = new System.Drawing.Font("Segoe UI Semibold", 14F, System.Drawing.FontStyle.Bold);
            this.ORCA_ProjectName.ForeColor = System.Drawing.Color.OrangeRed;
            this.ORCA_ProjectName.Location = new System.Drawing.Point(542, 69);
            this.ORCA_ProjectName.Name = "ORCA_ProjectName";
            this.ORCA_ProjectName.Size = new System.Drawing.Size(867, 57);
            this.ORCA_ProjectName.TabIndex = 5;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.BackColor = System.Drawing.Color.Transparent;
            this.label7.Font = new System.Drawing.Font("Segoe UI Semibold", 16F, System.Drawing.FontStyle.Bold);
            this.label7.Location = new System.Drawing.Point(72, 62);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(427, 59);
            this.label7.TabIndex = 4;
            this.label7.Text = "ORCA Project Name:";
            // 
            // Summit_Connect
            // 
            this.Summit_Connect.Enabled = false;
            this.Summit_Connect.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.Summit_Connect.Location = new System.Drawing.Point(613, 157);
            this.Summit_Connect.Name = "Summit_Connect";
            this.Summit_Connect.Size = new System.Drawing.Size(214, 59);
            this.Summit_Connect.TabIndex = 2;
            this.Summit_Connect.Text = "Connect";
            this.Summit_Connect.UseVisualStyleBackColor = true;
            this.Summit_Connect.Click += new System.EventHandler(this.Summit_Connect_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Font = new System.Drawing.Font("Segoe UI Semibold", 16F, System.Drawing.FontStyle.Bold);
            this.label6.Location = new System.Drawing.Point(72, 157);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(512, 59);
            this.label6.TabIndex = 1;
            this.label6.Text = "Summit System Connect:";
            // 
            // Montage_Tab
            // 
            this.Montage_Tab.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Montage_Tab.Controls.Add(this.label5);
            this.Montage_Tab.Controls.Add(this.Montage_FrameDuration);
            this.Montage_Tab.Controls.Add(this.Montage_SamplingRate);
            this.Montage_Tab.Controls.Add(this.label4);
            this.Montage_Tab.Controls.Add(this.label3);
            this.Montage_Tab.Controls.Add(this.Montage_Duration);
            this.Montage_Tab.Controls.Add(this.LeadSelection_Panel);
            this.Montage_Tab.Controls.Add(this.Montage_Run);
            this.Montage_Tab.Location = new System.Drawing.Point(8, 64);
            this.Montage_Tab.Name = "Montage_Tab";
            this.Montage_Tab.Padding = new System.Windows.Forms.Padding(3);
            this.Montage_Tab.Size = new System.Drawing.Size(1744, 828);
            this.Montage_Tab.TabIndex = 1;
            this.Montage_Tab.Text = "Montage Check";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(67, 631);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(547, 50);
            this.label5.TabIndex = 12;
            this.label5.Text = "Frame Duration (milliseconds): ";
            // 
            // Montage_FrameDuration
            // 
            this.Montage_FrameDuration.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.Montage_FrameDuration.Location = new System.Drawing.Point(625, 629);
            this.Montage_FrameDuration.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.Montage_FrameDuration.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.Montage_FrameDuration.Name = "Montage_FrameDuration";
            this.Montage_FrameDuration.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Montage_FrameDuration.Size = new System.Drawing.Size(152, 57);
            this.Montage_FrameDuration.TabIndex = 11;
            this.Montage_FrameDuration.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Montage_FrameDuration.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            // 
            // Montage_SamplingRate
            // 
            this.Montage_SamplingRate.BackColor = System.Drawing.SystemColors.Window;
            this.Montage_SamplingRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Montage_SamplingRate.FormattingEnabled = true;
            this.Montage_SamplingRate.Items.AddRange(new object[] {
            "250 Hz",
            "500 Hz",
            "1000 Hz"});
            this.Montage_SamplingRate.Location = new System.Drawing.Point(1448, 524);
            this.Montage_SamplingRate.Name = "Montage_SamplingRate";
            this.Montage_SamplingRate.Size = new System.Drawing.Size(191, 58);
            this.Montage_SamplingRate.TabIndex = 10;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(940, 527);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(362, 50);
            this.label4.TabIndex = 9;
            this.label4.Text = "Sampling Rate (Hz): ";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(67, 529);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(521, 50);
            this.label3.TabIndex = 7;
            this.label3.Text = "Montage Duration (seconds): ";
            // 
            // Montage_Duration
            // 
            this.Montage_Duration.Location = new System.Drawing.Point(625, 527);
            this.Montage_Duration.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.Montage_Duration.Name = "Montage_Duration";
            this.Montage_Duration.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.Montage_Duration.Size = new System.Drawing.Size(152, 57);
            this.Montage_Duration.TabIndex = 6;
            this.Montage_Duration.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.Montage_Duration.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // LeadSelection_Panel
            // 
            this.LeadSelection_Panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LeadSelection_Panel.BackColor = System.Drawing.Color.White;
            this.LeadSelection_Panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.LeadSelection_Panel.Controls.Add(this.label2);
            this.LeadSelection_Panel.Controls.Add(this.label1);
            this.LeadSelection_Panel.Controls.Add(this.RightCh2_Checkbox);
            this.LeadSelection_Panel.Controls.Add(this.RightCh1_Checkbox);
            this.LeadSelection_Panel.Controls.Add(this.LeftCh2_Checkbox);
            this.LeadSelection_Panel.Controls.Add(this.LeftCh1_Checkbox);
            this.LeadSelection_Panel.Location = new System.Drawing.Point(76, 49);
            this.LeadSelection_Panel.Name = "LeadSelection_Panel";
            this.LeadSelection_Panel.Size = new System.Drawing.Size(1563, 398);
            this.LeadSelection_Panel.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(1058, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(326, 50);
            this.label2.TabIndex = 6;
            this.label2.Text = "Right Hemisphere";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(207, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(301, 50);
            this.label1.TabIndex = 5;
            this.label1.Text = "Left Hemisphere";
            // 
            // RightCh2_Checkbox
            // 
            this.RightCh2_Checkbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.RightCh2_Checkbox.BackgroundImage = global::SummitTouretteTask.Properties.Resources.ECoG;
            this.RightCh2_Checkbox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.RightCh2_Checkbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.RightCh2_Checkbox.Checked = true;
            this.RightCh2_Checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.RightCh2_Checkbox.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.RightCh2_Checkbox.FlatAppearance.CheckedBackColor = System.Drawing.Color.Red;
            this.RightCh2_Checkbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RightCh2_Checkbox.ForeColor = System.Drawing.Color.Red;
            this.RightCh2_Checkbox.Location = new System.Drawing.Point(894, 153);
            this.RightCh2_Checkbox.Name = "RightCh2_Checkbox";
            this.RightCh2_Checkbox.Size = new System.Drawing.Size(641, 78);
            this.RightCh2_Checkbox.TabIndex = 2;
            this.RightCh2_Checkbox.UseVisualStyleBackColor = true;
            this.RightCh2_Checkbox.CheckedChanged += new System.EventHandler(this.RightCh2_Checkbox_CheckedChanged);
            // 
            // RightCh1_Checkbox
            // 
            this.RightCh1_Checkbox.BackgroundImage = global::SummitTouretteTask.Properties.Resources.DBSLead;
            this.RightCh1_Checkbox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.RightCh1_Checkbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.RightCh1_Checkbox.Checked = true;
            this.RightCh1_Checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.RightCh1_Checkbox.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.RightCh1_Checkbox.FlatAppearance.CheckedBackColor = System.Drawing.Color.Red;
            this.RightCh1_Checkbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.RightCh1_Checkbox.ForeColor = System.Drawing.Color.Red;
            this.RightCh1_Checkbox.Location = new System.Drawing.Point(894, 303);
            this.RightCh1_Checkbox.Name = "RightCh1_Checkbox";
            this.RightCh1_Checkbox.Size = new System.Drawing.Size(641, 54);
            this.RightCh1_Checkbox.TabIndex = 4;
            this.RightCh1_Checkbox.UseVisualStyleBackColor = true;
            this.RightCh1_Checkbox.CheckedChanged += new System.EventHandler(this.RightCh1_Checkbox_CheckedChanged);
            // 
            // LeftCh2_Checkbox
            // 
            this.LeftCh2_Checkbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LeftCh2_Checkbox.BackgroundImage = global::SummitTouretteTask.Properties.Resources.ECoG;
            this.LeftCh2_Checkbox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.LeftCh2_Checkbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.LeftCh2_Checkbox.Checked = true;
            this.LeftCh2_Checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.LeftCh2_Checkbox.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.LeftCh2_Checkbox.FlatAppearance.CheckedBackColor = System.Drawing.Color.Red;
            this.LeftCh2_Checkbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.LeftCh2_Checkbox.ForeColor = System.Drawing.Color.Red;
            this.LeftCh2_Checkbox.Location = new System.Drawing.Point(38, 153);
            this.LeftCh2_Checkbox.Name = "LeftCh2_Checkbox";
            this.LeftCh2_Checkbox.Size = new System.Drawing.Size(641, 78);
            this.LeftCh2_Checkbox.TabIndex = 1;
            this.LeftCh2_Checkbox.UseVisualStyleBackColor = true;
            this.LeftCh2_Checkbox.CheckedChanged += new System.EventHandler(this.LeftCh2_Checkbox_CheckedChanged);
            // 
            // LeftCh1_Checkbox
            // 
            this.LeftCh1_Checkbox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LeftCh1_Checkbox.BackgroundImage = global::SummitTouretteTask.Properties.Resources.DBSLead;
            this.LeftCh1_Checkbox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.LeftCh1_Checkbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.LeftCh1_Checkbox.Checked = true;
            this.LeftCh1_Checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.LeftCh1_Checkbox.FlatAppearance.BorderColor = System.Drawing.Color.Red;
            this.LeftCh1_Checkbox.FlatAppearance.CheckedBackColor = System.Drawing.Color.Transparent;
            this.LeftCh1_Checkbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.LeftCh1_Checkbox.ForeColor = System.Drawing.Color.Red;
            this.LeftCh1_Checkbox.Location = new System.Drawing.Point(38, 303);
            this.LeftCh1_Checkbox.Name = "LeftCh1_Checkbox";
            this.LeftCh1_Checkbox.Size = new System.Drawing.Size(641, 54);
            this.LeftCh1_Checkbox.TabIndex = 3;
            this.LeftCh1_Checkbox.UseVisualStyleBackColor = true;
            this.LeftCh1_Checkbox.CheckedChanged += new System.EventHandler(this.LeftCh1_Checkbox_CheckedChanged);
            // 
            // Montage_Run
            // 
            this.Montage_Run.BackColor = System.Drawing.Color.FloralWhite;
            this.Montage_Run.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Montage_Run.Location = new System.Drawing.Point(761, 730);
            this.Montage_Run.Name = "Montage_Run";
            this.Montage_Run.Size = new System.Drawing.Size(200, 62);
            this.Montage_Run.TabIndex = 0;
            this.Montage_Run.Text = "Run";
            this.Montage_Run.UseVisualStyleBackColor = false;
            this.Montage_Run.Click += new System.EventHandler(this.Montage_Run_Click);
            // 
            // ConfigSensing_Tab
            // 
            this.ConfigSensing_Tab.Controls.Add(this.Sensing_GetStatusButton);
            this.ConfigSensing_Tab.Controls.Add(this.Sensing_SamplingRate);
            this.ConfigSensing_Tab.Controls.Add(this.label9);
            this.ConfigSensing_Tab.Location = new System.Drawing.Point(8, 64);
            this.ConfigSensing_Tab.Name = "ConfigSensing_Tab";
            this.ConfigSensing_Tab.Padding = new System.Windows.Forms.Padding(3);
            this.ConfigSensing_Tab.Size = new System.Drawing.Size(1744, 828);
            this.ConfigSensing_Tab.TabIndex = 2;
            this.ConfigSensing_Tab.Text = "Sensing Configuration";
            this.ConfigSensing_Tab.UseVisualStyleBackColor = true;
            // 
            // Task_Tab
            // 
            this.Task_Tab.Location = new System.Drawing.Point(8, 64);
            this.Task_Tab.Name = "Task_Tab";
            this.Task_Tab.Size = new System.Drawing.Size(1744, 828);
            this.Task_Tab.TabIndex = 3;
            this.Task_Tab.Text = "Task Selection";
            this.Task_Tab.UseVisualStyleBackColor = true;
            // 
            // Sensing_SamplingRate
            // 
            this.Sensing_SamplingRate.BackColor = System.Drawing.SystemColors.Window;
            this.Sensing_SamplingRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Sensing_SamplingRate.FormattingEnabled = true;
            this.Sensing_SamplingRate.Items.AddRange(new object[] {
            "250 Hz",
            "500 Hz",
            "1000 Hz"});
            this.Sensing_SamplingRate.Location = new System.Drawing.Point(436, 239);
            this.Sensing_SamplingRate.Name = "Sensing_SamplingRate";
            this.Sensing_SamplingRate.Size = new System.Drawing.Size(191, 58);
            this.Sensing_SamplingRate.TabIndex = 12;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(62, 241);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(362, 50);
            this.label9.TabIndex = 11;
            this.label9.Text = "Sampling Rate (Hz): ";
            // 
            // Sensing_GetStatusButton
            // 
            this.Sensing_GetStatusButton.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold);
            this.Sensing_GetStatusButton.Location = new System.Drawing.Point(71, 46);
            this.Sensing_GetStatusButton.Name = "Sensing_GetStatusButton";
            this.Sensing_GetStatusButton.Size = new System.Drawing.Size(450, 59);
            this.Sensing_GetStatusButton.TabIndex = 13;
            this.Sensing_GetStatusButton.Text = "Get Sensing Configuration";
            this.Sensing_GetStatusButton.UseVisualStyleBackColor = true;
            this.Sensing_GetStatusButton.Click += new System.EventHandler(this.Sensing_GetStatusButton_Click);
            // 
            // Mainpage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1894, 1009);
            this.Controls.Add(this.Mainpage_TaskTab);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Mainpage";
            this.Text = "Summit Tourette Project";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Mainpage_FormClosing);
            this.Mainpage_TaskTab.ResumeLayout(false);
            this.Welcome_Tab.ResumeLayout(false);
            this.Welcome_Tab.PerformLayout();
            this.Montage_Tab.ResumeLayout(false);
            this.Montage_Tab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Montage_FrameDuration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.Montage_Duration)).EndInit();
            this.LeadSelection_Panel.ResumeLayout(false);
            this.LeadSelection_Panel.PerformLayout();
            this.ConfigSensing_Tab.ResumeLayout(false);
            this.ConfigSensing_Tab.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl Mainpage_TaskTab;
        private System.Windows.Forms.TabPage Welcome_Tab;
        private System.Windows.Forms.TabPage Montage_Tab;
        private System.Windows.Forms.Button Montage_Run;
        private System.Windows.Forms.CheckBox LeftCh2_Checkbox;
        private System.Windows.Forms.CheckBox RightCh2_Checkbox;
        private System.Windows.Forms.CheckBox RightCh1_Checkbox;
        private System.Windows.Forms.CheckBox LeftCh1_Checkbox;
        private System.Windows.Forms.Panel LeadSelection_Panel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown Montage_Duration;
        private System.Windows.Forms.ComboBox Montage_SamplingRate;
        private System.Windows.Forms.NumericUpDown Montage_FrameDuration;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button Summit_Connect;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox ORCA_ProjectName;
        private System.Windows.Forms.Button ORCA_Submit;
        private System.Windows.Forms.Button Summit_DiscoverRCS;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button Summit_GetStatusButton;
        private System.Windows.Forms.Label Summit_SerialNumber;
        private System.Windows.Forms.Label Summit_BatteryLevel;
        private System.Windows.Forms.Label Summit_SenseStatus;
        private System.Windows.Forms.Label Summit_StimStatus;
        private System.Windows.Forms.Label Summit_UntilEOS;
        private System.Windows.Forms.TabPage ConfigSensing_Tab;
        private System.Windows.Forms.Button Sensing_GetStatusButton;
        private System.Windows.Forms.ComboBox Sensing_SamplingRate;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TabPage Task_Tab;
    }
}

