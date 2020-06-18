﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Kebler.Models;
using Kebler.Models.PsevdoVM;
using Kebler.Models.Torrent;
using Kebler.Models.Torrent.Args;
using Kebler.Models.Torrent.Response;
using Kebler.Services;
using Kebler.TransmissionCore;
using log4net;
using Microsoft.Win32;

namespace Kebler.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for AddTorrentDialog.xaml
    /// </summary>
    public partial class AddTorrentDialog : INotifyPropertyChanged
    {

        public AddTorrentDialog(string path, ref TransmissionClient transmissionClient)
        {
            _torrentFileInfo = new FileInfo(path);
            TorrentPath = _torrentFileInfo.FullName;
            _transmissionClient = transmissionClient;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            IsAutoStart = true;
            if (!ConfigService.Instanse.IsAddTorrentWindowShow)
            {
                InitializeComponent();
                DataContext = this;
                IsWorking = false;
                IsAddTorrentWindowShow = ConfigService.Instanse.IsAddTorrentWindowShow;
            }
            Result.Visibility = Visibility.Collapsed;
            PeerLimit = ConfigService.Instanse.TorrentPeerLimit;

            Loaded += AddTorrentDialog_Loaded;
            FilesTree.Border = new Thickness(1);
        }

        private async void AddTorrentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.settings = await _transmissionClient.GetSessionSettingsAsync(cancellationToken);
            DownlaodDir = settings.DownloadDirectory;
            await LoadTree();

            LoadingSettingsGrid.Visibility = Visibility.Collapsed;

        }

        private void ChangePath(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Torrent files (*.torrent)|*.torrent|All files (*.*)|*.*",
                Multiselect = false,
                InitialDirectory = _torrentFileInfo.DirectoryName ?? throw new InvalidOperationException()
            };

            if (openFileDialog.ShowDialog() != true) return;

            _torrentFileInfo = new FileInfo(openFileDialog.FileName);
            TorrentPath = _torrentFileInfo.FullName;
        }




        private void Cancel(object sender, RoutedEventArgs e)
        {
            IsWorking = false;
            DialogResult = false;
            cancellationTokenSource.Cancel();
            Close();
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            IsWorking = false;
            cancellationTokenSource.Cancel();
        }

        private async void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded)
                return;
            await LoadTree();

            LoadingSettingsGrid.Visibility = Visibility.Collapsed;
        }

        private async Task LoadTree()
        {
            LoadingSettingsGrid.Visibility = Visibility.Visible;

            var filebytes = await File.ReadAllBytesAsync(TorrentPath, cancellationToken);
            TorrentInfo.TryParse(filebytes, out var parsedTorrent);

            FilesTree.UpdateFilesTree(ref parsedTorrent);
        }







        public void Add(object sender, RoutedEventArgs e)
        {
            Result.Visibility = Visibility.Collapsed;
            IsWorking = true;
            Task.Factory.StartNew(async () =>
            {
                if (!File.Exists(TorrentPath))
                    throw new Exception("Torrent file not found");


                var filebytes = await File.ReadAllBytesAsync(TorrentPath, cancellationToken);


                var encodedData = Convert.ToBase64String(filebytes);
                //string decodedString = Encoding.UTF8.GetString(filebytes);
                var newTorrent = new NewTorrent
                {
                    Metainfo = encodedData,
                    Paused = !IsAutoStart,
                    DownloadDirectory = DownlaodDir ?? settings.DownloadDirectory,
                    PeerLimit = PeerLimit
                };


                //Debug.WriteLine($"Start Adding torrentFileInfo {path}");

                Log.Info($"Start adding torrentFileInfo {newTorrent.Filename}");

                while (true)
                {
                    if (Dispatcher.HasShutdownStarted) return;
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        TorrentResult = await _transmissionClient.TorrentAddAsync(newTorrent, cancellationToken);

                        if (TorrentResult.Result == Enums.ReponseResult.Ok)
                        {

                            switch (TorrentResult.Value.Status)
                            {
                                case Enums.AddTorrentStatus.Added:
                                    {
                                        Log.Info($"Torrent {newTorrent.Filename} added as {TorrentResult.Value.Name}");
                                        IsWorking = false;

                                        ConfigService.Instanse.TorrentPeerLimit = PeerLimit;
                                        ConfigService.Save();
                                        Dispatcher.Invoke(() =>
                                        {
                                            DialogResult = true;
                                            Close();
                                        });
                                        return;
                                    }
                                case Enums.AddTorrentStatus.Duplicate:
                                    {
                                        Log.Info($"Adding torrentFileInfo result '{TorrentResult.Value.Status}' {TorrentResult.Value.Name}");

                                        Dispatcher.Invoke(() =>
                                        {
                                            Result.Content = Kebler.Resources.Dialogs.ATD_TorrentExist;
                                            Result.Visibility = Visibility.Visible;
                                        });
                                        return;
                                    }
                            }

                        }
                        else
                        {
                            Log.Info($"Adding torrentFileInfo result '{TorrentResult.Value.Status}' {newTorrent.Filename}");
                            await Task.Delay(100, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                        return;
                    }
                    finally
                    {
                        IsWorking = false;
                    }
                }
            }, cancellationToken);
        }

        private void ChangeVisibilityWindow(object sender, RoutedEventArgs e)
        {
            ConfigService.Instanse.IsAddTorrentWindowShow = IsAddTorrentWindowShow;
            ConfigService.Save();
        }

        public new void Show()
        {
            throw new Exception("Use ShowDialog instead of Show()");
        }

        private void AddTorrentDialog_OnClosing(object sender, CancelEventArgs e)
        {
            FilesTree = null;
            cancellationTokenSource?.Cancel();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    public partial class AddTorrentDialog
    {
        FileInfo _torrentFileInfo;
        SessionSettings settings;
        TransmissionClient _transmissionClient;
        public AddTorrentResponse TorrentResult;


        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;


        public string TorrentPath { get; set; }
        public string DownlaodDir { get; set; }
        public bool IsWorking { get; set; }
        public bool IsAddTorrentWindowShow { get; set; }
        public int PeerLimit { get; set; }
        public int UploadLimit { get; set; }
        public bool IsAutoStart { get; set; }

        public FilesTreeViewModel FilesTree { get; private set; } = new FilesTreeViewModel();

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        
    }
}
