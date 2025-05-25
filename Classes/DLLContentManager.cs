using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class DLL {
    public string? NAME { get; set; }
    public string? PATH { get; set; }
}

public class DLLCOLLECTION {
    public List<DLL>? DLL_LIST { get; set; }
}

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
namespace ParoxInjector.Classes {
    public class DLLContentManager {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        public static void LOAD(object sender, RoutedEventArgs e, MainWindow MAINWINDOW) {
            OpenFileDialog DIALOG = new OpenFileDialog { Filter = "DLL files (*.dll)|*.dll", Title = "Select a DLL file" };

            if (DIALOG.ShowDialog() is true) {
                string DLLPATH = DIALOG.FileName;
                MAINWINDOW.DLLPATH.Text = DLLPATH;

                MAINWINDOW.DLLICON.Source = Imaging.CreateBitmapSourceFromHIcon(
                    System.Drawing.Icon.ExtractAssociatedIcon(DLLPATH).Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
                string DLLNAME = Path.GetFileName(DLLPATH);
                MAINWINDOW.DLLNAME.Text = DLLNAME;

                ProcessListManager.SHOW(MAINWINDOW);
                ADDRECENT(DLLNAME, DLLPATH, MAINWINDOW);
            }
        }

        public static void REFRESH(MainWindow MAINWINDOW) {
            try {
                if (!File.Exists("RecentDLLs.json")) {
                    var NEWCOLLECTION = new DLLCOLLECTION { DLL_LIST = new List<DLL>() };
                    string NEWJSON = JsonConvert.SerializeObject(NEWCOLLECTION, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText("RecentDLLs.json", NEWJSON);
                }

                string JSON = File.ReadAllText("RecentDLLs.json");
                DLLCOLLECTION? COLLECTION = JsonConvert.DeserializeObject<DLLCOLLECTION>(JSON);

                var REMOVECOLLECTION = new List<UIElement>();
                foreach (UIElement ELEMENT in MAINWINDOW.RECENTDLLCONTAINER.Children) if (ELEMENT is Button || ELEMENT is StackPanel) REMOVECOLLECTION.Add(ELEMENT);
                foreach (var ITEM in REMOVECOLLECTION) MAINWINDOW.RECENTDLLCONTAINER.Children.Remove(ITEM);

                if (COLLECTION is not null && COLLECTION.DLL_LIST is not null) {
                    for (int i = 0; i < COLLECTION.DLL_LIST.Count(); i++) {
                        if (!File.Exists(COLLECTION.DLL_LIST[i].PATH)) {
                            DebugFile.INSERT($"[DLLContentManager] \"{COLLECTION.DLL_LIST[i].NAME}\" could not be found at {COLLECTION.DLL_LIST[i].PATH}\n[DLLContentManager] Removing \"{COLLECTION.DLL_LIST[i].NAME}\" from RecentDLLs.json {DateTime.Now}");
                            COLLECTION.DLL_LIST.Remove(COLLECTION.DLL_LIST[i]);
                            File.WriteAllText("RecentDLLs.json", JsonConvert.SerializeObject(COLLECTION, Newtonsoft.Json.Formatting.Indented));
                            continue;
                        }

                        StackPanel PANEL = new StackPanel { Orientation = Orientation.Horizontal };

                        Image DLLICON = new Image { 
                            Source = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.Icon.ExtractAssociatedIcon(COLLECTION.DLL_LIST[i].PATH).Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()),
                            Width = 32,
                            Height = 32,
                        };

                        TextBlock DLLNAME = new TextBlock {
                            Text = COLLECTION.DLL_LIST[i].NAME,
                            FontFamily = new FontFamily("Global Monospace"),
                            Foreground = new SolidColorBrush(Colors.White),
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        PANEL.Children.Add(DLLICON);
                        PANEL.Children.Add(DLLNAME);

                        Button DLL_LOADBUTTON = new Button {
                            Content = PANEL,
                            Background = Brushes.Transparent,
                            BorderBrush = Brushes.Transparent,
                            Tag = COLLECTION.DLL_LIST[i].PATH,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                        };

                        DLL_LOADBUTTON.Click += (SENDER, ROUTEDEVENTARGS) => LOADRECENT(SENDER, ROUTEDEVENTARGS, MAINWINDOW);

                        MAINWINDOW.RECENTDLLCONTAINER.Children.Add(DLL_LOADBUTTON);
                    }
                } else DebugFile.INSERT($"[DLLContentManager] Could not find recent DLLs. {DateTime.Now}");
            } catch (Exception ex) {
                DebugFile.INSERT($"[DLLContentManager] Failed to fetch Recent DLLs (WARNING: MED) {DateTime.Now}\n[DLLContentManager] {ex.Message}");
                MessageBox.Show($"Failed to fetch Recent DLLs\nPress \"OK\" to continue.\n{ex.Message}", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void LOADRECENT(object SENDER, RoutedEventArgs ROUTEDEVENTARGS, MainWindow MAINWINDOW)
        {
            Button? BUTTON = SENDER as Button;
            if (BUTTON != null & File.Exists("RecentDLLs.json"))
            {
                string? JSON = File.ReadAllText("RecentDLLs.json");
                DLLCOLLECTION? COLLECTION = JsonConvert.DeserializeObject<DLLCOLLECTION>(JSON) ?? new DLLCOLLECTION { DLL_LIST = new List<DLL>() };
                foreach (var DLL in COLLECTION.DLL_LIST) if (!File.Exists(DLL.PATH)) { REFRESH(MAINWINDOW); return; }

                string? DLLPATH = BUTTON.Tag.ToString();
                MAINWINDOW.DLLPATH.Text = DLLPATH;

                MAINWINDOW.DLLICON.Source = Imaging.CreateBitmapSourceFromHIcon(
                    System.Drawing.Icon.ExtractAssociatedIcon(DLLPATH).Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
                MAINWINDOW.DLLNAME.Text = Path.GetFileName(DLLPATH);

                ProcessListManager.SHOW(MAINWINDOW);
            }
        }

        public static void SAVERECENT(DLLCOLLECTION COLLECTION, MainWindow MAINWINDOW) {
            try {
                string JSON = JsonConvert.SerializeObject(COLLECTION, Formatting.Indented);
                File.WriteAllText("RecentDLLs.json", JSON);
                REFRESH(MAINWINDOW);
            } catch (Exception EXCEPTION) {
                DebugFile.INSERT($"[DLLContentManager] Failed to save Recent DLL {DateTime.Now}\n[DLLContentManager] {EXCEPTION.Message}");
                MessageBox.Show("Failed to save DLL\nPress \"OK\" to continue.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void ADDRECENT(string NAME, string PATH, MainWindow MAINWINDOW) {
            try {
                if (!File.Exists("RecentDLLs.json")) {
                    var NEWCOLLECTION = new DLLCOLLECTION { DLL_LIST = new List<DLL>() };
                    string NEWJSON = JsonConvert.SerializeObject(NEWCOLLECTION, Formatting.Indented);
                    File.WriteAllText("RecentDLLs.json", NEWJSON);
                }

                string JSON = File.ReadAllText("RecentDLLs.json");
                DLLCOLLECTION COLLECTION = JsonConvert.DeserializeObject<DLLCOLLECTION>(JSON) ?? new DLLCOLLECTION { DLL_LIST = new List<DLL>() };

                foreach (var DLL in COLLECTION.DLL_LIST) if (DLL.PATH == PATH) return;
                COLLECTION.DLL_LIST.Add(new DLL { NAME = NAME, PATH = PATH });

                SAVERECENT(COLLECTION, MAINWINDOW);
            } catch (Exception EXCEPTION) { DebugFile.INSERT($"[DLLContentManager] Failed to write recent DLL \"{NAME}\" to RecentDLLs.json {DateTime.Now}\n[DLLContentManager] {EXCEPTION.Message}"); }
        }
    }
}
