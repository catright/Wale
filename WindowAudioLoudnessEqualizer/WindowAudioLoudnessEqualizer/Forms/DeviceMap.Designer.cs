namespace Wale.WinForm
{
    partial class DeviceMap
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
            this.titlePanel = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.updateButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.titlePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // titlePanel
            // 
            this.titlePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.titlePanel.BackColor = System.Drawing.Color.SteelBlue;
            this.titlePanel.Controls.Add(this.label3);
            this.titlePanel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.titlePanel.Location = new System.Drawing.Point(9, 9);
            this.titlePanel.Margin = new System.Windows.Forms.Padding(0);
            this.titlePanel.MinimumSize = new System.Drawing.Size(346, 50);
            this.titlePanel.Name = "titlePanel";
            this.titlePanel.Size = new System.Drawing.Size(542, 50);
            this.titlePanel.TabIndex = 15;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Algerian", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.WindowText;
            this.label3.Location = new System.Drawing.Point(19, 8);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(310, 35);
            this.label3.TabIndex = 0;
            this.label3.Text = "Wale - Device Map";
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.treeView1.Location = new System.Drawing.Point(9, 62);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(542, 296);
            this.treeView1.TabIndex = 16;
            // 
            // updateButton
            // 
            this.updateButton.Location = new System.Drawing.Point(12, 364);
            this.updateButton.Name = "updateButton";
            this.updateButton.Size = new System.Drawing.Size(332, 28);
            this.updateButton.TabIndex = 17;
            this.updateButton.Text = "Update";
            this.updateButton.UseVisualStyleBackColor = true;
            this.updateButton.Click += new System.EventHandler(this.updateButton_Click);
            // 
            // closeButton
            // 
            this.closeButton.Location = new System.Drawing.Point(350, 364);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(198, 28);
            this.closeButton.TabIndex = 18;
            this.closeButton.Text = "Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // DeviceMap
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(560, 404);
            this.ControlBox = false;
            this.Controls.Add(this.closeButton);
            this.Controls.Add(this.updateButton);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.titlePanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DeviceMap";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "DeviceMap";
            this.titlePanel.ResumeLayout(false);
            this.titlePanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel titlePanel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button updateButton;
        private System.Windows.Forms.Button closeButton;
    }
}