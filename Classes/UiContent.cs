using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class CollectionFragment { public string? Name { get; set; } public string? Path { get; set; } }
public class Collection { public List<CollectionFragment> Files { get; set; } = new(); }

namespace ParoxInjector.Classes {
    public class UiContent {
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        public static async Task load(object sender, RoutedEventArgs e, MainWindow Window) {
            OpenFileDialog DIALOG = new OpenFileDialog { Filter = "DLL files (*.dll)|*.dll", Title = "Select a DLL file" };

            if (DIALOG.ShowDialog() is true) {
                string PATH = DIALOG.FileName;
                Window.DLLPath.Text = PATH;

                string Name = Path.GetFileName(PATH);
                Window.DLLName.Text = Name;

                SaveRecent(Name, PATH, Window);
                await FilterClass.Refresh(Window);
            }
        }

        public static void REFRESH(MainWindow MAINWINDOW) {
            string eCOLLECTION = ParoxIO.read(ParoxIO.CollectionFragmentsPath);
            Collection COLLECTION = JsonConvert.DeserializeObject<Collection>(eCOLLECTION) ?? new Collection { Files = new List<CollectionFragment>() };
            if (COLLECTION.Files == null) { COLLECTION.Files = new List<CollectionFragment>(); }

            try {
                var UiCOLLECTION = new List<UIElement>();
                foreach (UIElement ELEMENT in MAINWINDOW.RECENTDLLCONTAINER.Children) if (ELEMENT is Button || ELEMENT is StackPanel) UiCOLLECTION.Add(ELEMENT);
                foreach (var ITEM in UiCOLLECTION) MAINWINDOW.RECENTDLLCONTAINER.Children.Remove(ITEM);

                DBUG.INSERT($"Cleared {UiCOLLECTION.Count} Element(s) from recent tab.", DEBUGLOGLEVEL.INFO);
                if (COLLECTION != null && COLLECTION.Files != null && COLLECTION.Files.Count > 0) {
                    DBUG.INSERT($"Refreshing recent tab with {COLLECTION.Files.Count} CollectionFragment(s).", DEBUGLOGLEVEL.INFO);
                    for (int Index = 0; Index < COLLECTION.Files.Count; Index++) {
                        DBUG.INSERT($"Verifying \"{COLLECTION.Files[Index].Name}\"", DEBUGLOGLEVEL.INFO);
                        var IndexedFile = COLLECTION.Files[Index];
                        if (IndexedFile?.Path == null || !File.Exists(IndexedFile.Path)) {
                            DBUG.INSERT($"\"{COLLECTION.Files[Index].Name}\" could not be found at {COLLECTION.Files[Index].Path}", DEBUGLOGLEVEL.WARNING);
                            DBUG.INSERT($"Removing \"{COLLECTION.Files[Index].Name}\" from CollectionFragments\"", DEBUGLOGLEVEL.INFO);
                            COLLECTION.Files.RemoveAt(Index);
                            continue;
                        }

                        ParoxIO.write(ParoxIO.CollectionFragmentsPath, JsonConvert.SerializeObject(COLLECTION));

                        StackPanel PANEL = new StackPanel
                        { Orientation = Orientation.Horizontal };

                        var ICONPath = COLLECTION.Files[Index].Path ?? "";
                        var ICONHandle = System.Drawing.Icon.ExtractAssociatedIcon(ICONPath)?.Handle ?? IntPtr.Zero;

                        Image ICON = new Image
                        { Source = Imaging.CreateBitmapSourceFromHIcon(ICONHandle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()), Width = 32, Height = 32, };

                        TextBlock TEXT = new TextBlock
                        { Text = COLLECTION.Files[Index].Name, FontFamily = new FontFamily("Global Monospace"), Foreground = new SolidColorBrush(Colors.White), VerticalAlignment = VerticalAlignment.Center, };

                        PANEL.Children.Add(ICON);
                        PANEL.Children.Add(TEXT);

                        Button DLL_LOADBUTTON = new Button
                        { Content = PANEL, Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, Tag = COLLECTION.Files[Index].Path, HorizontalAlignment = HorizontalAlignment.Stretch, };

                        DLL_LOADBUTTON.Click += (SENDER, ROUTEDEVENTARGS) => LOADRECENT(SENDER, ROUTEDEVENTARGS, MAINWINDOW);

                        MAINWINDOW.RECENTDLLCONTAINER.Children.Add(DLL_LOADBUTTON);
                    }
                } else DBUG.INSERT($"Failed to refresh recent tab or no CollectionFragment(s) found.", DEBUGLOGLEVEL.INFO); 
            } catch (Exception EXCEPTION) {
                DBUG.INSERT($"Failed to refresh recent tab.", DEBUGLOGLEVEL.WARNING, EXCEPTION);
            }
        }

        public static async void LOADRECENT(object SENDER, RoutedEventArgs ROUTEDEVENTARGS, MainWindow MAINWINDOW) {
            Button? BUTTON = SENDER as Button;
            if (BUTTON != null & File.Exists(ParoxIO.CollectionFragmentsPath)) {
                string? JSON = ParoxIO.read(ParoxIO.CollectionFragmentsPath);
                Collection? COLLECTION = JsonConvert.DeserializeObject<Collection>(JSON) ?? new Collection { Files = new List<CollectionFragment>() };
                if (COLLECTION.Files == null || COLLECTION.Files.Count == 0) return;
                foreach (var DLL in COLLECTION.Files) if (!File.Exists(DLL.Path)) { REFRESH(MAINWINDOW); return; }

                string? DLLPATH = BUTTON?.Tag.ToString();
                MAINWINDOW.DLLPath.Text = DLLPATH;
                MAINWINDOW.DLLName.Text = Path.GetFileName(DLLPATH);

                await FilterClass.Refresh(MAINWINDOW);
            }
        }

        public static void SAVERECENT(Collection COLLECTION, MainWindow MAINWINDOW) {
            try {
                string JSON = JsonConvert.SerializeObject(COLLECTION, Formatting.Indented);
                ParoxIO.write(ParoxIO.CollectionFragmentsPath, JSON);
                REFRESH(MAINWINDOW);
            } catch (Exception EXCEPTION) {
                DBUG.INSERT($"Failed to save dll as a CollectionFragment.", DEBUGLOGLEVEL.ERROR, EXCEPTION);
            }
        }

        public static void SaveRecent(string NAME, string PATH, MainWindow MAINWINDOW) {
            try {
                if (!File.Exists(ParoxIO.CollectionFragmentsPath)) {
                    var NEWCOLLECTION = new Collection { Files = new List<CollectionFragment>() };
                    string NEWJSON = JsonConvert.SerializeObject(NEWCOLLECTION, Formatting.Indented);
                    ParoxIO.write(ParoxIO.CollectionFragmentsPath, NEWJSON);
                }

                string eCOLLECTION = ParoxIO.read(ParoxIO.CollectionFragmentsPath);
                Collection COLLECTION = JsonConvert.DeserializeObject<Collection>(eCOLLECTION) ?? new Collection { Files = new List<CollectionFragment>() };

                foreach (var DLL in COLLECTION.Files) if (DLL.Path == PATH) return;
                COLLECTION.Files.Add(new CollectionFragment { Name = NAME, Path = PATH });

                SAVERECENT(COLLECTION, MAINWINDOW);
            } catch (Exception EXCEPTION) { DBUG.INSERT($"dll \"{NAME}\" failed to save to \"{ParoxIO.CollectionFragmentsPath}\"", DEBUGLOGLEVEL.ERROR, EXCEPTION); }
        }
    }
}
