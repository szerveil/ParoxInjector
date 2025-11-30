using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class ProcessInfo
{
    public int ID { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public Icon? Icon { get; set; }
}

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
public static class ProcessExtensions {
    [DllImport("Kernel32.dll")]
    private static extern uint QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

    public static string? GetMainModuleFileName(this Process PROCESS, int BUFFER = 1024) {
        try {
            var FILENAMEBUILDER = new StringBuilder(BUFFER);
            uint BUFFERLENGTH = (uint)FILENAMEBUILDER.Capacity + 1;
            return (QueryFullProcessImageName(PROCESS.Handle, 0, FILENAMEBUILDER, ref BUFFERLENGTH) is not 0) ? FILENAMEBUILDER.ToString() : null;
        } catch (Win32Exception) {
            return null;
        }
    }

    public static Icon GetIcon(this Process PROCESS) {
        try {
            string MAINMODULEFILENAME = PROCESS.GetMainModuleFileName();
            if (string.IsNullOrEmpty(MAINMODULEFILENAME)) return null;
            return Icon.ExtractAssociatedIcon(MAINMODULEFILENAME);
        } catch { return null; }
    }

    public static Process ParentProcess(this Process Process) {
        if (Process == null) throw new ArgumentNullException(nameof(Process));

        int ParentProcessID;
        int ProcessID = Process.Id;

        using (ManagementObject ManagementObject = new ManagementObject($"win32_process.handle='{ProcessID}'")) {
            ManagementObject.Get();
            ParentProcessID = Convert.ToInt32(ManagementObject["ParentProcessId"]);
        }

        try { return Process.GetProcessById(ParentProcessID); } catch (ArgumentException) { return null; }
    }
}

public static class IconUtilities {
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);
    public static ImageSource CONVERT(this Bitmap Icon) {
        IntPtr IconBitmap = Icon.GetHbitmap();
        ImageSource Image = Imaging.CreateBitmapSourceFromHBitmap(IconBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        if (!DeleteObject(IconBitmap)) throw new Win32Exception();

        return Image;
    }
}

namespace ParoxInjector.Classes {
    internal class FilterClass {
        private static string? FilterStrings;
        private static HashSet<string> FilterArray = [];
        private static readonly string? Filter = ParoxIO.fetchPath("ProcessFilter.txt");
        private static void Clear(MainWindow Window) {  Window.FilterPass.Items.Clear(); }
        private static async Task PopulateFilterStrings() { try { FilterStrings = await new HttpClient().GetStringAsync("https://raw.githubusercontent.com/szerveil/ParoxInjector/refs/heads/main/ProcessFilter.txt"); } catch { return; } }

        public static async Task PopulateFilter() {
            await PopulateFilterStrings();

            if (!File.Exists(Filter)) {
                try {
                    await File.WriteAllTextAsync(Filter, FilterStrings);
                    DBUG.INSERT($"FilterStrings Populated.", DEBUGLOGLEVEL.INFO);
                } catch { DBUG.INSERT($"Local Process Filter loaded.", DEBUGLOGLEVEL.ERROR); }
            }

            if (File.Exists(Filter)) {
                try {
                    string FilterStringsFile = await File.ReadAllTextAsync(Filter);

                    if (FilterStrings is not null) {
                        if (FilterStringsFile == FilterStrings) {
                            var FILTER = FilterStringsFile.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            FilterArray = new HashSet<string>(FILTER, StringComparer.OrdinalIgnoreCase);
                            DBUG.INSERT($"Local Process Filter loaded.", DEBUGLOGLEVEL.INFO);
                        } else {
                            await File.WriteAllTextAsync(Filter, FilterStrings);
                            DBUG.INSERT($"Local Process Filter updated.", DEBUGLOGLEVEL.INFO);

                            var FILTER = FilterStrings.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            FilterArray = new HashSet<string>(FILTER, StringComparer.OrdinalIgnoreCase);

                            DBUG.INSERT($"Local Process Filter loaded.", DEBUGLOGLEVEL.INFO);
                        }
                    } else {
                        var FILTER = FilterStringsFile.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        FilterArray = new HashSet<string>(FILTER, StringComparer.OrdinalIgnoreCase);

                        DBUG.INSERT($"Local Process Filter could not be updated.", DEBUGLOGLEVEL.WARNING);
                        if (FILTER.Length > 0) DBUG.INSERT($"Local Process Filter loaded.", DEBUGLOGLEVEL.INFO); 
                        else DBUG.INSERT($"Local Process Filter is empty.", DEBUGLOGLEVEL.WARNING);

                        MessageBox.Show("Local Process Filter update failed.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                } catch {}
            }
        }

        public static async Task Refresh(MainWindow MAINWINDOW) {
            Clear(MAINWINDOW);
            if (FilterArray == (HashSet<string>) []) await PopulateFilter();
            await FilterPass(MAINWINDOW);
        }

        public static async Task RoutedRefresh(object SENDER, RoutedEventArgs ROUTEDEVENTARGS, MainWindow Window) {
            Clear(Window);
            if (FilterArray == (HashSet<string>) []) await PopulateFilter();
            await FilterPass(Window);
        }

        private static async Task FilterPass(MainWindow Window) {
            var Verified = new HashSet<int>();

            if (FilterArray == (HashSet<string>) []) await PopulateFilter();

            await Task.Run(() => {
                foreach (var Process in Process.GetProcesses()) {
                    if (FilterArray.Contains(Process.ProcessName)) continue;

                    var PARENTPROCESS = Process.ParentProcess();
                    if (PARENTPROCESS != null && Verified.Contains(PARENTPROCESS.Id)) continue;

                    var ProcessInfo = new ProcessInfo {
                        ID = Process.Id,
                        Name = Process.ProcessName,
                        Title = Process.MainWindowTitle,
                        Icon = Process.GetIcon() ?? SystemIcons.Application
                    };

                    Application.Current.Dispatcher.Invoke(() => {
                        var Process = new ListBoxItem {
                            Content = $"{ProcessInfo.Name} (Process ID: {ProcessInfo.ID})",
                            Tag = ProcessInfo
                        };

                        var ProcessContent = new Grid();
                        ProcessContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                        ProcessContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var ProcessIcon = new System.Windows.Controls.Image { Source = ProcessInfo.Icon.ToBitmap().CONVERT(), Width = 32, Height = 32 };
                        var ProcessInfoBlock = new TextBlock { Text = $"{ProcessInfo.Name} (Process ID: {ProcessInfo.ID})", Style = (Style)Application.Current.Resources["ProcessInfoTextBlock"] };

                        Grid.SetColumn(ProcessIcon, 0);
                        ProcessContent.Children.Add(ProcessIcon);
                        Grid.SetColumn(ProcessInfoBlock, 1);
                        ProcessContent.Children.Add(ProcessInfoBlock);

                        Process.Content = ProcessContent;
                        Process.Tag = ProcessInfo;
                        Window.FilterPass.Items.Add(Process);

                        Verified.Add(ProcessInfo.ID);
                    });
                }
            });
        }
    }
}
