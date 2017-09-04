namespace Wale.WinForm
{
    partial class MainWindow
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
            Audio.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.NI = new System.Windows.Forms.NotifyIcon(this.components);
            this.NICMstrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cmsAutoControl = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.licensesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.tbVolume = new System.Windows.Forms.TextBox();
            this.lVolume = new System.Windows.Forms.Label();
            this.bVolumeSet = new System.Windows.Forms.Button();
            this.pbBaseLevel = new Wale.WinForm.NewProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.lBaseVolume = new System.Windows.Forms.Label();
            this.pbMasterVolume = new Wale.WinForm.NewProgressBar();
            this.tbInterval = new System.Windows.Forms.TextBox();
            this.titlePanel = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.pbMasterPeak = new Wale.WinForm.NewProgressBar();
            this.tabControl1 = new Wale.WinForm.CustomTabControl();
            this.tabMain = new System.Windows.Forms.TabPage();
            this.cbAlwaysTop = new System.Windows.Forms.CheckBox();
            this.cbStayOn = new System.Windows.Forms.CheckBox();
            this.tabSession = new System.Windows.Forms.TabPage();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.Logs = new System.Windows.Forms.TextBox();
            this.NICMstrip.SuspendLayout();
            this.titlePanel.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // NI
            // 
            this.NI.ContextMenuStrip = this.NICMstrip;
            this.NI.Icon = global::Wale.Properties.Resources.speaker_gray1;
            this.NI.Text = "Wale";
            this.NI.Visible = true;
            this.NI.MouseClick += new System.Windows.Forms.MouseEventHandler(this.NI_MouseClick);
            // 
            // NICMstrip
            // 
            this.NICMstrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cmsAutoControl,
            this.settingsToolStripMenuItem,
            this.toolStripSeparator1,
            this.helpToolStripMenuItem,
            this.licensesToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.NICMstrip.Name = "NICMstrip";
            this.NICMstrip.Size = new System.Drawing.Size(165, 120);
            // 
            // cmsAutoControl
            // 
            this.cmsAutoControl.Enabled = false;
            this.cmsAutoControl.Name = "cmsAutoControl";
            this.cmsAutoControl.Size = new System.Drawing.Size(164, 22);
            this.cmsAutoControl.Text = "&AutoControl(On)";
            this.cmsAutoControl.Visible = false;
            this.cmsAutoControl.Click += new System.EventHandler(this.AutoControlMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.settingsToolStripMenuItem.Text = "&Configuration";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.ConfigToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(161, 6);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.helpToolStripMenuItem.Text = "Help";
            this.helpToolStripMenuItem.Click += new System.EventHandler(this.helpToolStripMenuItem_Click);
            // 
            // licensesToolStripMenuItem
            // 
            this.licensesToolStripMenuItem.Name = "licensesToolStripMenuItem";
            this.licensesToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.licensesToolStripMenuItem.Text = "Licenses";
            this.licensesToolStripMenuItem.Click += new System.EventHandler(this.licensesToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(164, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.NI_Exit_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ForeColor = System.Drawing.Color.LightGray;
            this.label1.Location = new System.Drawing.Point(6, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "Master";
            // 
            // tbVolume
            // 
            this.tbVolume.AcceptsReturn = true;
            this.tbVolume.Location = new System.Drawing.Point(48, 50);
            this.tbVolume.MaxLength = 5;
            this.tbVolume.Name = "tbVolume";
            this.tbVolume.Size = new System.Drawing.Size(55, 21);
            this.tbVolume.TabIndex = 2;
            this.tbVolume.Enter += new System.EventHandler(this.textBox_Enter);
            this.tbVolume.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbVolume_KeyDown);
            this.tbVolume.Leave += new System.EventHandler(this.textBox_Leave);
            this.tbVolume.MouseEnter += new System.EventHandler(this.textBox_Enter);
            this.tbVolume.MouseLeave += new System.EventHandler(this.textBox_Leave);
            // 
            // lVolume
            // 
            this.lVolume.AutoSize = true;
            this.lVolume.ForeColor = System.Drawing.Color.CadetBlue;
            this.lVolume.Location = new System.Drawing.Point(69, 29);
            this.lVolume.Name = "lVolume";
            this.lVolume.Size = new System.Drawing.Size(11, 12);
            this.lVolume.TabIndex = 5;
            this.lVolume.Text = "0";
            this.lVolume.Click += new System.EventHandler(this.lVolume_Click);
            // 
            // bVolumeSet
            // 
            this.bVolumeSet.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.bVolumeSet.Location = new System.Drawing.Point(109, 49);
            this.bVolumeSet.Name = "bVolumeSet";
            this.bVolumeSet.Size = new System.Drawing.Size(90, 23);
            this.bVolumeSet.TabIndex = 6;
            this.bVolumeSet.Text = "Set Volume";
            this.bVolumeSet.UseVisualStyleBackColor = true;
            this.bVolumeSet.Click += new System.EventHandler(this.VolumeSet_Click);
            // 
            // pbBaseLevel
            // 
            this.pbBaseLevel.Location = new System.Drawing.Point(109, 6);
            this.pbBaseLevel.MarqueeAnimationSpeed = 10;
            this.pbBaseLevel.Name = "pbBaseLevel";
            this.pbBaseLevel.Size = new System.Drawing.Size(90, 10);
            this.pbBaseLevel.TabIndex = 7;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.LightGray;
            this.label2.Location = new System.Drawing.Point(6, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 12);
            this.label2.TabIndex = 8;
            this.label2.Text = "Base";
            // 
            // lBaseVolume
            // 
            this.lBaseVolume.AutoSize = true;
            this.lBaseVolume.ForeColor = System.Drawing.Color.LightGray;
            this.lBaseVolume.Location = new System.Drawing.Point(69, 6);
            this.lBaseVolume.Name = "lBaseVolume";
            this.lBaseVolume.Size = new System.Drawing.Size(11, 12);
            this.lBaseVolume.TabIndex = 9;
            this.lBaseVolume.Text = "0";
            // 
            // pbMasterVolume
            // 
            this.pbMasterVolume.ForeColor = System.Drawing.Color.CornflowerBlue;
            this.pbMasterVolume.Location = new System.Drawing.Point(109, 24);
            this.pbMasterVolume.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.pbMasterVolume.MarqueeAnimationSpeed = 10;
            this.pbMasterVolume.Name = "pbMasterVolume";
            this.pbMasterVolume.Size = new System.Drawing.Size(90, 10);
            this.pbMasterVolume.TabIndex = 10;
            // 
            // tbInterval
            // 
            this.tbInterval.Location = new System.Drawing.Point(8, 50);
            this.tbInterval.MaxLength = 5;
            this.tbInterval.Name = "tbInterval";
            this.tbInterval.Size = new System.Drawing.Size(36, 21);
            this.tbInterval.TabIndex = 11;
            this.tbInterval.Enter += new System.EventHandler(this.textBox_Enter);
            this.tbInterval.Leave += new System.EventHandler(this.textBox_Leave);
            this.tbInterval.MouseEnter += new System.EventHandler(this.textBox_Enter);
            this.tbInterval.MouseLeave += new System.EventHandler(this.textBox_Leave);
            // 
            // titlePanel
            // 
            this.titlePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.titlePanel.BackColor = System.Drawing.Color.CadetBlue;
            this.titlePanel.ContextMenuStrip = this.NICMstrip;
            this.titlePanel.Controls.Add(this.label3);
            this.titlePanel.ForeColor = System.Drawing.SystemColors.ControlText;
            this.titlePanel.Location = new System.Drawing.Point(5, 5);
            this.titlePanel.Margin = new System.Windows.Forms.Padding(0);
            this.titlePanel.MinimumSize = new System.Drawing.Size(220, 30);
            this.titlePanel.Name = "titlePanel";
            this.titlePanel.Size = new System.Drawing.Size(221, 30);
            this.titlePanel.TabIndex = 13;
            this.titlePanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseDown);
            this.titlePanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseMove);
            this.titlePanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseUp);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Algerian", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.WindowText;
            this.label3.Location = new System.Drawing.Point(12, 5);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 21);
            this.label3.TabIndex = 0;
            this.label3.Text = "WALE";
            this.label3.MouseDown += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseDown);
            this.label3.MouseMove += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseMove);
            this.label3.MouseUp += new System.Windows.Forms.MouseEventHandler(this.titlePanel_MouseUp);
            // 
            // pbMasterPeak
            // 
            this.pbMasterPeak.ForeColor = System.Drawing.Color.PaleVioletRed;
            this.pbMasterPeak.Location = new System.Drawing.Point(109, 34);
            this.pbMasterPeak.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.pbMasterPeak.MarqueeAnimationSpeed = 10;
            this.pbMasterPeak.Name = "pbMasterPeak";
            this.pbMasterPeak.Size = new System.Drawing.Size(90, 10);
            this.pbMasterPeak.TabIndex = 13;
            // 
            // tabControl1
            // 
            this.tabControl1.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.ContextMenuStrip = this.NICMstrip;
            this.tabControl1.Controls.Add(this.tabMain);
            this.tabControl1.Controls.Add(this.tabSession);
            this.tabControl1.Controls.Add(this.tabLog);
            this.tabControl1.Location = new System.Drawing.Point(6, 40);
            this.tabControl1.MinimumSize = new System.Drawing.Size(220, 155);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(220, 155);
            this.tabControl1.TabIndex = 15;
            // 
            // tabMain
            // 
            this.tabMain.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.tabMain.ContextMenuStrip = this.NICMstrip;
            this.tabMain.Controls.Add(this.label2);
            this.tabMain.Controls.Add(this.cbAlwaysTop);
            this.tabMain.Controls.Add(this.pbMasterVolume);
            this.tabMain.Controls.Add(this.pbMasterPeak);
            this.tabMain.Controls.Add(this.lVolume);
            this.tabMain.Controls.Add(this.bVolumeSet);
            this.tabMain.Controls.Add(this.label1);
            this.tabMain.Controls.Add(this.tbInterval);
            this.tabMain.Controls.Add(this.cbStayOn);
            this.tabMain.Controls.Add(this.lBaseVolume);
            this.tabMain.Controls.Add(this.pbBaseLevel);
            this.tabMain.Controls.Add(this.tbVolume);
            this.tabMain.Location = new System.Drawing.Point(1, 1);
            this.tabMain.Name = "tabMain";
            this.tabMain.Size = new System.Drawing.Size(218, 133);
            this.tabMain.TabIndex = 0;
            this.tabMain.Text = "Master(F3)";
            // 
            // cbAlwaysTop
            // 
            this.cbAlwaysTop.AutoSize = true;
            this.cbAlwaysTop.Checked = global::Wale.Properties.Settings.Default.AlwaysTop;
            this.cbAlwaysTop.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::Wale.Properties.Settings.Default, "AlwaysTop", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbAlwaysTop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbAlwaysTop.ForeColor = System.Drawing.Color.LightGray;
            this.cbAlwaysTop.Location = new System.Drawing.Point(8, 77);
            this.cbAlwaysTop.Name = "cbAlwaysTop";
            this.cbAlwaysTop.Size = new System.Drawing.Size(108, 16);
            this.cbAlwaysTop.TabIndex = 14;
            this.cbAlwaysTop.Text = "AlwaysTop(F7)";
            this.cbAlwaysTop.UseVisualStyleBackColor = true;
            // 
            // cbStayOn
            // 
            this.cbStayOn.AutoSize = true;
            this.cbStayOn.Checked = global::Wale.Properties.Settings.Default.StayOn;
            this.cbStayOn.DataBindings.Add(new System.Windows.Forms.Binding("Checked", global::Wale.Properties.Settings.Default, "StayOn", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.cbStayOn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbStayOn.ForeColor = System.Drawing.Color.LightGray;
            this.cbStayOn.Location = new System.Drawing.Point(8, 99);
            this.cbStayOn.Name = "cbStayOn";
            this.cbStayOn.Size = new System.Drawing.Size(85, 16);
            this.cbStayOn.TabIndex = 12;
            this.cbStayOn.Text = "StayOn(F8)";
            this.cbStayOn.UseVisualStyleBackColor = true;
            // 
            // tabSession
            // 
            this.tabSession.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.tabSession.ContextMenuStrip = this.NICMstrip;
            this.tabSession.Location = new System.Drawing.Point(1, 1);
            this.tabSession.Name = "tabSession";
            this.tabSession.Size = new System.Drawing.Size(218, 133);
            this.tabSession.TabIndex = 1;
            this.tabSession.Text = "Session(F4)";
            // 
            // tabLog
            // 
            this.tabLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.tabLog.Controls.Add(this.Logs);
            this.tabLog.Location = new System.Drawing.Point(1, 1);
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Size = new System.Drawing.Size(218, 133);
            this.tabLog.TabIndex = 2;
            this.tabLog.Text = "Log";
            // 
            // Logs
            // 
            this.Logs.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.Logs.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.Logs.ContextMenuStrip = this.NICMstrip;
            this.Logs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Logs.ForeColor = System.Drawing.Color.LightGray;
            this.Logs.Location = new System.Drawing.Point(3, 3);
            this.Logs.Multiline = true;
            this.Logs.Name = "Logs";
            this.Logs.ReadOnly = true;
            this.Logs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.Logs.Size = new System.Drawing.Size(212, 127);
            this.Logs.TabIndex = 0;
            this.Logs.VisibleChanged += new System.EventHandler(this.Logs_VisibleChanged);
            // 
            // MainWindow
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.ClientSize = new System.Drawing.Size(231, 200);
            this.ControlBox = false;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.titlePanel);
            this.DataBindings.Add(new System.Windows.Forms.Binding("TopMost", global::Wale.Properties.Settings.Default, "AlwaysTop", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(230, 200);
            this.Name = "MainWindow";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = global::Wale.Properties.Settings.Default.AlwaysTop;
            this.Deactivate += new System.EventHandler(this.MainWindow_MouseLeave);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainWindow_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWindow_FormClosed);
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.LocationChanged += new System.EventHandler(this.MainWindow_LocationAndSizeChanged);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainWindow_KeyDown);
            this.MouseLeave += new System.EventHandler(this.MainWindow_MouseLeave);
            this.NICMstrip.ResumeLayout(false);
            this.titlePanel.ResumeLayout(false);
            this.titlePanel.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabMain.ResumeLayout(false);
            this.tabMain.PerformLayout();
            this.tabLog.ResumeLayout(false);
            this.tabLog.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon NI;
        private System.Windows.Forms.ContextMenuStrip NICMstrip;
        private System.Windows.Forms.ToolStripMenuItem cmsAutoControl;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbVolume;
        private System.Windows.Forms.Label lVolume;
        private System.Windows.Forms.Button bVolumeSet;
        private Wale.WinForm.NewProgressBar pbBaseLevel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lBaseVolume;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private Wale.WinForm.NewProgressBar pbMasterVolume;
        private System.Windows.Forms.TextBox tbInterval;
        private System.Windows.Forms.CheckBox cbStayOn;
        private System.Windows.Forms.Panel titlePanel;
        private System.Windows.Forms.Label label3;
        private Wale.WinForm.NewProgressBar pbMasterPeak;
        private System.Windows.Forms.CheckBox cbAlwaysTop;
        private Wale.WinForm.CustomTabControl tabControl1;
        private System.Windows.Forms.TabPage tabMain;
        private System.Windows.Forms.TabPage tabSession;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.TextBox Logs;
        private System.Windows.Forms.ToolStripMenuItem licensesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
    }
}

