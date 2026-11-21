/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
    Modified for Vela branding.
*/

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using BulkCrapUninstaller.Forms;
using BulkCrapUninstaller.Properties;
using Klocman;
using Klocman.Extensions;
using Klocman.Forms;
using Klocman.Forms.Tools;
using Klocman.Tools;
using UninstallTools.Dialogs;

namespace BulkCrapUninstaller
{
    internal static class EntryPoint
    {
        public static bool IsRestarting { get; internal set; }

        private const string MUTEX_NAME = @"Global\Vela-singleinstance";
        private static Mutex _mutex;

        [STAThread]
        public static void Main(string[] args)
        {
            NBugConfigurator.SetupNBug();

            using (LogWriter.StartLogging())
            {
                try
                {
                    Directory.SetCurrentDirectory(Program.AssemblyLocation.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                try
                {
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.EnableVisualStyles();

                    _mutex = new Mutex(true, MUTEX_NAME, out var createdNew);
                    if (!createdNew)
                    {
                        _mutex.Dispose();
                        HandleBeingSecondInstance();
                        return;
                    }

                    SetupDependancies();

                    if(Properties.Settings.Default.WindowDpiAware)
                        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

                    var startupMgr = args.Contains("/startupmanager", StringComparison.OrdinalIgnoreCase) || 
                                     args.Contains("/sm", StringComparison.OrdinalIgnoreCase);

                    if (startupMgr)
                        Application.Run(StartupManagerWindow.ShowManagerWindow());
                    else
                        Application.Run(new MainWindow());
                }
                finally
                {
                    ProcessShutdown();
                }
            }
        }

        private static void ProcessShutdown()
        {
            if (!IsRestarting && !_mutex.SafeWaitHandle.IsClosed)
                _mutex.ReleaseMutex();
            _mutex.Dispose();
        }

        public static void Restart()
        {
            try
            {
                IsRestarting = true;

                _mutex.ReleaseMutex();
                Application.Restart();
            }
            catch (Exception ex)
            {
                PremadeDialogs.GenericError(ex);
                IsRestarting = false;
            }
        }



        private static void HandleBeingSecondInstance()
        {
            try
            {
                var location = Environment.ProcessPath;
                var otherInstance = Process.GetProcesses().FirstOrDefault(x =>
                {
                    try
                    {
                        return x.Id != Environment.ProcessId && x.MainWindowHandle != IntPtr.Zero &&
                               string.Equals(x.MainModule?.FileName, location, StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                });
                var mainWind = otherInstance?.MainWindowHandle;
                if (mainWind != null)
                {
                    try
                    {
                        NativeMethods.SetForegroundWindow(mainWind.Value);
                    }
                    catch (Exception ex)
                    {
                        PremadeDialogs.GenericError(ex);
                    }
                }
                else
                {
                    var title = Program.ProductName + " is already running";
                    CustomMessageBox.ShowDialog(null, new CmbBasicSettings(title, title, "Close the other instance before opening this copy.", Resources.Icon_Logo, "OK"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern int SetForegroundWindow(IntPtr hwnd);
        }

        private static void SetupDependancies()
        {
            // Order is semi-important, prepare settings should go first.
            Program.PrepareSettings();
            CultureConfigurator.SetupCulture();
            //try
            //{
            //    UpdateSystem.ProcessPendingUpdates();
            //}
            //catch (Exception ex)
            //{
            //    PremadeDialogs.GenericError(ex);
            //}
        }
    }
}
