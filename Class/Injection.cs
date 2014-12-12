﻿/*
    Copyright (C) 2014 LeagueSharp

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

#region

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LeagueSharp.Loader.Data;
using System.Media;
using System.Windows.Forms;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace LeagueSharp.Loader.Class
{
    public static class Injection
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public delegate bool InjectDLLDelegate(int processId, string path);

        public delegate void OnInjectDelegate(EventArgs args);

        public static event OnInjectDelegate OnInject;

        private static InjectDLLDelegate injectDLL = null;

        const UInt32 WM_KEYDOWN = 0x0100;
        const UInt32 WM_KEYUP = 0x0101;
        const UInt32 WM_SYSKEYDOWN = 0x0104;
        const UInt32 WM_SYSKEYUP = 0x0105;
        const int VK_F5 = 0x74;

        public static bool IsInjected
        {
            get
            {
                var leagueProcess = GetLeagueProcess();
                if (leagueProcess != null)
                {
                    try
                    {
                        return
                        leagueProcess.Modules.Cast<ProcessModule>()
                            .Any(processModule => processModule.ModuleName == Path.GetFileName(Directories.CoreFilePath));
                    }
                    catch (Exception e)
                    {
                        Utility.Log(LogStatus.Error, "Injector", string.Format("Error - {0}", e.ToString()), Logs.MainLog);
                    }
                }
                return false;
            }
        }

        public static bool Injecterized(Process leagueProcess)
        {
            if (leagueProcess != null)
            {
                try
                {
                    return
                    leagueProcess.Modules.Cast<ProcessModule>()
                        .Any(processModule => processModule.ModuleName == Path.GetFileName(Directories.CoreFilePath));
                }
                catch (Exception e)
                {
                    Utility.Log(LogStatus.Error, "Injector", string.Format("Error - {0}", e.ToString()), Logs.MainLog);
                }
            }
            return false;
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(IntPtr ZeroOnly, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        private static void ResolveInjectDLL()
        {
            var hModule = LoadLibrary(Directories.BootstrapFilePath);
            if (!(hModule != IntPtr.Zero))
            {
                return;
            }
            var procAddress = GetProcAddress(hModule, "_InjectDLL@8");
            if (!(procAddress != IntPtr.Zero))
            {
                return;
            }
            injectDLL =
                Marshal.GetDelegateForFunctionPointer(procAddress, typeof(InjectDLLDelegate)) as InjectDLLDelegate;
        }

        public static IntPtr GetLeagueWnd()
        {
            return FindWindow(IntPtr.Zero, "League of Legends (TM) Client");
        }

        public static Process GetLeagueProcess()
        {
            var processesByName = Process.GetProcessesByName("League of Legends");
            IEnumerable<Process> processes = Process.GetProcesses().ToList().Where(p => p.ProcessName == "League of Legends");
            if (processesByName.Length > 0)
            {
                return processesByName[0];
            }
            return null;
        }

        public static List<Process> GetLeagueProcesses()
        {
            var processesByName = Process.GetProcessesByName("League of Legends");

            if (processesByName.Length > 0)
            {
                return processesByName.ToList();
            }
            return null;
        }

        public static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size++ > 0)
            {
                var builder = new StringBuilder(size);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return String.Empty;
        }

        public static IEnumerable<IntPtr> FindWindowsWithText(string titleText)
        {
            IntPtr found = IntPtr.Zero;
            List<IntPtr> windows = new List<IntPtr>();

            EnumWindows(delegate(IntPtr wnd, IntPtr param)
            {
                if (GetWindowText(wnd).Contains(titleText))
                {
                    windows.Add(wnd);
                }
                return true;
            },
                        IntPtr.Zero);

            return windows;
        }

        private static HashSet<IntPtr> _Injected = new HashSet<IntPtr>();

        public static void Pulse()
        {
            var leagueProcesses = GetLeagueProcesses();

            if (leagueProcesses != null)
            {
                foreach (var leagueProcess in leagueProcesses)
                {
                    try
                    {
                        //Don't inject untill we checked that there are not updates for the loader.
                        if(Updater.Updating || !Updater.CheckedForUpdates)
                            return;

                        if (leagueProcess == null)
                            return;

                        Config.Instance.LeagueOfLegendsExePath = leagueProcess.Modules[0].FileName;
                        if (leagueProcess != null && !Injecterized(leagueProcess) && Updater.UpdateCore(leagueProcess.Modules[0].FileName, true).Item1)
                        {
                            if (injectDLL == null)
                            {
                                ResolveInjectDLL();
                            }

                            var num = injectDLL(leagueProcess.Id, Directories.CoreFilePath)
                                ? 1
                                : 0;



                            IntPtr leagueWnd = leagueProcess.MainWindowHandle;

                            foreach (var assembly in Config.Instance.SelectedProfile.InstalledAssemblies.Where(a => a.InjectChecked))
                            {
                                Injection.LoadAssembly(leagueWnd, assembly);
                            }

                            if (OnInject != null)
                            {
                                OnInject(EventArgs.Empty);
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static void LoadAssembly(IntPtr wnd, LeagueSharpAssembly assembly)
        {
            if (assembly.Type != AssemblyType.Library && assembly.Status == AssemblyStatus.Ready)
            {
                var str = string.Format("load \"{0}\"", assembly.PathToBinary);
                var lParam = new COPYDATASTRUCT { cbData = 1, dwData = str.Length * 2 + 2, lpData = str };
                SendMessage(wnd, 74U, IntPtr.Zero, ref lParam);
            }
        }

        public static void UnloadAssembly(IntPtr wnd, LeagueSharpAssembly assembly)
        {
            if (assembly.Type != AssemblyType.Library && assembly.Status == AssemblyStatus.Ready)
            {
                var str = string.Format("unload \"{0}\"", Path.GetFileName(assembly.PathToBinary));
                var lParam = new COPYDATASTRUCT { cbData = 1, dwData = str.Length * 2 + 2, lpData = str };
                SendMessage(wnd, 74U, IntPtr.Zero, ref lParam);
            }
        }

        public static void SendConfig(IntPtr wnd)
        {
            wnd = wnd != IntPtr.Zero ? wnd : GetLeagueWnd();
            var str = string.Format(
                "{0}{1}{2}{3}", (Config.Instance.Settings.GameSettings[0].SelectedValue == "True") ? "1" : "0",
                (Config.Instance.Settings.GameSettings[3].SelectedValue == "True") ? "1" : "0",
                (Config.Instance.Settings.GameSettings[1].SelectedValue == "True") ? "1" : "0",
                (Config.Instance.Settings.GameSettings[2].SelectedValue == "True") ? "2" : "0");

            var lParam = new COPYDATASTRUCT { cbData = 2, dwData = str.Length * 2 + 2, lpData = str };
            SendMessage(wnd, 74U, IntPtr.Zero, ref lParam);
        }

        public struct COPYDATASTRUCT
        {
            public int cbData;
            public int dwData;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpData;
        }
    }
}