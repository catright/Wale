using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Wale.WPF
{
    public static class ShortcutCommands
    {
        public static readonly RoutedCommand SCMasterTab = new RoutedCommand("SCAlwaysTop", typeof(ShortcutCommands), new InputGestureCollection() { new KeyGesture(Key.F2) });
        public static readonly RoutedCommand SCSessionTab = new RoutedCommand("SCAlwaysTop", typeof(ShortcutCommands), new InputGestureCollection() { new KeyGesture(Key.F3) });
        public static readonly RoutedCommand SCLogTab = new RoutedCommand("SCLogTab", typeof(ShortcutCommands), new InputGestureCollection() { new KeyGesture(Key.F4) });
        public static readonly RoutedCommand SCAlwaysTop = new RoutedCommand("SCAlwaysTop", typeof(ShortcutCommands), new InputGestureCollection() { new KeyGesture(Key.F7) });
        public static readonly RoutedCommand SCStayOn = new RoutedCommand("SCStayOn", typeof(ShortcutCommands), new InputGestureCollection() { new KeyGesture(Key.F8) });
        public static readonly RoutedCommand SCDetail = new RoutedCommand("SCDetail", typeof(ShortcutCommands), new InputGestureCollection() { new KeyGesture(Key.F9) });
    }
}