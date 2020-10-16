﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Kebler.Const;
using Kebler.Services;

namespace Kebler.Update
{
    public partial class App : Application
    {
        public static StringBuilder BUILDER = new StringBuilder();

        private App()
        {
            try
            {
                var module = Process.GetCurrentProcess()?.MainModule;
                var path = module?.FileName;

                Log($"Current Path: {path}");

                if (path.Equals(ConstStrings.InstallerExePath))
                {
                    Log("Try start from Temp");
                    Directory.CreateDirectory(ConstStrings.TempInstallerFolder);
                    File.Copy(ConstStrings.InstallerExePath, ConstStrings.TempInstallerExePath, true);

                    using (var process = new Process())
                    {
                        var info = new ProcessStartInfo
                        {
                            FileName = ConstStrings.TempInstallerExePath,
                            UseShellExecute = true,
                            CreateNoWindow = true
                        };

                        process.StartInfo = info;
                        process.EnableRaisingEvents = false;
                        process.Start();
                    }

                    Log("Started Temp");
                    Current.Shutdown();
                    Environment.Exit(0);
                    return;
                }
                else
                {
                    Log($"Go for Update from {path}");

                    Console.WriteLine("CheckUpdate");
                    new MainWindow().Show();
                }
            }
            catch (Exception ex)
            {
                var ss = new StringBuilder();
                ss.Append(ex.Message);
                ss.Append(ex);
                ss.Append(ex.StackTrace);
                Log(ss.ToString());
                DONE(false);
            }
        }

        public static void Log(string msg)
        {
            BUILDER.Append(msg + Environment.NewLine);
        }

       

        static void startKebler()
        {
            Process.Start(new ProcessStartInfo()
            {
                WorkingDirectory = ConstStrings.KeblerAppFolderPath,
                FileName = ConstStrings.KeblerExepath,
                Arguments = ConstStrings.KeblerAppFolderPath,
                UseShellExecute = true,
            });
        }

        public static void CreateShortcut()
        {
            var lnkFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{nameof(Kebler)}.lnk");
            Shortcut.Create(lnkFileName, ConstStrings.KeblerExepath,
                ConstStrings.KeblerAppFolderPath, ConstStrings.KeblerAppFolderPath, "", null, ConstStrings.KeblerExepath);
        }


        public static void DONE(bool isTrue)
        {
            if (isTrue == false)
            {
                File.AppendAllText("install.log", BUILDER.ToString());
            }
            else if (isTrue == true)
            {
                CreateShortcut();
                startKebler();
            }
            JUSTDIEMOTHERFUCKER();

        }

        private static void JUSTDIEMOTHERFUCKER()
        {
            Current.Shutdown();
            Environment.Exit(0);
        }
    }
}