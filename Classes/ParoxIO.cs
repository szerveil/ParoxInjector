using System.IO;

namespace ParoxInjector.Classes {
    internal class ParoxIO {
        public static string CollectionFragmentsPath = "CollectionFragments.json";
        public static string read(string Path) {
            try {
                if(!File.Exists(Path)) File.Create(Path).Close();
                return File.ReadAllText(Path);
            } catch (Exception ex) {
                DBUG.INSERT($"Error reading file: {ex.Message}", DEBUGLOGLEVEL.ERROR, ex);
                return String.Empty;
            }
        }
        public static Task write(string Path, string Content) {
            try {
                File.WriteAllTextAsync(Path, Content);
                return Task.CompletedTask;
            } catch (Exception ex) {
                DBUG.INSERT($"Error writing to file: {ex.Message}", DEBUGLOGLEVEL.ERROR, ex);
                return Task.CompletedTask;
            }
        }
        public static Task append(string Path, string Content) {
            try {
                File.AppendAllTextAsync(Path, Content);
                return Task.CompletedTask;
            } catch (Exception ex) {
                DBUG.INSERT($"Error appending to file: {ex.Message}", DEBUGLOGLEVEL.ERROR, ex);
                return Task.CompletedTask;
            }
        }
        public static string fetchPath(string FileName) { 
            try {
                string DirectoryPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", FileName);
                return DirectoryPath;
            } catch (Exception ex) {
                DBUG.INSERT($"Error getting path: {ex.Message}", DEBUGLOGLEVEL.ERROR, ex);
                return String.Empty;
            }
        }
    }
}
