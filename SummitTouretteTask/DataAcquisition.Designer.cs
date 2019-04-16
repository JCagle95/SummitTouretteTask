namespace SummitTouretteTask
{
    partial class DataAcquisition
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
            this.instruction = new System.Windows.Forms.Label();
            this.Task_Image = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.Task_Image)).BeginInit();
            this.SuspendLayout();
            // 
            // instruction
            // 
            this.instruction.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.instruction.Font = new System.Drawing.Font("Microsoft YaHei", 50F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.instruction.Location = new System.Drawing.Point(175, 59);
            this.instruction.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.instruction.Name = "instruction";
            this.instruction.Size = new System.Drawing.Size(450, 312);
            this.instruction.TabIndex = 1;
            this.instruction.Text = "Task Idled";
            this.instruction.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Task_Image
            // 
            this.Task_Image.Location = new System.Drawing.Point(190, 59);
            this.Task_Image.Name = "Task_Image";
            this.Task_Image.Size = new System.Drawing.Size(435, 312);
            this.Task_Image.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.Task_Image.TabIndex = 2;
            this.Task_Image.TabStop = false;
            // 
            // DataAcquisition
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Azure;
            this.ClientSize = new System.Drawing.Size(787, 431);
            this.Controls.Add(this.Task_Image);
            this.Controls.Add(this.instruction);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "DataAcquisition";
            this.Text = "DataAcquisitionTest";
            ((System.ComponentModel.ISupportInitialize)(this.Task_Image)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label instruction;
        private System.Windows.Forms.PictureBox Task_Image;
    }
}