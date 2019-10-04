﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Kebler.Models;
using Kebler.Services;
using Kebler.UI.Windows;
using log4net;
using log4net.Config;
using SharpConfig;

namespace Kebler
{
    /// <inheritdoc />
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static event EventHandler LanguageChanged;

        private static Models.GlobalConfiguration Configuration;
        private static readonly List<CultureInfo> Languages = new List<CultureInfo>();
        public static readonly ILog Log = LogManager.GetLogger(typeof(App));
        private static Configuration Conf;
        public static UI.Windows.KeblerWindow KeblerControl;

        Mutex myMutex;

        private static CultureInfo Language
        {
            get => System.Threading.Thread.CurrentThread.CurrentUICulture;
            set
            {
                if (value == null) return;
                if (Equals(value, System.Threading.Thread.CurrentThread.CurrentUICulture)) return;

                //1. Меняем язык приложения:
                System.Threading.Thread.CurrentThread.CurrentUICulture = value;

                //2. Создаём ResourceDictionary для новой культуры
                var dict = new ResourceDictionary();
                switch (value.Name)
                {
                    case "ru-RU":
                        dict.Source = new Uri($"Resources/lang.{value.Name}.xaml", UriKind.Relative);
                        break;
                    default:
                        dict.Source = new Uri("Resources/lang.en-US.xaml", UriKind.Relative);
                        break;
                }

                //3. Находим старую ResourceDictionary и удаляем его и добавляем новую ResourceDictionary
                var oldDict = (from d in Current.Resources.MergedDictionaries where d.Source != null && d.Source.OriginalString.StartsWith("Resources/lang.") select d).FirstOrDefault();
                if (oldDict != null)
                {
                    var ind = Current.Resources.MergedDictionaries.IndexOf(oldDict);
                    Current.Resources.MergedDictionaries.Remove(oldDict);
                    Current.Resources.MergedDictionaries.Insert(ind, dict);
                }
                else
                {
                    Current.Resources.MergedDictionaries.Add(dict);
                }

                //4. Вызываем евент для оповещения всех окон.
                LanguageChanged?.Invoke(Current, new EventArgs());
            }
        }



        App()
        {


            var logRepo = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepo, new FileInfo("log4net.config"));

            int hwnd = Win32.FindWindow(null, "Kebler");
            if (hwnd != 0)
            {

                var data = "";
                foreach (var text in Environment.GetCommandLineArgs())
                {
                    data += $"{text} ";
                }

                SendArgs((IntPtr)hwnd, data);
                //App.Current.Shutdown();
            }

            FileAssociations.Create_abc_FileAssociation();

            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;



            //log4net.Config.XmlConfigurator.Configure(null);


            LanguageChanged += App_LanguageChanged;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Dispatcher_UnhandledException;
            Current.Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            InitConfig();
            Languages.Clear();

            foreach (var lang in Data.LangList)
            {
                Languages.Add(new CultureInfo(lang));
            }

            ReadSettings();
        }

        private static void ReadSettings()
        {
            Data.SettingsData = StorageRepository.GetSettingsList();
        }

        public static bool SendArgs(IntPtr targetHWnd, string args)
        {
            var cds = new Win32.CopyDataStruct();
            try
            {
                cds.cbData = (args.Length + 1) * 2;
                cds.lpData = Win32.LocalAlloc(0x40, cds.cbData);
                Marshal.Copy(args.ToCharArray(), 0, cds.lpData, args.Length);
                cds.dwData = (IntPtr)1;
                Win32.SendMessage(targetHWnd, Win32.WM_COPYDATA, IntPtr.Zero, ref cds);
            }
            finally
            {
                cds.Dispose();
            }

            return true;
        }

        #region Events
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!(e.ExceptionObject is Exception exception)) return;
            Log.Error(exception);

        }

        private static void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception);
            e.Handled = true;
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            Language = Configuration.Language == null ? new CultureInfo(Data.LangList[0]) : new CultureInfo(Configuration.Language);

            KeblerControl = new UI.Windows.KeblerWindow();
            KeblerControl.Show();

            base.OnStartup(e);
        }

        private static void App_LanguageChanged(object sender, EventArgs e)
        {
            Log.Info($"Set language: {Language.Name}");
            Conf[nameof(Models.GlobalConfiguration)][nameof(Models.GlobalConfiguration.Language)].StringValue = Language.Name;
            Conf.SaveToFile(Data.ConfigName);
        }

        #endregion


        #region Actions

        private static void InitConfig()
        {
            if (!File.Exists(Data.ConfigName))
                using (File.Create(Data.ConfigName))
                {

                }

            Conf = SharpConfig.Configuration.LoadFromFile(Data.ConfigName);

            Log.Info($"Configuration:{Environment.NewLine}" + PrintConfig(Conf));
            Conf.SaveToFile(Data.ConfigName);
            Configuration = Conf[nameof(Models.GlobalConfiguration)].ToObject<Models.GlobalConfiguration>();
        }

        private static string PrintConfig(Configuration cfg)
        {
            var text = string.Empty;

            foreach (var section in cfg)
            {
                text += $"[{section.Name}]{Environment.NewLine}";

                foreach (var setting in section)
                {
                    text += "  " + Environment.NewLine;

                    if (setting.IsArray)
                        text += $"[Array, {setting.ArraySize} elements] ";

                    text += $"{setting}{Environment.NewLine}";
                }

                text += Environment.NewLine;
            }

            return text;
        }


        #endregion

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Language = Configuration.Language == null ? new CultureInfo(Data.LangList[0]) : new CultureInfo(Configuration.Language);

            KeblerControl = new UI.Windows.KeblerWindow();
            KeblerControl.Show();

            base.OnStartup(e);
        }
    }
}
