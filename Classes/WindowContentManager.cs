using System.Windows.Input;
using System.Windows;

namespace ParoxInjector.Classes
{
    internal class WindowContentManager
    {
        public static void TopDrag(object sender, MouseButtonEventArgs e, MainWindow mainWindow)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                mainWindow.DragMove();
            }
        }

        public static void Close(object sender, RoutedEventArgs e, MainWindow mainWindow)
        {
            mainWindow.Close();
        }

        public static void Minimize(object sender, RoutedEventArgs e, MainWindow mainWindow)
        {
            mainWindow.WindowState = WindowState.Minimized;
        }
    }
}
