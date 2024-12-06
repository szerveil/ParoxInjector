# ParoxInjector
I have tested various times with "TEST" DLLs and actual UI DLLs.

I have implemented to show child processes in some cases like Windows Store apps, to inject a DLL to them you have to inject to the child window as injecting to the direct window will either not work, or crash the process. A child window is defined by the same name as the window with no icon or just a window with no icon and "AppxWindow" or something of the sort.

**There are specific apps that are restricted from injecting to for personal prefrence. If you would like you can build your own version without the restrictions by removing them inside the excludedProcesses list at the bottom of ProcessListManager Class**

Have not had any issues with it so far. Feature requests / Issues in the issues tab if you have any ideas, thanks!

# Preview
![image](https://github.com/user-attachments/assets/4ff3e441-6e02-4fb2-8b28-c11c098732d7)
