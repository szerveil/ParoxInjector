using ParoxInjector.Classes;
using System.ComponentModel;
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

        // No extra functions, FilterClass.RoutedRefresh() is for Refresh touchDelegate event
        private void SetWindowDebug(object? SENDER, EventArgs EVENTARGS) {
            if (DBUG.SetWindowDebug() == DBUG.DEBUGFLAGS.None) DBUG.INSERT($"[Window] Loaded.", DEBUGLOGLEVEL.INFO);
        }
        private Task PopulateFilter(object? SENDER, EventArgs EVENTARGS) => FilterClass.PopulateFilter();
        private Task Refresh(object? SENDER, EventArgs EVENTARGS) => FilterClass.Refresh(this);
        private async void RoutedRefresh(object SENDER, RoutedEventArgs EVENTARGS) => await FilterClass.RoutedRefresh(SENDER, EVENTARGS, this);
        private void WINDOWCLOSING(object? SENDER, CancelEventArgs EVENTARGS) => DBUG.INSERT($"[Window] Window Closed.", DEBUGLOGLEVEL.INFO);

        private async void ONCONTENTRENDERED(object? SENDER, EventArgs EVENTARGS) {
            SetWindowDebug(SENDER, EVENTARGS);
            await PopulateFilter(SENDER, EVENTARGS);
            await Refresh(SENDER, EVENTARGS);
        }

        private async void PROCESSLIST_SELECTIONCHANGED(object SENDER, SelectionChangedEventArgs SELECTIONCHANGEDEVENTARGS) {
            if (FilterPass.SelectedItem is not null) {
                var ITEM = FilterPass.SelectedItem as ListBoxItem;

                if (ITEM is not null) {
                    var PROCESSINFO = ITEM.Tag as ProcessInfo;
                    if (PROCESSINFO is not null) {
                        PROCESSID.Text = PROCESSINFO.ID.ToString();
                        PROCESSNAME.Text = PROCESSINFO.Name;
                        await FilterClass.Refresh(this);
                    }
                }
            }
        }

        private async void LOAD(object SENDER, RoutedEventArgs ROUTEDEVENTARGS) => await UiContent.load(SENDER, ROUTEDEVENTARGS, this);
        private void REFRESH(object? SENDER, EventArgs EVENTARGS) => UiContent.REFRESH(this);
        private async void Hook(object SENDER, RoutedEventArgs ROUTEDEVENTARGS) {
            try {
                int ProcessID = int.Parse(this.PROCESSID.Text);
                string PROCESSNAME = this.PROCESSNAME.Text;
                string DLLPath = this.DLLPath.Text;

                bool Process = await Task.Run(() => {
                    try {
                        System.Diagnostics.Process.GetProcessById(ProcessID);
                        return true;
                    }
                    catch { return false; }
                });

                Hooks Hook = new Hooks();
                Hook.HookProcess(ProcessID, DLLPath);

                DBUG.INSERT($"[InjectManager] {PROCESSNAME} (PID: {ProcessID}) not found", DEBUGLOGLEVEL.INFO);
                await FilterClass.Refresh(this);
            }
            catch (Exception EXCEPTION) { DBUG.INSERT("[InjectManager] Failed.", DEBUGLOGLEVEL.ERROR, EXCEPTION); }
        }

        private void TOPDRAG(object SENDER, MouseButtonEventArgs MOUSEBUTTONEVENTARGS) => WindowContentManager.TOPDRAG(SENDER, MOUSEBUTTONEVENTARGS, this);
        private void MINIMIZE(object SENDER, RoutedEventArgs ROUTEDEVENTARGS) => WindowContentManager.MINIMIZE(SENDER, ROUTEDEVENTARGS, this);
        private void CLOSE(object SENDER, RoutedEventArgs ROUTEDEVENTARGS) => WindowContentManager.CLOSE(SENDER, ROUTEDEVENTARGS, this);
    }
}
