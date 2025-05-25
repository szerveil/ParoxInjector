using ParoxInjector.Classes;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ParoxInjector
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.ContentRendered += ONCONTENTRENDERED;
            this.Closing += WINDOWCLOSING;
        }

        // No extra functions, PLM.RefreshProcessList() is for Refresh touchDelegate event
        private Task UPDATEPROCESSFILTER(object? SENDER, EventArgs EVENTARGS) => ProcessListManager.UPDATEPROCESSFILTER();
        private void SHOW(object? SENDER, EventArgs EVENTARGS) => ProcessListManager.SHOW(this);
        private void REFRESH(object SENDER, RoutedEventArgs EVENTARGS) => ProcessListManager.REFRESH(SENDER, EVENTARGS, this);
        private void WINDOWCLOSING(object? SENDER, CancelEventArgs EVENTARGS) => DebugFile.INSERT($"[Window] Window Closed {DateTime.Now}");
        private void DEBUGC(object? SENDER, EventArgs EVENTARGS) {
            if (!DebugFile.DEBUGC()) DebugFile.INSERT($"[Window] Loaded without errors. {DateTime.Now}");
        }

        private async void ONCONTENTRENDERED(object? SENDER, EventArgs EVENTARGS) {
            await UPDATEPROCESSFILTER(SENDER, EVENTARGS);
            REFRESH(SENDER, EVENTARGS);
            SHOW(SENDER, EVENTARGS);
            DEBUGC(SENDER, EVENTARGS);
        }

        private void PROCESSLIST_SELECTIONCHANGED(object SENDER, SelectionChangedEventArgs SELECTIONCHANGEDEVENTARGS) {
            if (PROCESSLIST.SelectedItem is not null) {
                var ITEM = PROCESSLIST.SelectedItem as ListBoxItem;

                if (ITEM is not null) {
                    var PROCESSINFO = ITEM.Tag as ProcessInfoClass;
                    if (PROCESSINFO is not null) {
                        PROCESSID.Text = PROCESSINFO.PROCESSID.ToString();
                        PROCESSNAME.Text = PROCESSINFO.PROCESSNAME;
                        ProcessListManager.SHOW(this);
                    }
                }
            }
        }

        private void LOAD(object SENDER, RoutedEventArgs ROUTEDEVENTARGS) => DLLContentManager.LOAD(SENDER, ROUTEDEVENTARGS, this);
        private void REFRESH(object? SENDER, EventArgs EVENTARGS) => DLLContentManager.REFRESH(this);
        private void INJECT(object SENDER, RoutedEventArgs ROUTEDEVENTARGS) {
            try {
                int PROCESSID = int.Parse(this.PROCESSID.Text);
                string PROCESSNAME = this.PROCESSNAME.Text;
                string DLLPATH = this.DLLPATH.Text;

                if (Process.GetProcessById(PROCESSID) is null) {
                    ProcessListManager.SHOW(this);
                    Process.GetProcessesByName(PROCESSNAME);
                }

                InjectManager INJECTMANAGER = new InjectManager();
                INJECTMANAGER.LOAD(PROCESSID, DLLPATH);
            } catch (Exception EXCEPTION) {
                DebugFile.INSERT($"[InjectManager] Failed to inject {DateTime.Now}\n[InjectManager] Error: {EXCEPTION.Message}");
                MessageBox.Show($"Error: {EXCEPTION.Message}");
            }
        }

        private void TOPDRAG(object SENDER, MouseButtonEventArgs MOUSEBUTTONEVENTARGS) => WindowContentManager.TOPDRAG(SENDER, MOUSEBUTTONEVENTARGS, this);
        private void MINIMIZE(object SENDER, RoutedEventArgs ROUTEDEVENTARGS) => WindowContentManager.MINIMIZE(SENDER, ROUTEDEVENTARGS, this);
        private void CLOSE(object SENDER, RoutedEventArgs ROUTEDEVENTARGS) => WindowContentManager.CLOSE(SENDER, ROUTEDEVENTARGS, this);
    }
}
