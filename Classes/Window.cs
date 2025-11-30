using System.Windows.Input;
using System.Windows;

namespace ParoxInjector.Classes {
    internal class WindowContentManager {
        public static void TOPDRAG(object sender, MouseButtonEventArgs MOUSEBUTTONEVENTARGS, MainWindow MAINWINDOW) { if (MOUSEBUTTONEVENTARGS.LeftButton == MouseButtonState.Pressed) MAINWINDOW.DragMove(); }
        public static void CLOSE(object SENDER, RoutedEventArgs ROUTEDEVENTARGS, MainWindow MAINWINDOW) => MAINWINDOW.Close();
        public static void MINIMIZE(object SENDER, RoutedEventArgs ROUTEDEVENTARGS, MainWindow MAINWINDOW) => MAINWINDOW.WindowState = WindowState.Minimized;
    }
}
