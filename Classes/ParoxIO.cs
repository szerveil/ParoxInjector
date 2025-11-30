using System.IO;

namespace ParoxInjector.Classes {
    internal class ParoxIO {
        public static string CollectionFragmentsPath = "CollectionFragments.json";
        public static string? read(string Path) {
            try {
                if(!File.Exists(Path)) File.Create(Path).Close();
                return File.ReadAllText(Path);
            } catch (Exception ex) {
                DBUG.INSERT($"[IO] Error reading file: {ex.Message}", DEBUGLOGLEVEL.ERROR, ex);
                return null;
            }
        }
        public static Task? write(string Path, string Content) {
            try {
                File.WriteAllTextAsync(Path, Content);
                return Task.CompletedTask;
            } catch (Exception ex) {
                DBUG.INSERT($"[IO] Error writing to file: {ex.Message}", DEBUGLOGLEVEL.ERROR, ex);
                return null;
            }
        }
    }
}
