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
        private List<Wale.CoreAudio.DeviceData> data;

        public DeviceMap()
        {
            base.InitializeComponent();
            InitializeComponent();
            SetTitle("Wale - Device Map");
            ColorBindings();
        }
        public DeviceMap(List<Wale.CoreAudio.DeviceData> data)
        {
            this.data = data;
            InitializeComponent();
            SetTitle("Wale - Device Map");
            ColorBindings();
            DrawMap();
        }
        private void ColorBindings()
        {
            this.ForeColor = ColorSet.ForeColor;
            this.BackColor = ColorSet.BackColor;

            titlePanel.BackColor = ColorSet.MainColor;
            treeView1.BackColor = this.BackColor;
        }
        

        private Task GetData()
        {
            return Task.Run(() => { data = new Wale.CoreAudio.Audio().GetDeviceList(); });
        }
        private void DrawMap()
        {
            if (data == null) { MessageBox.Show("There is no data."); return; }

            treeView1.Nodes.Clear();
            foreach (Wale.CoreAudio.DeviceData dd in data)
            {
                TreeNode node = new TreeNode(dd.Name) { ToolTipText = dd.DeviceId };
                switch (dd.State)
                {
                    case CoreAudio.DeviceState.Active:
                        node.BackColor = ColorSet.MainColor;
                        break;
                    case CoreAudio.DeviceState.Disabled:
                        node.BackColor = ColorSet.AverageColor;
                        break;
                    case CoreAudio.DeviceState.UnPlugged:
                        node.BackColor = ColorSet.PeakColor;
                        break;
                    default:
                        break;
                }
                if (dd.Sessions != null)
                {
                    foreach (Wale.CoreAudio.SessionData ss in dd.Sessions)
                    {
                        TreeNode childNode = new TreeNode(ss.Name);
                        switch (ss.State)
                        {
                            case CoreAudio.SessionState.Active:
                                childNode.BackColor = ColorSet.MainColor;
                                break;
                            case CoreAudio.SessionState.Inactive:
                                childNode.BackColor = ColorSet.AverageColor;
                                break;
                            case CoreAudio.SessionState.Expired:
                                childNode.BackColor = ColorSet.PeakColor;
                                break;
                            default:
                                break;
                        }
                        node.Nodes.Add(childNode);
                    }
                }
                node.ContextMenuStrip = new ContextMenuStrip();
                node.ContextMenuStrip.Items.Add("Enable", null, OnEnableDevice);
                node.ContextMenuStrip.Items.Add("Disable", null, OnDisableDevice);
                node.ContextMenuStrip.Items.Add("Re-Enable", null, OnReEnableDevice);
                treeView1.Nodes.Add(node);
            }

            treeView1.ExpandAll();
        }
        private void OnEnableDevice(object sender, EventArgs e)
        {
            ///TreeNode node = sender as TreeNode;//data.Find(dd => dd.Name == node.Name);
            Wale.CoreAudio.Audio audio = new CoreAudio.Audio();
            audio.EnableAudioDevice(treeView1.SelectedNode.ToolTipText);
        }
        private void OnDisableDevice(object sender, EventArgs e)
        {
            //TreeNode node = sender as TreeNode;//data.Find(dd => dd.Name == node.Name);
            Wale.CoreAudio.Audio audio = new CoreAudio.Audio();
            audio.DisableAudioDevice(treeView1.SelectedNode.ToolTipText);
        }
        private void OnReEnableDevice(object sender, EventArgs e)
        {
            //TreeNode node = sender as TreeNode;//data.Find(dd => dd.Name == node.Name);
            Wale.CoreAudio.Audio audio = new CoreAudio.Audio();
            audio.DisableAudioDevice(treeView1.SelectedNode.ToolTipText);
            audio.EnableAudioDevice(treeView1.SelectedNode.ToolTipText);
        }


        private void closeButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }
        private async void updateButton_Click(object sender, EventArgs e)
        {
            await GetData();
            DrawMap();
        }

        private void DeviceMap_Shown(object sender, EventArgs e)
        {
            updateButton_Click(sender, e);
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeView1.SelectedNode = e.Node;
        }
    }//End class DeviceMap
}//End namespace Wale.Winform
