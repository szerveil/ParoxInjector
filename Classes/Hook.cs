using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace ParoxInjector.Classes {
    internal class Hooks {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint VirtualAllocEx(nint hProcess, nint lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(nint hProcess, nint lpBaseAddress, byte[] lpBuffer, uint nSize, out nint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint CreateRemoteThread(nint hProcess, nint lpThreadAttributes, uint dwStackSize, nint lpStartAddress, nint lpParameter, uint dwCreationFlags, out nint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GetProcAddress(nint hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(nint hObject);

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x00001000;
        private const uint PAGE_READWRITE = 0x04;

        public void HookProcess(int ProcessID, string DLLPath) {
            IntPtr PROCESSH = OpenProcess(PROCESS_ALL_ACCESS, false, ProcessID);
            if (PROCESSH == IntPtr.Zero) {
                MessageBox.Show("Failed to open process.");
                throw new Exception("Failed.");
            }

            IntPtr BASEADDRESS = VirtualAllocEx(PROCESSH, IntPtr.Zero, (uint)DLLPath.Length, MEM_COMMIT, PAGE_READWRITE);
            if (BASEADDRESS == IntPtr.Zero) {
                MessageBox.Show("Failed to allocate memory.");
                CloseHandle(PROCESSH);
                throw new Exception("Failed.");
            }

            byte[] BYTES = Encoding.ASCII.GetBytes(DLLPath);
            IntPtr BYTESWRITTEN;

            bool RESULT = WriteProcessMemory(PROCESSH, BASEADDRESS, BYTES, (uint)BYTES.Length, out BYTESWRITTEN);
            if (!RESULT) {
                MessageBox.Show("Failed to write to process memory.");
                CloseHandle(PROCESSH);
                throw new Exception("Failed.");
            }

            IntPtr MODULE = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (MODULE == IntPtr.Zero) {
                MessageBox.Show("Failed to get LoadLibrary address.");
                CloseHandle(PROCESSH);
                throw new Exception("Failed.");
            }

            IntPtr THREAD = CreateRemoteThread(PROCESSH, IntPtr.Zero, 0, MODULE, BASEADDRESS, 0, out _);
            if (THREAD == IntPtr.Zero) {
                MessageBox.Show("Failed to create remote thread.");
                CloseHandle(PROCESSH);
                throw new Exception("Failed.");
            }

            CloseHandle(THREAD);
            CloseHandle(PROCESSH);
        }
    }
}
