﻿using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Kebler.Services;
using Kebler.Theme.Default;
using Transmission.API.RPC.Entity;

namespace Kebler.UI.Controls
{
    /// <summary>
    /// Interaction logic for TopBarControl.xaml
    /// </summary>
    public partial class TopBarControl : UserControl
    {
       // private readonly string[] _sortValues = {nameof(TorrentInfo.UploadedEver), nameof(TorrentInfo.RateUpload), nameof(TorrentInfo.RateDownload) };

        public TopBarControl()
        {
            InitializeComponent();
        }

      


        private void OpenConnectionManager(object sender, RoutedEventArgs e)
        {
            App.KeblerControl.OpenConnectionManager();
        }

        private void ConnectFirst(object sender, RoutedEventArgs e)
        {
            App.KeblerControl.Connect();
        }

        private void AddTorrent(object sender, RoutedEventArgs e)
        {
            App.KeblerControl.AddTorrent();
        }

        private void SortValMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem mi)) return;

            ConfigService.Instanse.SortVal = mi.Tag.ToString();
            ConfigService.Save();
            App.KeblerControl.UpdateSorting();
        }

        private void SortTypeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem mi)) return;

            int.TryParse(mi.Tag.ToString(), out var val);

            ConfigService.Instanse.SortType = val;

            ConfigService.Save();

            App.KeblerControl.UpdateSorting();
        }

        private void Report(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start https://github.com/JeremiSharkboy/Kebler/issues") { CreateNoWindow = true });
        }

        private void About(object sender, RoutedEventArgs e)
        {
            var dd = new Windows.MessageBox(new About());
            dd.ShowDialog();
        }
    }
}
