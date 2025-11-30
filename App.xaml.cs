using System.Windows;
using ParoxInjector.Classes;

namespace ParoxInjector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs STARTUPEVENTARGS)
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            this.Startup += App_Startup;
            base.OnStartup(STARTUPEVENTARGS);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e){
            e.Handled = true;
        }

        private void App_Startup(object sender, StartupEventArgs e) {
            DBUG.CLEAR();
        }
    }
}
