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
    public partial class DeviceMap : JDPack.FlatForm
    {
        Properties.Settings settings = Properties.Settings.Default;

        public DeviceMap()
        {
            InitializeComponent();
            SetTitle("Wale - Device Map");
            Make();
            ColorBindings();
        }

        private void Make()
        {
            
        }
        private void ColorBindings()
        {
            this.ForeColor = ColorSet.ForeColor;
            this.BackColor = ColorSet.BackColor;

            titlePanel.BackColor = ColorSet.MainColor;
            treeView1.BackColor = this.BackColor;
        }
        


        private void closeButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }
        private void updateButton_Click(object sender, EventArgs e)
        {

        }



        private void GetAllDevices()
        {
            //Wale.Subclasses.Audio.GetDevices();
        }



    }//End class DeviceMap
}//End namespace Wale.Winform
