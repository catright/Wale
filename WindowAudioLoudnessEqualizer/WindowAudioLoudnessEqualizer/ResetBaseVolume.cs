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
    public partial class ResetBaseVolume : Form
    {
        public double baseVolume;

        public ResetBaseVolume() { InitializeComponent(); }
        public ResetBaseVolume(double v) { InitializeComponent(); baseVolume = v; }

        private void InputMessageBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { bCancel_Click(sender, e); }
        }
        private void bCancel_Click(object sender, EventArgs e) { DialogResult = DialogResult.Cancel; Close(); }

        private void tbValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Return) { bOk_Click(sender, e); }
        }
        private void bOk_Click(object sender, EventArgs e)
        {
            try { baseVolume = Convert.ToDouble(tbValue.Text); }
            catch { MessageBox.Show("Invalid value"); return; }
            DialogResult = DialogResult.OK;
            Close();
        }



    }
}
