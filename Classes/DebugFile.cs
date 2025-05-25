using System.IO;

namespace ParoxInjector.Classes {
    internal class DebugFile {
        static bool DEBUGFLAG = false;

        public static void INSERT(string CONTENT) {
            if (!File.Exists("Debug.txt")) {
                File.WriteAllText("Debug.txt", CONTENT);
                DEBUGFLAG = true;
                return;
            }

            File.AppendAllText("Debug.txt", $"\n{CONTENT}");
            DEBUGFLAG = true;
        }

        public static void CLEAR() => File.Delete("Debug.txt");
        public static bool DEBUGC() { return DEBUGFLAG; }
    }
}
