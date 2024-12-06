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

public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string? ProcessName { get; set; }
    public string? WindowTitle { get; set; }
    public Icon? Icon { get; set; }
}

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
public static class ProcessExtensions
{
    [DllImport("Kernel32.dll")]
    private static extern uint QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

    public static string? GetMainModuleFileName(this Process process, int buffer = 1024)
    {
        var fileNameBuilder = new StringBuilder(buffer);
        uint bufferLength = (uint)fileNameBuilder.Capacity + 1;
        return QueryFullProcessImageName(process.Handle, 0, fileNameBuilder, ref bufferLength) != 0 ?
            fileNameBuilder.ToString() :
            null;
    }

    public static Icon GetIcon(this Process process)
    {
        try
        {
            string mainModuleFileName = process.GetMainModuleFileName();
            return Icon.ExtractAssociatedIcon(mainModuleFileName);
        }
        catch
        {
            return null;
        }
    }

    public static Process GetParentProcess(this Process process)
    {
        try
        {
            int parentPid = 0;
            int processPid = process.Id;
            using (ManagementObject mo = new ManagementObject($"win32_process.handle='{processPid}'"))
            {
                mo.Get();
                parentPid = Convert.ToInt32(mo["ParentProcessId"]);
            }
            return Process.GetProcessById(parentPid);
        }
        catch
        {
            return null;
        }
    }
}

public static class IconUtilities
{
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    public static ImageSource ToImageSource(this Bitmap icon)
    {
        IntPtr hBitmap = icon.GetHbitmap();

        ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
            hBitmap,
            IntPtr.Zero,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        if (!DeleteObject(hBitmap))
        {
            throw new Win32Exception();
        }

        return wpfBitmap;
    }
}

namespace ParoxInjector.Classes
{
    internal class ProcessListManager
    {
        public static void ShowProcessList(MainWindow mainWindow)
        {
            mainWindow.ProcessList.Items.Clear();
            var ProcessManager = new ProcessListManager();
            ProcessManager.CreateProcessList(mainWindow);
        }

        public static void RefreshProcessList(object sender, RoutedEventArgs e, MainWindow mainWindow)
        {
            mainWindow.ProcessList.Items.Clear();
            var ProcessManager = new ProcessListManager();
            ProcessManager.CreateProcessList(mainWindow);
        }

        private async void CreateProcessList(MainWindow mainWindow)
        {
            var addedProcessIds = new HashSet<int>();

            await Task.Run(() =>
            {
                foreach (var process in Process.GetProcesses())
                {
                    if (AddProcessQ(process.ProcessName) || addedProcessIds.Contains(process.Id))
                    {
                        continue;
                    }

                    var parentProcess = process.GetParentProcess();
                    if (parentProcess != null && addedProcessIds.Contains(parentProcess.Id))
                    {
                        continue;
                    }

                    var processInfo = new ProcessInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        WindowTitle = process.MainWindowTitle,
                        Icon = process.GetIcon() ?? SystemIcons.Application
                    };

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var item = new ListBoxItem
                        {
                            Content = $"{processInfo.ProcessName} (Process ID: {processInfo.ProcessId})",
                            Tag = processInfo
                        };

                        var grid = new Grid();
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var bitmap = processInfo.Icon.ToBitmap();
                        var image = new System.Windows.Controls.Image
                        {
                            Source = bitmap.ToImageSource(),
                            Width = 32,
                            Height = 32
                        };
                        Grid.SetColumn(image, 0);
                        grid.Children.Add(image);

                        var textBlock = new TextBlock
                        {
                            Text = $"{processInfo.ProcessName} (Process ID: {processInfo.ProcessId})",
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontFamily = new System.Windows.Media.FontFamily("Global Monospace"),
                            Foreground = new SolidColorBrush(Colors.White),
                        };
                        Grid.SetColumn(textBlock, 1);
                        grid.Children.Add(textBlock);

                        item.Content = grid;
                        item.Tag = processInfo;
                        mainWindow.ProcessList.Items.Add(item);

                        addedProcessIds.Add(process.Id);
                    });
                }
            });
        }

        private static bool AddProcessQ(string processName)
        {
            string[] excludedProcesses = new string[]
            {
        "TextInputHost","ApplicationFrameHost","StartMenuExperienceHost","ShellExperienceHost","conhost","dllhost","sihost","symsrvhost","svchost","vshost","RuntimeBroker","SgrmBroker","UserOOBEBroker","explorer","Idle","SearchUI","System","Secure System","SystemSettings","VcxprojReader","ctfmon","nvsphelper64","SearchApp","SecurityHealthSystray","SecurityHealthService","taskhostw","System","Secure System","Registry","smss","csrss","wininit","services","LsaIso","lsass","winlogon","fontdrvhost","WUDFHost","dwm","dasHost","spoolsrv","warp-svc","pservice","GameManagerService","RtkBtManServ","MsMpEng","MpDefenderCoreService","Memory Compression","audiodg","spoolsv","smartscreen","vcpkgsrv","vctip","MoUsoCoreWorker",
        "Cloudflare WARP","CrashHandler","Discord","devenv","GameBarPresenceWriter","GameOverlayUI","gamingservices","gamingservicesnet","ParoxInjector","parsecd","msedgewebview2","NisSrv","nvcontainer","NVDisplay.Container","NVIDIA Overlay","Razer Synapse Service","Razer Central","Razer Synapse 3","RazerCentralService","RobloxCrashHandler","RobloxPlayer","RobloxPlayerBeta","RzAppManager","RzBTLEManager","RzChromaConnectServer","RzChromaStreamServer","RzDeviceManager","RzDiagnostic","RzIoTDeviceManager","RzSDKServer","RzSDKService","RzSmartLightingDeviceManager","Spotify","steam","steamservice","steamwebhelper","wallpaper32","Windows10Universal","xgamehelper",
        "CefSharp.BrowserSubprocess","StandardCollector.Service","ServiceHub.Host.AnyCPU","ServiceHub.DataWarehouseHost","ServiceHub.IndexingService","ServiceHub.ThreadedWaitDialog","ServiceHub.RoslynCodeAnalysisService","ServiceHub.IntellicodeModelService","ServiceHub.TestWindowStoreHost","ServiceHub.SettingsHost","ServiceHub.IdentityHost","ServiceHub.VSDetouredHost","MpCmdRun","Microsoft.ServiceHub.Controller","ServiceHub.Host.dotnet.x64",
        "PerfWatson2",
        "WmiPrvSE","CompPkgSrv","MSBuild","VBCSCompiler"
            };

            return Array.Exists(excludedProcesses, p => p.Equals(processName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
