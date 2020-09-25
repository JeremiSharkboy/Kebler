﻿using Kebler.Const;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Threading;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Kebler.Services;

namespace Kebler.Update
{

    public partial class App : Application
    {

        public StringBuilder BUILDER = new StringBuilder();
        public static App Instance;

        App()
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

                    using (Process process = new Process())
                    {
                        ProcessStartInfo info = new ProcessStartInfo
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
                    return;
                }
                else
                {
                    Log($"Go for Update from {path}");

                    Console.WriteLine("CheckUpdate");
                    HasUpdate();
                }
            }
            catch (Exception ex)
            {
                var ss = new StringBuilder();
                ss.Append(ex.Message);
                ss.Append(ex);
                ss.Append(ex.StackTrace);
                new EXCEPTIONWINDOW(ss.ToString()).ShowDialog();
                Current.Shutdown();
            }

        }


        public void Log(string msg)
        {
            BUILDER.Append(msg + Environment.NewLine);
        }

        public void HasUpdate()
        {
            var getEnv = Environment.GetEnvironmentVariable(nameof(Kebler), EnvironmentVariableTarget.User);
            var current = new Version(FileVersionInfo.GetVersionInfo(getEnv).FileVersion);


            var result = UpdaterApi.Check(ConstStrings.GITHUB_USER, nameof(Kebler), current);
            Log($"Current {current} Serv {result.Item2}");

            Log($"Env Path: {getEnv}");


            if (getEnv == null)
            {
                var updateUrl = UpdaterApi.GetlatestUri();

                var wd = new MainWindow(new Uri(updateUrl));
                wd.ShowDialog();
                Current.Shutdown(0);
            }
            else
            {
                var updateUrl = UpdaterApi.GetlatestUri();

                if (File.Exists(ConstStrings.KeblerExepath))
                {
                    if (result.Item2 > current)
                    {

                        var wd = new MainWindow(new Uri(updateUrl));
                        wd.ShowDialog();
                        Current.Shutdown(0);
                    }
                    else
                    {
                        CreateShortcut();
                        Process.Start(getEnv);
                        Current.Shutdown(0);
                    }
                }
                else
                {
                    var wd = new MainWindow(new Uri(updateUrl));
                    wd.ShowDialog();
                    Current.Shutdown(0);
                }
            }
        }

        public void CreateShortcut()
        {
            var lnkFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Kebler.lnk");
            Shortcut.Create(lnkFileName, ConstStrings.KeblerExepath,
                null, null, nameof(Kebler), null, null);
        }

    }
}
