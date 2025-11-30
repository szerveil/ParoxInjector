using System.Diagnostics;
using System.IO;

namespace ParoxInjector.Classes {
    internal class DBUG {
        [Flags]
        public enum DEBUGFLAGS : byte { None = 0, Inserted = 1 }
        static DEBUGFLAGS DEBUGFLAG = DEBUGFLAGS.None;

        public static void INSERT(string MESSAGE, DEBUGLOGLEVEL LOGLEVEL) { INSERT(MESSAGE, LOGLEVEL, null); }

        public static void INSERT(string MESSAGE, DEBUGLOGLEVEL LOGLEVEL, object? EXCEPTIONLOG) {
            var STACK = new StackTrace(1, true);
            var STACKFRAME = STACK.GetFrame(1);
            string DBUGCALLORIGIN = Path.GetFileName(STACKFRAME?.GetFileName() ?? "Unknown.cs");
            if (!File.Exists("DEBUG.txt") || ParoxIO.read("DEBUG.txt") == string.Empty) {
                ParoxIO.append("DEBUG.txt", $"[{LOGLEVEL}] [{DBUGCALLORIGIN}] {MESSAGE} {DateTime.Now}");
                DEBUGFLAG = DEBUGFLAGS.Inserted;
                return;
            }

            string? EXCEPTIONMESSAGE = (EXCEPTIONLOG is Exception EXCEPTION && !string.IsNullOrEmpty(EXCEPTION.Message)) ? EXCEPTIONMESSAGE = EXCEPTION.Message : null;
            if (EXCEPTIONLOG is string STRING && !string.IsNullOrEmpty(STRING)) { EXCEPTIONMESSAGE = STRING; }

            if (EXCEPTIONMESSAGE != null) {
                ParoxIO.append("DEBUG.txt", $"\n[{LOGLEVEL}] [{DBUGCALLORIGIN}] {MESSAGE} {DateTime.Now}\n[{LOGLEVEL}] {EXCEPTIONMESSAGE}");
                DEBUGFLAG = DEBUGFLAGS.Inserted;
                return;
            }

            ParoxIO.append("DEBUG.txt", $"\n[{LOGLEVEL}] [{DBUGCALLORIGIN}] {MESSAGE} {DateTime.Now}");
            DEBUGFLAG = DEBUGFLAGS.Inserted;
        }

        public static void CLEAR() => ParoxIO.write("DEBUG.txt", string.Empty);
        public static DEBUGFLAGS SetWindowDebug() { return DEBUGFLAG; }
    }

    public enum DEBUGLOGLEVEL { INFO, WARNING, ERROR }
}
