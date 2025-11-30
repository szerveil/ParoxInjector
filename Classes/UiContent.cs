using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class CollectionFragment
{
    public string? Name { get; set; }
    public string? Path { get; set; }
}

public class Collection
{
    public List<CollectionFragment>? Files { get; set; }
}

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
namespace ParoxInjector.Classes
{
    public class UiContent
    {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        public static async Task load(object sender, RoutedEventArgs e, MainWindow Window)
        {
            OpenFileDialog DIALOG = new OpenFileDialog { Filter = "DLL files (*.dll)|*.dll", Title = "Select a DLL file" };

            if (DIALOG.ShowDialog() is true)
            {
                string PATH = DIALOG.FileName;
                Window.DLLPath.Text = PATH;

                string Name = Path.GetFileName(PATH);
                Window.DLLName.Text = Name;

                SaveRecent(Name, PATH, Window);
                await FilterClass.Refresh(Window);
            }
        }

        public static void REFRESH(MainWindow MAINWINDOW)
        {
            string? nCOLLECTION;
            nCOLLECTION = ParoxIO.read(ParoxIO.CollectionFragmentsPath);
            Collection? COLLECTION = JsonConvert.DeserializeObject<Collection>(nCOLLECTION);
            if (COLLECTION.Files == null) { COLLECTION.Files = new List<CollectionFragment>(); }

            try
            {
                var UiCOLLECTION = new List<UIElement>();
                foreach (UIElement ELEMENT in MAINWINDOW.RECENTDLLCONTAINER.Children) if (ELEMENT is Button || ELEMENT is StackPanel) UiCOLLECTION.Add(ELEMENT);
                foreach (var ITEM in UiCOLLECTION) MAINWINDOW.RECENTDLLCONTAINER.Children.Remove(ITEM);

                if (COLLECTION != null && COLLECTION.Files != null)
                {
                    for (int Index = COLLECTION.Files.Count; Index >= 0; Index--)
                    {
                        var IndexedFile = COLLECTION.Files[Index];
                        if (IndexedFile?.Path == null || !File.Exists(IndexedFile.Path))
                        {
                            DBUG.INSERT($"[DLLContentManager] \"{COLLECTION.Files[Index].Name}\" could not be found at {COLLECTION.Files[Index].Path}\n[DLLContentManager] Removing \"{COLLECTION.Files[Index].Name}\" from RDLLS.JSON", DEBUGLOGLEVEL.WARNING);
                            COLLECTION.Files.RemoveAt(Index);
                            continue;
                        }

                        ParoxIO.write(ParoxIO.CollectionFragmentsPath, JsonConvert.SerializeObject(COLLECTION));

                        StackPanel PANEL = new StackPanel
                        { Orientation = Orientation.Horizontal };

                        Image ICON = new Image
                        { Source = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.Icon.ExtractAssociatedIcon(COLLECTION.Files[Index].Path).Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()), Width = 32, Height = 32, };

                        TextBlock TEXT = new TextBlock
                        { Text = COLLECTION.Files[Index].Name, FontFamily = new FontFamily("Global Monospace"), Foreground = new SolidColorBrush(Colors.White), VerticalAlignment = VerticalAlignment.Center, };

                        PANEL.Children.Add(ICON);
                        PANEL.Children.Add(TEXT);

                        Button DLL_LOADBUTTON = new Button
                        {
                            Content = PANEL,
                            Background = Brushes.Transparent,
                            BorderBrush = Brushes.Transparent,
                            Tag = COLLECTION.Files[Index].Path,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                        };

                        DLL_LOADBUTTON.Click += (SENDER, ROUTEDEVENTARGS) => LOADRECENT(SENDER, ROUTEDEVENTARGS, MAINWINDOW);

                        MAINWINDOW.RECENTDLLCONTAINER.Children.Add(DLL_LOADBUTTON);
                    }
                }
                else DBUG.INSERT($"[DLLContentManager] Could not find RDLLS.", DEBUGLOGLEVEL.INFO);
            }
            catch (Exception EXCEPTION)
            {
                DBUG.INSERT($"[DLLContentManager] Failed to fetch RDLLS.", DEBUGLOGLEVEL.WARNING, EXCEPTION);
                MessageBox.Show($"Failed to fetch RDLLS.\nPress \"OK\" to continue.\n{EXCEPTION.Message}", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static async void LOADRECENT(object SENDER, RoutedEventArgs ROUTEDEVENTARGS, MainWindow MAINWINDOW)
        {
            Button? BUTTON = SENDER as Button;
            if (BUTTON != null & File.Exists(ParoxIO.CollectionFragmentsPath))
            {
                string? JSON = ParoxIO.read(ParoxIO.CollectionFragmentsPath);
                Collection? COLLECTION = JsonConvert.DeserializeObject<Collection>(JSON) ?? new Collection { Files = new List<CollectionFragment>() };
                if (COLLECTION.Files?.Count == 0) return;
                foreach (var DLL in COLLECTION.Files) if (!File.Exists(DLL.Path)) { REFRESH(MAINWINDOW); return; }

                string? DLLPATH = BUTTON?.Tag.ToString();
                MAINWINDOW.DLLPath.Text = DLLPATH;
                MAINWINDOW.DLLName.Text = Path.GetFileName(DLLPATH);

                await FilterClass.Refresh(MAINWINDOW);
            }
        }

        public static void SAVERECENT(Collection COLLECTION, MainWindow MAINWINDOW)
        {
            try
            {
                string JSON = JsonConvert.SerializeObject(COLLECTION, Formatting.Indented);
                ParoxIO.write(ParoxIO.CollectionFragmentsPath, JSON);
                REFRESH(MAINWINDOW);
            }
            catch (Exception EXCEPTION)
            {
                DBUG.INSERT($"[DLLContentManager] Failed to save DLL.", DEBUGLOGLEVEL.ERROR, EXCEPTION);
                MessageBox.Show("Failed to save DLL.\nPress \"OK\" to continue.", "WARNING", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public static void SaveRecent(string NAME, string PATH, MainWindow MAINWINDOW)
        {
            string? nCOLLECTION;
            nCOLLECTION = ParoxIO.read(ParoxIO.CollectionFragmentsPath);
            Collection? COLLECTION = JsonConvert.DeserializeObject<Collection>(nCOLLECTION);
            if (COLLECTION.Files == null) COLLECTION.Files = new List<CollectionFragment>();

            try
            {
                if (!File.Exists(ParoxIO.CollectionFragmentsPath))
                {
                    var NEWCOLLECTION = new Collection { Files = new List<CollectionFragment>() };
                    string NEWJSON = JsonConvert.SerializeObject(NEWCOLLECTION, Formatting.Indented);
                    ParoxIO.write(ParoxIO.CollectionFragmentsPath, NEWJSON);
                }

                COLLECTION = JsonConvert.DeserializeObject<Collection>(nCOLLECTION) ?? new Collection { Files = new List<CollectionFragment>() };

                if (COLLECTION.Files == null) COLLECTION.Files = new List<CollectionFragment>();
                foreach (var DLL in COLLECTION.Files) if (DLL.Path == PATH) return;
                COLLECTION.Files.Add(new CollectionFragment { Name = NAME, Path = PATH });

                SAVERECENT(COLLECTION, MAINWINDOW);
            }
            catch (Exception EXCEPTION) { DBUG.INSERT($"[DLLContentManager] Write Failed DLL \"{NAME}\" to \"{ParoxIO.CollectionFragmentsPath}\"", DEBUGLOGLEVEL.ERROR, EXCEPTION); }
        }
    }
}
