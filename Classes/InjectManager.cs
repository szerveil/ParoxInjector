using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace ParoxInjector.Classes
{
    internal class InjectManager
    {
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

        public void InjectDLL(int processID, string dllPath)
        {
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processID);
            if (hProcess == IntPtr.Zero)
            {
                MessageBox.Show("Failed to open process.");
                throw new Exception("Failed.");
            }

            IntPtr lpBaseAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllPath.Length, MEM_COMMIT, PAGE_READWRITE);
            if (lpBaseAddress == IntPtr.Zero)
            {
                MessageBox.Show("Failed to allocate memory.");
                CloseHandle(hProcess);
                throw new Exception("Failed.");
            }

            byte[] bytes = Encoding.ASCII.GetBytes(dllPath);
            IntPtr lpNumberOfBytesWritten;
            bool writeResult = WriteProcessMemory(hProcess, lpBaseAddress, bytes, (uint)bytes.Length, out lpNumberOfBytesWritten);
            if (!writeResult)
            {
                MessageBox.Show("Failed to write to process memory.");
                CloseHandle(hProcess);
                throw new Exception("Failed.");
            }

            IntPtr hModule = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (hModule == IntPtr.Zero)
            {
                MessageBox.Show("Failed to get LoadLibrary address.");
                CloseHandle(hProcess);
                throw new Exception("Failed.");
            }

            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, hModule, lpBaseAddress, 0, out _);
            if (hThread == IntPtr.Zero)
            {
                MessageBox.Show("Failed to create remote thread.");
                CloseHandle(hProcess);
                throw new Exception("Failed.");
            }

            CloseHandle(hThread);
            CloseHandle(hProcess);
        }
    }
}
