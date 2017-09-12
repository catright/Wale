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
    public partial class Licenses : JDPack.FlatForm
    {
        public Licenses()
        {
            InitializeComponent();
            SetTitle("Wale - Licenses");
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


            JDPack.FormPack2.Bind(tabPage1, "BackColor", this, "BackColor");
            JDPack.FormPack2.Bind(tabPage2, "BackColor", this, "BackColor");
            JDPack.FormPack2.Bind(richTextBox1, "BackColor", this, "BackColor");
            JDPack.FormPack2.Bind(richTextBox2, "BackColor", this, "BackColor");
            JDPack.FormPack2.Bind(button1, "BackColor", this, "BackColor");
        }


        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }



    }//End class Licenses
}//End namespace Wale.WinForm