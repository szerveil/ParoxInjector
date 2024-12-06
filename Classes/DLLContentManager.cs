using System.IO;
using Newtonsoft.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Interop;
using Microsoft.Win32;
using System.Runtime.InteropServices;

public class RecentDLL
{
    public string? Name { get; set; }
    public string? Path { get; set; }
}

public class RecentDLLClass
{
    public List<RecentDLL>? RecentDLLs { get; set; }
}

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
namespace ParoxInjector.Classes
{
    public class DLLContentManager
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        public static void LoadDLL(object sender, RoutedEventArgs e, MainWindow mainWindow)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "DLL files (*.dll)|*.dll",
                Title = "Select a DLL file"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string dllPath = openFileDialog.FileName;
                mainWindow.DLLPath.Text = dllPath;

                mainWindow.DLLIcon.Source = Imaging.CreateBitmapSourceFromHIcon(
                    System.Drawing.Icon.ExtractAssociatedIcon(dllPath).Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
                string dllName = Path.GetFileName(dllPath);
                mainWindow.DLLName.Text = dllName;

                ProcessListManager.ShowProcessList(mainWindow);
                AddRecent(dllName, dllPath, mainWindow);
            }
        }
        public static void RefreshRecents(MainWindow mainWindow)
        {
            try
            {
                if (!File.Exists("RecentDLLs.json"))
                {
                    var initialData = new RecentDLLClass { RecentDLLs = new List<RecentDLL>() };
                    string initialJson = JsonConvert.SerializeObject(initialData, Formatting.Indented);
                    File.WriteAllText("RecentDLLs.json", initialJson);
                }

                string json = File.ReadAllText("RecentDLLs.json");
                RecentDLLClass? data = JsonConvert.DeserializeObject<RecentDLLClass>(json);

                var itemsToRemove = new List<UIElement>();
                foreach (UIElement element in mainWindow.RecentDLLContainer.Children)
                {
                    if (element is Button || element is StackPanel)
                    {
                        itemsToRemove.Add(element);
                    }
                }
                foreach (var item in itemsToRemove)
                {
                    mainWindow.RecentDLLContainer.Children.Remove(item);
                }

                if (data != null && data.RecentDLLs != null)
                {
                    for (int dllIndex = 0; dllIndex < data.RecentDLLs.Count(); dllIndex++)
                    {
                        if(!File.Exists(data.RecentDLLs[dllIndex].Path))
                        {
                            DebugFile.Insert($"[DLLContentManager] \"{data.RecentDLLs[dllIndex].Name}\" could not be found at {data.RecentDLLs[dllIndex].Path}\n[DLLContentManager] Removing \"{data.RecentDLLs[dllIndex].Name}\" from RecentDLLs.json {DateTime.Now}");
                            data.RecentDLLs.Remove(data.RecentDLLs[dllIndex]);
                            File.WriteAllText("RecentDLLs.json", JsonConvert.SerializeObject(data, Formatting.Indented));
                            continue;
                        }
                        StackPanel contentPanel = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                        };

                        Image dllIcon = new Image
                        {
                            Source = Imaging.CreateBitmapSourceFromHIcon(
                                System.Drawing.Icon.ExtractAssociatedIcon(data.RecentDLLs[dllIndex].Path).Handle,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions()
                            ),
                            Width = 32,
                            Height = 32,
                        };

                        TextBlock dllName = new TextBlock
                        {
                            Text = data.RecentDLLs[dllIndex].Name,
                            FontFamily = new FontFamily("Global Monospace"),
                            Foreground = new SolidColorBrush(Colors.White),
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        contentPanel.Children.Add(dllIcon);
                        contentPanel.Children.Add(dllName);

                        Button dllButton = new Button
                        {
                            Content = contentPanel,
                            Background = Brushes.Transparent,
                            BorderBrush = Brushes.Transparent,
                            Tag = data.RecentDLLs[dllIndex].Path,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                        };
                        dllButton.Click += (s, e) => LoadRecentDLL(s, e, mainWindow);

                        mainWindow.RecentDLLContainer.Children.Add(dllButton);
                    }
                }
                else
                {
                    DebugFile.Insert($"[DLLContentManager] Could not find recent DLLs. {DateTime.Now}");
                }
            }
            catch (Exception ex)
            {
                DebugFile.Insert($"[DLLContentManager] Failed to fetch Recent DLLs (WARNING: MED) {DateTime.Now}\n[DLLContentManager] {ex.Message}");
                MessageBox.Show($"Failed to fetch Recent DLLs\nPress \"OK\" to continue.\n{ex.Message}", "WARNING");
            }
        }

        public static void LoadRecentDLL(object sender, RoutedEventArgs e, MainWindow mainWindow)
        {
            Button? clickedButton = sender as Button;
            if (clickedButton != null & File.Exists("RecentDLLs.json"))
            {
                string? json = File.ReadAllText("RecentDLLs.json");
                RecentDLLClass? data = JsonConvert.DeserializeObject<RecentDLLClass>(json) ?? new RecentDLLClass { RecentDLLs = new List<RecentDLL>() };
                foreach (var dll in data.RecentDLLs)
                {
                    if(!File.Exists(dll.Path))
                    {
                        RefreshRecents(mainWindow);
                        return;
                    }
                }

                string? dllPath = clickedButton.Tag.ToString();
                mainWindow.DLLPath.Text = dllPath;

                mainWindow.DLLIcon.Source = Imaging.CreateBitmapSourceFromHIcon(
                    System.Drawing.Icon.ExtractAssociatedIcon(dllPath).Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
                mainWindow.DLLName.Text = System.IO.Path.GetFileName(dllPath);

                ProcessListManager.ShowProcessList(mainWindow);
            }
        }

        public static void SaveRecent(RecentDLLClass data, MainWindow mainWindow)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText("RecentDLLs.json", json);
                RefreshRecents(mainWindow);
            }
            catch (Exception ex)
            {
                DebugFile.Insert($"[DLLContentManager] Failed to save Recent DLL {DateTime.Now}\n[DLLContentManager] {ex.Message}");
                MessageBox.Show("Failed to save Recent DLL\nPress \"OK\" to continue.", "WARNING");
            }
        }

        public static void AddRecent(string name, string path, MainWindow mainWindow)
        {
            try
            {
                if (!File.Exists("RecentDLLs.json"))
                {
                    var initialData = new RecentDLLClass { RecentDLLs = new List<RecentDLL>() };
                    string initialJson = JsonConvert.SerializeObject(initialData, Formatting.Indented);
                    File.WriteAllText("RecentDLLs.json", initialJson);
                }

                string json = File.ReadAllText("RecentDLLs.json");
                RecentDLLClass data = JsonConvert.DeserializeObject<RecentDLLClass>(json) ?? new RecentDLLClass { RecentDLLs = new List<RecentDLL>() };

                foreach (var dll in data.RecentDLLs)
                {
                    if (dll.Path == path)
                    {
                        return;
                    }
                }
                data.RecentDLLs.Add(new RecentDLL { Name = name, Path = path });

                SaveRecent(data, mainWindow);
            }
            catch (Exception ex)
            {
                DebugFile.Insert($"[DLLContentManager] Failed to write recent DLL \"{name}\" to RecentDLLs.json {DateTime.Now}\n[DLLContentManager] {ex.Message}");
            }
        }
    }
}
