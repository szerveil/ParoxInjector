using System.Diagnostics;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Interop;
using System.ComponentModel;
using System.Management;
using System.Net.Http;
using System.IO;
using System.Reflection;

public class ProcessInfoClass
{
    public int PROCESSID { get; set; }
    public string? PROCESSNAME { get; set; }
    public string? WINDOWTITLE { get; set; }
    public Icon? ICON { get; set; }
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

    public static Icon GETICON(this Process PROCESS) {
        try {
            string MAINMODULEFILENAME = PROCESS.GetMainModuleFileName();
            if (string.IsNullOrEmpty(MAINMODULEFILENAME)) return null;
            return Icon.ExtractAssociatedIcon(MAINMODULEFILENAME);
        } catch { return null; }
    }

    public static Process GETPARENTPROCESS(this Process PROCESS) {
        try {
            int PARENTPID = 0;
            int PROCESSPID = PROCESS.Id;
            using (ManagementObject MANAGEMENTOBJECT = new ManagementObject($"win32_process.handle='{PROCESSPID}'")) {
                MANAGEMENTOBJECT.Get();
                PARENTPID = Convert.ToInt32(MANAGEMENTOBJECT["ParentProcessId"]);
            }

            return Process.GetProcessById(PARENTPID);
        } catch { return null; }
    }
}

public static class IconUtilities {
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);
    public static ImageSource BITMAPASIMAGESOURCE(this Bitmap ICON) {
        IntPtr BITMAP = ICON.GetHbitmap();
        ImageSource IMAGE = Imaging.CreateBitmapSourceFromHBitmap(BITMAP, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        if (!DeleteObject(BITMAP)) throw new Win32Exception();

        return IMAGE;
    }
}

namespace ParoxInjector.Classes {
    internal class ProcessListManager {
        private static HashSet<string>? PROCESSFILTER;
        private static readonly string FILTERFILE = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "ProcessFilter.txt");

        public static void CLEARPROCESSLIST(MainWindow MAINWINDOW) { MAINWINDOW.PROCESSLIST.Items.Clear(); }

        public static async Task UPDATEPROCESSFILTER() {
            string? GITHUBFILTER;

            try { GITHUBFILTER = await new HttpClient().GetStringAsync("https://raw.githubusercontent.com/szerveil/ParoxInjector/refs/heads/main/ProcessFilter.txt"); } catch { GITHUBFILTER = null; }

            if(!File.Exists(FILTERFILE)) {
                try {
                    await File.WriteAllTextAsync(FILTERFILE, GITHUBFILTER);
                    DebugFile.INSERT($"[ProcessListManager] Local Process Filter created.");
                } catch { DebugFile.INSERT($"[ProcessListManager] Local Process Filter creation failed."); }
            }

            if (File.Exists(FILTERFILE)) {
                try {
                    string LOCALFILTER = await File.ReadAllTextAsync(FILTERFILE);

                    if (GITHUBFILTER is not null) {
                        if (LOCALFILTER == GITHUBFILTER) {
                            var FILTER = LOCALFILTER.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            PROCESSFILTER = new HashSet<string>(FILTER, StringComparer.OrdinalIgnoreCase);

                            DebugFile.INSERT($"[ProcessListManager] Local Process Filter is up to date.");
                            DebugFile.INSERT($"[ProcessListManager] Local Process Filter loaded.");
                        } else {
                            await File.WriteAllTextAsync(FILTERFILE, GITHUBFILTER);
                            DebugFile.INSERT($"[ProcessListManager] Local Process Filter updated.");

                            var FILTER = GITHUBFILTER.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            PROCESSFILTER = new HashSet<string>(FILTER, StringComparer.OrdinalIgnoreCase);

                            DebugFile.INSERT($"[ProcessListManager] Local Process Filter loaded.");
                        }
                    } else {
                        var FILTER = LOCALFILTER.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        PROCESSFILTER = new HashSet<string>(FILTER, StringComparer.OrdinalIgnoreCase);

                        DebugFile.INSERT($"[ProcessListManager] Local Process Filter could not be updated.");
                        if (FILTER.Length > 0) DebugFile.INSERT($"[ProcessListManager] Local Process Filter loaded."); 
                        else DebugFile.INSERT($"[ProcessListManager] Local Process Filter is empty.");

                        MessageBox.Show("Local Process Filter update failed.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                } catch {}
            }
        }

        public static async Task SHOW(MainWindow MAINWINDOW) {
            CLEARPROCESSLIST(MAINWINDOW);
            if (PROCESSFILTER == null) await UPDATEPROCESSFILTER();
            await CREATE(MAINWINDOW);
        }

        public static async Task REFRESH(object SENDER, RoutedEventArgs ROUTEDEVENTARGS, MainWindow MAINWINDOW) {
            CLEARPROCESSLIST(MAINWINDOW);
            if (PROCESSFILTER == null) await UPDATEPROCESSFILTER();
            await CREATE(MAINWINDOW);
        }

        private static async Task CREATE(MainWindow MAINWINDOW) {
            var PROCESSLIST = new HashSet<int>();

            if (PROCESSFILTER == null) await UPDATEPROCESSFILTER();

            await Task.Run(() => {
                foreach (var PROCESS in Process.GetProcesses()) {
                    if (PROCESSFILTER != null && PROCESSFILTER.Contains(PROCESS.ProcessName) || PROCESSLIST.Contains(PROCESS.Id)) continue;

                    var PARENTPROCESS = PROCESS.GETPARENTPROCESS();
                    if (PARENTPROCESS != null && PROCESSLIST.Contains(PARENTPROCESS.Id)) continue;

                    var PROCESSINFO = new ProcessInfoClass {
                        PROCESSID = PROCESS.Id,
                        PROCESSNAME = PROCESS.ProcessName,
                        WINDOWTITLE = PROCESS.MainWindowTitle,
                        ICON = PROCESS.GETICON() ?? SystemIcons.Application
                    };

                    Application.Current.Dispatcher.Invoke(() => {
                        var ITEM = new ListBoxItem {
                            Content = $"{PROCESSINFO.PROCESSNAME} (Process ID: {PROCESSINFO.PROCESSID})",
                            Tag = PROCESSINFO
                        };

                        var GRID = new Grid();
                        GRID.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                        GRID.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var BITMAP = PROCESSINFO.ICON.ToBitmap();
                        var IMAGE = new System.Windows.Controls.Image { Source = BITMAP.BITMAPASIMAGESOURCE(), Width = 32, Height = 32 };

                        Grid.SetColumn(IMAGE, 0);
                        GRID.Children.Add(IMAGE);

                        var PID = new TextBlock {
                            Text = $"{PROCESSINFO.PROCESSNAME} (Process ID: {PROCESSINFO.PROCESSID})",
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontFamily = new System.Windows.Media.FontFamily("Global Monospace"),
                            Foreground = new SolidColorBrush(Colors.White),
                        };

                        Grid.SetColumn(PID, 1);
                        GRID.Children.Add(PID);

                        ITEM.Content = GRID;
                        ITEM.Tag = PROCESSINFO;
                        MAINWINDOW.PROCESSLIST.Items.Add(ITEM);

                        PROCESSLIST.Add(PROCESS.Id);
                    });
                }
            });
        }
    }
}
