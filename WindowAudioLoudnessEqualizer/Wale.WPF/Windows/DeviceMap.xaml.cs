using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for DeviceMap.xaml
    /// </summary>
    public partial class DeviceMap : Window
    {
        //Properties.Settings settings = Properties.Settings.Default;
        private CoreAudio.MapDataDevice[] data;

        public DeviceMap()
        {
            InitializeComponent();
            Init();
        }
        private void Init()
        {
            MainGrid.Children.RemoveAt(0);
            MainGrid.Children.Insert(0, new TitleBar(this));

            this.Title = "Wale " + Localization.Interpreter.Current.DeviceMap;
            if (ContentGrid.Children[0].GetType() == typeof(Loading)) { ContentGrid.Children.RemoveAt(0); }
        }


        #region Data control
        private Task GetData() => Task.Run(() => data = new CoreAudio.Map().Devices);
        private void DrawMap()
        {
            if (data == null) { MessageBox.Show("There is no data."); return; }
            //View.ItemsSource = data;
            View.Items.Clear();
            View.Items.IsLiveSorting = true;
            for (int i = 0; i < data.Length; i++)
            {
                TreeViewItem node = new TreeViewItem() { Header = data[i].Name, ToolTip = data[i].DeviceId };
                switch (data[i].State)
                {
                    case CoreAudio.DeviceState.Active:
                        node.Background = new SolidColorBrush(ColorSet.MainColor);
                        break;
                    case CoreAudio.DeviceState.Disabled:
                        node.Background = new SolidColorBrush(ColorSet.AverageColor);
                        break;
                    case CoreAudio.DeviceState.UnPlugged:
                        node.Background = new SolidColorBrush(ColorSet.PeakColor);
                        break;
                    default:
                        break;
                }
                if (data[i].Sessions != null)
                {
                    for (int j = 0; j < data[i].Sessions.Length; j++)
                    {
                        TreeViewItem childNode = new TreeViewItem() { Header = data[i].Sessions[j].Name };
                        switch (data[i].Sessions[j].State)
                        {
                            case CoreAudio.SessionState.AudioSessionStateActive:
                                childNode.Background = new SolidColorBrush(ColorSet.MainColor);
                                break;
                            case CoreAudio.SessionState.AudioSessionStateInactive:
                                childNode.Background = new SolidColorBrush(ColorSet.AverageColor);
                                break;
                            case CoreAudio.SessionState.AudioSessionStateExpired:
                                childNode.Background = new SolidColorBrush(ColorSet.PeakColor);
                                break;
                            default:
                                break;
                        }
                        childNode.IsExpanded = true;
                        node.Items.Add(childNode);
                    }
                }
                node.ContextMenu = new ContextMenu();

                MenuItem item1 = new MenuItem() { Header = "Enable", };
                item1.Click += OnEnableDevice;
                node.ContextMenu.Items.Add(item1);

                MenuItem item2 = new MenuItem() { Header = "Disable", };
                item2.Click += OnDisableDevice;
                node.ContextMenu.Items.Add(item2);

                MenuItem item3 = new MenuItem() { Header = "Re-Enable", };
                item3.Click += OnReEnableDevice;
                node.ContextMenu.Items.Add(item3);

                node.IsExpanded = true;
                View.Items.Add(node);
            }
            //View.Items.LiveSortingProperties.Add("Name");
            View.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("Header", System.ComponentModel.ListSortDirection.Ascending));
        }
        private void OnEnableDevice(object sender, EventArgs e)
        {
            ///TreeNode node = sender as TreeNode;//data.Find(dd => dd.Name == node.Name);
            //Wale.CoreAudio.Audio audio = new CoreAudio.Audio();
            //audio.EnableAudioDevice(View.SelectedItem.ToolTipText);
        }
        private void OnDisableDevice(object sender, EventArgs e)
        {
            //TreeNode node = sender as TreeNode;//data.Find(dd => dd.Name == node.Name);
            //Wale.CoreAudio.Audio audio = new CoreAudio.Audio();
            //audio.DisableAudioDevice(View.SelectedNode.ToolTipText);
        }
        private void OnReEnableDevice(object sender, EventArgs e)
        {
            //TreeNode node = sender as TreeNode;//data.Find(dd => dd.Name == node.Name);
            //Wale.CoreAudio.Audio audio = new CoreAudio.Audio();
            //audio.DisableAudioDevice(View.SelectedNode.ToolTipText);
            //audio.EnableAudioDevice(View.SelectedNode.ToolTipText);
        }
        #endregion

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            ContentGrid.Children.Insert(0, new Loading() { Margin = new Thickness(0, 0, 0, 30) });
            //LoadingCircle.Visibility = Visibility.Visible;
            await GetData();
            DrawMap();
            //LoadingCircle.Visibility = Visibility.Hidden;
            if (ContentGrid.Children[0].GetType() == typeof(Loading)) { ContentGrid.Children.RemoveAt(0); }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Update_Click(sender, new RoutedEventArgs());
        }
    }
}
