using System.IO;

namespace ParoxInjector.Classes
{
    internal class DebugFile
    {
        static bool WroteDebug = false;

        public static void Insert(string logString)
        {
            if (!File.Exists("Debug.txt"))
            {
                File.WriteAllText("Debug.txt", logString);
                WroteDebug = true;
                return;
            }

            File.AppendAllText("Debug.txt", $"\n{logString}");
            WroteDebug = true;
        }

        public static void Clear()
        {
            File.Delete("Debug.txt");
        }

        public static bool WasDebugWrote()
        {
            return WroteDebug;
        }
    }
}
