﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Kebler.Const;
using Kebler.Models;
using Kebler.Resources;
using Kebler.Services;
using Kebler.ViewModels;
using Microsoft.AppCenter.Crashes;
using ILog = log4net.ILog;
using LogManager = log4net.LogManager;

namespace Kebler
{
    internal static class Updater
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static async Task CheckUpdates()
        {
            Log.Info("Check updates");
            try
            {
                var current = Assembly.GetExecutingAssembly().GetName().Version;

                var result = await UpdaterApi.Check(ConstStrings.GITHUB_USER, nameof(Kebler), current, ConfigService.Instanse.AllowPreRelease);

                Log.Info($"Current {current} Serv {result.Item2.name}");

                App.Instance.IsUpdateReady = result.Item1;
                if (result.Item1)
                {
                    var mgr = new WindowManager();
                    var dialogres = await mgr.ShowDialogAsync(new MessageBoxViewModel(LocalizationProvider.GetLocalizedValue(nameof(Strings.NewUpdate).Replace("%d", result.Item2.tag_name)), string.Empty,
                        Enums.MessageBoxDilogButtons.YesNo, true));
                    if (dialogres == true) 
                        InstallUpdates();
                }
            }
            catch (Exception ex)
            {
                App.Instance.IsUpdateReady = false;
                Log.Error(ex);
                Crashes.TrackError(ex);
            }
        }

        public static void InstallUpdates()
        {
            Directory.CreateDirectory(ConstStrings.TempInstallerFolder);
            File.Copy(ConstStrings.InstallerExePath, ConstStrings.TempInstallerExePath, true);

            using (Process process = new Process())
            {
                var info = new ProcessStartInfo
                {
                    FileName = ConstStrings.TempInstallerExePath,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };

                process.StartInfo = info;
                process.EnableRaisingEvents = false;
                process.Start();
            }

            Application.Current.Shutdown();
        }
    }
}