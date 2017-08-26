namespace Wale.WinForm
{
    partial class ResetBaseVolume
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
            this.tbValue = new System.Windows.Forms.TextBox();
            this.bOk = new System.Windows.Forms.Button();
            this.bCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbValue
            // 
            this.tbValue.Location = new System.Drawing.Point(12, 12);
            this.tbValue.Name = "tbValue";
            this.tbValue.Size = new System.Drawing.Size(123, 21);
            this.tbValue.TabIndex = 0;
            this.tbValue.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbValue_KeyDown);
            // 
            // bOk
            // 
            this.bOk.Location = new System.Drawing.Point(12, 39);
            this.bOk.Name = "bOk";
            this.bOk.Size = new System.Drawing.Size(58, 23);
            this.bOk.TabIndex = 1;
            this.bOk.Text = "OK";
            this.bOk.UseVisualStyleBackColor = true;
            this.bOk.Click += new System.EventHandler(this.bOk_Click);
            // 
            // bCancel
            // 
            this.bCancel.Location = new System.Drawing.Point(77, 39);
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size(58, 23);
            this.bCancel.TabIndex = 2;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            this.bCancel.Click += new System.EventHandler(this.bCancel_Click);
            // 
            // ResetBaseVolume
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(147, 70);
            this.ControlBox = false;
            this.Controls.Add(this.bCancel);
            this.Controls.Add(this.bOk);
            this.Controls.Add(this.tbValue);
            this.Name = "ResetBaseVolume";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "baseVolume";
            this.TopMost = true;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.InputMessageBox_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbValue;
        private System.Windows.Forms.Button bOk;
        private System.Windows.Forms.Button bCancel;
    }
}