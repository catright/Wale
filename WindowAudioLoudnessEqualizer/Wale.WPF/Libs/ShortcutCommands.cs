using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Wale.WPF
{
    public static class ShortcutCommands
    {
        private static RoutedCommand MakeCommand(KeyGesture gesture, [CallerMemberName] string name = "")
            => new RoutedCommand(name, typeof(ShortcutCommands), new InputGestureCollection() { gesture });
        private static RoutedCommand MakeCommand(InputGestureCollection gestures, [CallerMemberName] string name = "")
            => new RoutedCommand(name, typeof(ShortcutCommands), gestures);
        //private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        //{
        //    if (e.Command is RoutedCommand com)
        //    {
        //        switch (com.Name)
        //        {
        //            case var value when value == SaveCommand.Name:
        //                break;
        //        }
        //    }
        //}
        public static readonly RoutedCommand SCDevShow = MakeCommand(new InputGestureCollection() { new KeyGesture(Key.F1, ModifierKeys.Shift), new KeyGesture(Key.F1, ModifierKeys.Control) });
        public static readonly RoutedCommand SCMasterTab = MakeCommand(new KeyGesture(Key.F2));
        public static readonly RoutedCommand SCSessionTab = MakeCommand(new KeyGesture(Key.F3));
        public static readonly RoutedCommand SCLogTab = MakeCommand(new KeyGesture(Key.F4));
        public static readonly RoutedCommand SCAlwaysTop = MakeCommand(new KeyGesture(Key.F7));
        public static readonly RoutedCommand SCStayOn = MakeCommand(new KeyGesture(Key.F8));
        public static readonly RoutedCommand SCDetail = MakeCommand(new KeyGesture(Key.F9));
        public static readonly RoutedCommand SCChangeUnit = MakeCommand(new KeyGesture(Key.F12));
    }
}