using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ParoxInjector.Classes;

namespace ParoxInjector
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.ContentRendered += RefreshRecents;
            this.ContentRendered += ShowProcessList;
            this.ContentRendered += LogIfDebug;
            this.Closing += WindowClosing;
        }

        // No extra functions, PLM.RefreshProcessList() is for Refresh touchDelegate event
        private void ShowProcessList(object? sender, EventArgs e) => ProcessListManager.ShowProcessList(this);
        private void RefreshProcessList(object sender, RoutedEventArgs e) => ProcessListManager.RefreshProcessList(sender, e, this);
        private void WindowClosing(object? sender, CancelEventArgs e) => DebugFile.Insert($"[Window] Window Closed {DateTime.Now}");
        private void LogIfDebug(object? sender, EventArgs e)
        {
            if (!DebugFile.WasDebugWrote())
            {
                DebugFile.Clear();
                DebugFile.Insert($"[DebugFile] Window Loaded without errors. {DateTime.Now}");
            }
        }

        private void ProcessList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProcessList.SelectedItem != null)
            {
                var listBoxItem = ProcessList.SelectedItem as ListBoxItem;
                if (listBoxItem != null)
                {
                    var processInfo = listBoxItem.Tag as ProcessInfo;
                    if (processInfo != null)
                    {
                        ProcessId.Text = processInfo.ProcessId.ToString();
                        ProcessName.Text = processInfo.ProcessName;
                        ProcessListManager.ShowProcessList(this);
                    }
                }
            }
        }

        private void LoadDLL(object sender, RoutedEventArgs e) => DLLContentManager.LoadDLL(sender, e, this);
        private void RefreshRecents(object? sender, EventArgs e) => DLLContentManager.RefreshRecents(this);
        private void InjectDLL(object sender, RoutedEventArgs e)
        {
            try
            {
                int processID = int.Parse(ProcessId.Text);
                string processName = ProcessName.Text;
                string dllPath = DLLPath.Text;

                if(Process.GetProcessById(processID) == null)
                {
                    ProcessListManager.ShowProcessList(this);
                    Process.GetProcessesByName(processName);
                }

                InjectManager injectManager = new InjectManager();
                injectManager.InjectDLL(processID, dllPath);
            }
            catch (Exception ex)
            {
                DebugFile.Insert($"[InjectManager] Failed to inject {DateTime.Now}\n[InjectManager] Error: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void TopDrag(object sender, MouseButtonEventArgs e) => WindowContentManager.TopDrag(sender, e, this);
        private void Minimize(object sender, RoutedEventArgs e) => WindowContentManager.Minimize(sender, e, this);
        private void Close(object sender, RoutedEventArgs e) => WindowContentManager.Close(sender, e, this);
    }
}
