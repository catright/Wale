using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace JDPack
{
    public class FlatForm : Form
    {
        protected Panel titlePanel;
        protected Label titleLabel;

        #region Title bar
        protected bool titleDrag = false;
        protected Point titlePosition;

        protected void titlePanel_MouseDown(object sender, MouseEventArgs e) { titleDrag = true; titlePosition = e.Location; }
        protected void titlePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (titleDrag)
            {
                //MessageBox.Show($"L={Screen.PrimaryScreen.WorkingArea.Left} R={Screen.PrimaryScreen.WorkingArea.Right}, T={Screen.PrimaryScreen.WorkingArea.Top} B={Screen.PrimaryScreen.WorkingArea.Bottom}");
                int x = Location.X + e.Location.X - titlePosition.X;
                if (x + this.Width >= Screen.PrimaryScreen.WorkingArea.Right) x = Screen.PrimaryScreen.WorkingArea.Right - this.Width;
                else if (x <= Screen.PrimaryScreen.WorkingArea.Left) x = Screen.PrimaryScreen.WorkingArea.Left;

                int y = Location.Y + e.Location.Y - titlePosition.Y;
                if (y + this.Height >= Screen.PrimaryScreen.WorkingArea.Bottom) y = Screen.PrimaryScreen.WorkingArea.Bottom - this.Height;
                else if (y <= Screen.PrimaryScreen.WorkingArea.Top) y = Screen.PrimaryScreen.WorkingArea.Top;
                //MessageBox.Show($"x={x} y={y}");
                Location = new Point(x, y);
            }
        }
        protected void titlePanel_MouseUp(object sender, MouseEventArgs e) { titleDrag = false; }
        protected void Form_LocationChanged(object sender, EventArgs e)
        {
            if ((this.Left + this.Width) > Screen.PrimaryScreen.Bounds.Width)
                this.Left = Screen.PrimaryScreen.Bounds.Width - this.Width;

            if (this.Left < Screen.PrimaryScreen.Bounds.Left)
                this.Left = Screen.PrimaryScreen.Bounds.Left;

            if ((this.Top + this.Height) > Screen.PrimaryScreen.Bounds.Height)
                this.Top = Screen.PrimaryScreen.Bounds.Height - this.Height;

            if (this.Top < Screen.PrimaryScreen.Bounds.Top)
                this.Top = Screen.PrimaryScreen.Bounds.Top;
        }

        public void SetTitle(string t) { this.titleLabel.Text = t; }
        #endregion

        public virtual void InitializeComponent()
        {
            this.titlePanel = new System.Windows.Forms.Panel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.titlePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // titlePanel
            // 
            this.titlePanel.BackColor = System.Drawing.Color.SteelBlue;
            this.titlePanel.Controls.Add(this.titleLabel);
            this.titlePanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.titlePanel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.titlePanel.Location = new System.Drawing.Point(0, 0);
            this.titlePanel.Margin = new System.Windows.Forms.Padding(0);
            this.titlePanel.MaximumSize = new System.Drawing.Size(0, 35);
            this.titlePanel.MinimumSize = new System.Drawing.Size(0, 35);
            this.titlePanel.Name = "titlePanel";
            this.titlePanel.Size = new System.Drawing.Size(276, 35);
            this.titlePanel.TabIndex = 15;
            this.titlePanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseDown);
            this.titlePanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseMove);
            this.titlePanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseUp);
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.titleLabel.Font = new System.Drawing.Font("Algerian", 13F);
            this.titleLabel.ForeColor = System.Drawing.SystemColors.WindowText;
            this.titleLabel.Location = new System.Drawing.Point(0, 0);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Padding = new System.Windows.Forms.Padding(12, 8, 12, 8);
            this.titleLabel.Size = new System.Drawing.Size(82, 36);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "WALE";
            this.titleLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseDown);
            this.titleLabel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseMove);
            this.titleLabel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseUp);
            // 
            // FlatForm
            // 
            this.ClientSize = new System.Drawing.Size(276, 239);
            this.ControlBox = false;
            this.Controls.Add(this.titlePanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FlatForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.LocationChanged += new System.EventHandler(this.Form_LocationChanged);
            this.titlePanel.ResumeLayout(false);
            this.titlePanel.PerformLayout();
            this.ResumeLayout(false);

        }
    }//End class FlatForm
}// End namespace JDPack
