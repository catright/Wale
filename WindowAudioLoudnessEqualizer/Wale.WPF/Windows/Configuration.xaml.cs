using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using Wale.Configs;

namespace Wale.WPF
{
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class Configuration : Window
    {
        //private readonly Window owner;
        private readonly ConfigDatalink CDL;
        internal readonly General gl;
        /// <summary>
        /// Initialization when window is poped up. Read all setting values, store all values as original, draw all graphs.
        /// </summary>
        public Configuration(Window owner)
        {
            InitializeComponent();

            //this.owner = owner;
            if (owner is MainWindow m) gl = m.gl;
            else throw new ArgumentException("ConfigSet: owner is not a MainWindow");
            this.DataContext = this.CDL = new ConfigDatalink();

            MainGrid.Children.Clear();
            MainGrid.Children.Add(new TitleBar(this));

            ConfigSet cs = new ConfigSet(this, false, true)
            {
                //cs.LogInvokedEvent += Cs_LogInvokedEvent;
                Margin = new Thickness(0, 35, 0, 0)
            };
            //cs.SizeChanged += ConfigSet_SizeChanged;
            gl.PropertyChanged += Default_PropertyChanged;
            MainGrid.Children.Add(cs);
            this.Height = (gl.AdvancedView ? Visual.ConfigSetLongHeight : Visual.ConfigSetHeight) + Visual.TitleBarHeight;
            //this.Width = 280;

            this.Title = "Wale " + Localization.Interpreter.Current.Configuration;
        }

        private void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AdvancedView")
            {
                DoChangeHeightSB((gl.AdvancedView ? Visual.ConfigSetLongHeight : Visual.ConfigSetHeight) + Visual.TitleBarHeight);
            }
        }

        private void ConfigSet_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //this.Height = (MainGrid.Children[1] as ConfigSet).Height + Wale.Configuration.Visual.TitleBarHeight;
            //DoChangeHeightSB((MainGrid.Children[1] as ConfigSet).Height + Wale.Configuration.Visual.TitleBarHeight);
            DoChangeHeightSB((gl.AdvancedView ? Visual.ConfigSetLongHeight : Visual.ConfigSetHeight) + Visual.TitleBarHeight);
        }

        private void Cs_LogInvokedEvent(object sender, ConfigSet.LogEventArgs e)
        {
            //Console.WriteLine(e.msg);
        }

        private void DoChangeHeightSB(double newHeight, string transition = null)
        {
            var transitRegex = new Regex(@"\d:\d:\d", RegexOptions.IgnoreCase);
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
        protected void Notify([System.Runtime.CompilerServices.CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected bool SetData<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            Notify(name);
            return true;
        }
    }
}
