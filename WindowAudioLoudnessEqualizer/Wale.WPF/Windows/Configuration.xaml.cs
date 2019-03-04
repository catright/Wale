using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class Configuration : Window
    {
        ConfigDatalink CDL;
        /// <summary>
        /// Initialization when window is poped up. Read all setting values, store all values as original, draw all graphs.
        /// </summary>
        public Configuration(AudioControl audio, Datalink dl)
        {
            InitializeComponent();

            this.CDL = new ConfigDatalink();
            this.DataContext = this.CDL;

            MainGrid.Children.Clear();

            MainGrid.Children.Add(new TitleBar(this));

            ConfigSet cs = new ConfigSet(audio, dl, this, false, true)
            {
                //cs.LogInvokedEvent += Cs_LogInvokedEvent;
                Margin = new Thickness(0, 35, 0, 0)
            };
            cs.SizeChanged += ConfigSet_SizeChanged;
            MainGrid.Children.Add(cs);
            this.Height = cs.Height + AppDatas.TitleBarHeight;
            //this.Width = 280;

            this.Title = "Wale " + Localization.Interpreter.Current.Configuration;
        }

        private void ConfigSet_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //this.Height = (MainGrid.Children[1] as ConfigSet).Height + AppDatas.TitleBarHeight;
            DoChangeHeightSB((MainGrid.Children[1] as ConfigSet).Height + AppDatas.TitleBarHeight);
        }

        private void Cs_LogInvokedEvent(object sender, ConfigSet.LogEventArgs e)
        {
            //Console.WriteLine(e.msg);
        }

        private void DoChangeHeightSB(double newHeight, string transition = null)
        {
            var transitRegex = new System.Text.RegularExpressions.Regex(@"\d:\d:\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (transition != null && transitRegex.IsMatch(transition)) CDL.Transition = transition;
            CDL.WindowHeight = newHeight;
            //CDL.WindowTop = this.Top + (this.Height - CDL.WindowHeight);
            BeginStoryboard(this.FindResource("changeHeightSB") as System.Windows.Media.Animation.Storyboard);
        }
    }

    public class ConfigDatalink : INotifyPropertyChanged
    {

        // window height change storyboard parameters
        private double _WindowHeight = 0;
        public double WindowHeight { get => _WindowHeight; set => SetData(ref _WindowHeight, value); }
        private double _WindowTop = 0;
        public double WindowTop { get => _WindowTop; set => SetData(ref _WindowTop, value); }
        private string _Transition = "0:0:.2";
        public string Transition { get => _Transition; set => SetData(ref _Transition, value); }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void Notify([System.Runtime.CompilerServices.CallerMemberName]string name = null) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        protected bool SetData<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            Notify(name);
            return true;
        }
    }
}
