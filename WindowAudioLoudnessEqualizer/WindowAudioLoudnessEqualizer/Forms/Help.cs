using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wale.WinForm
{
    public partial class Help : Form
    {
        public Help()
        {
            InitializeComponent();
            ColorBindings();

            Activate();
        }

        private void ColorBindings()
        {
            this.ForeColor = ColorSet.ForeColor;
            this.BackColor = ColorSet.BackColor;

            titlePanel.BackColor = ColorSet.MainColor;

            button1.BackColor = ColorSet.BackColorAlt;
            button1.FlatAppearance.BorderColor = ColorSet.ForeColor;

            JDPack.FormPack2.Bind(panel1, "BackColor", this, "BackColor");
            JDPack.FormPack2.Bind(richTextBox1, "BackColor", this, "BackColor");
            JDPack.FormPack2.Bind(button1, "BackColor", this, "BackColor");
        }
        #region title panel control, location and size check events
        //title panel control
        private bool titleDrag = false;
        private Point titlePosition;
        private void titlePanel_MouseDown(object sender, MouseEventArgs e) { titleDrag = true; titlePosition = e.Location; }
        private void titlePanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (titleDrag)
            {
                //MessageBox.Show($"L={Screen.AllScreens[0].WorkingArea.Left} R={Screen.AllScreens[0].WorkingArea.Right}, T={Screen.AllScreens[0].WorkingArea.Top} B={Screen.AllScreens[0].WorkingArea.Bottom}");
                int x = Location.X + e.Location.X - titlePosition.X;
                if (x + this.Width >= Screen.AllScreens[0].WorkingArea.Right) x = Screen.AllScreens[0].WorkingArea.Right - this.Width;
                else if (x <= Screen.AllScreens[0].WorkingArea.Left) x = Screen.AllScreens[0].WorkingArea.Left;

                int y = Location.Y + e.Location.Y - titlePosition.Y;
                if (y + this.Height >= Screen.AllScreens[0].WorkingArea.Bottom) y = Screen.AllScreens[0].WorkingArea.Bottom - this.Height;
                else if (y <= Screen.AllScreens[0].WorkingArea.Top) y = Screen.AllScreens[0].WorkingArea.Top;
                //MessageBox.Show($"x={x} y={y}");
                Location = new Point(x, y);
            }
        }
        private void titlePanel_MouseUp(object sender, MouseEventArgs e) { titleDrag = false; }
        private void Form_LocationChanged(object sender, EventArgs e)
        {
            if ((this.Left + this.Width) > Screen.AllScreens[0].Bounds.Width)
                this.Left = Screen.AllScreens[0].Bounds.Width - this.Width;

            if (this.Left < Screen.AllScreens[0].Bounds.Left)
                this.Left = Screen.AllScreens[0].Bounds.Left;

            if ((this.Top + this.Height) > Screen.AllScreens[0].Bounds.Height)
                this.Top = Screen.AllScreens[0].Bounds.Height - this.Height;

            if (this.Top < Screen.AllScreens[0].Bounds.Top)
                this.Top = Screen.AllScreens[0].Bounds.Top;
        }
        #endregion



        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/catright/Wale/wiki");
        }
    }
}
