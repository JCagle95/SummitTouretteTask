namespace SummitTouretteTask
{
    partial class MontageTask
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
            this.SuspendLayout();
            // 
            // instruction
            // 
            this.instruction.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.instruction.Font = new System.Drawing.Font("Microsoft YaHei", 50F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.instruction.Location = new System.Drawing.Point(350, 150);
            this.instruction.Name = "instruction";
            this.instruction.Size = new System.Drawing.Size(900, 600);
            this.instruction.TabIndex = 0;
            this.instruction.Text = "+";
            this.instruction.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MontageTask
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(1574, 829);
            this.Controls.Add(this.instruction);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Name = "MontageTask";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Montage Check Task";
            this.Load += new System.EventHandler(this.MontageTask_Load);
            this.ResumeLayout(false);

        }

        #endregion
        
        private System.Windows.Forms.Label instruction;
    }
}