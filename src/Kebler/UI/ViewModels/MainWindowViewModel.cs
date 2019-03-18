﻿using Kebler.Models;
using Kebler.Services;
using Kebler.Services.Converters;
using Kebler.UI.Windows;
using Kebler.UI.Windows.Dialogs;
using LiteDB;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Transmission.API.RPC;
using Transmission.API.RPC.Entity;

namespace Kebler.UI.ViewModels
{

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private List<Server> serversList;
        private LiteCollection<Server> dbServers;
        private Statistic stats;
        private SessionInfo sessionInfo;
        private TransmissionClient transmissionClient;
        private ConnectionManager cmWindow;
        private Task whileCycleTask;
        private CancellationTokenSource cancelTokenSource = new CancellationTokenSource();


        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static Dispatcher Dispatcher => Application.Current.Dispatcher;

        public event PropertyChangedEventHandler PropertyChanged;
        public bool IsConnected { get; set; } = false;
        public bool IsConnecting { get; set; } = false;
        public Server ConnectedServer { get; set; }
        public bool IsDoingStuff { get; set; } = false;
        public string LongStatusText { get; set; }

        public ObservableCollection<TorrentInfo> TorrentList { get; set; } = new ObservableCollection<TorrentInfo>();
        public TorrentInfo SelectedTorrent { get; set; }

        public bool IsErrorOccuredWhileConnecting { get; set; } = false;
        public int TorrentDataGridSelectedIndex { get; set; } = 0;


        #region StatusBarProps
        public string IsConnectedStatusText { get; set; } = string.Empty;
        public string DownloadSpeed { get; set; } = string.Empty;
        public string UploadSpeed { get; set; } = string.Empty;


        #endregion

        public MainWindowViewModel()
        {
            Start();
        }
        private void Start()
        {
            Task.Factory.StartNew(() =>
            {
                GetAllServer();
                InitConnection();
            });
        }

        public void InitConnection()
        {
            if (serversList.Count == 0)
            {
                ShowCm();
            }
            else
            {
                new Task(() => { TryConnect(serversList.FirstOrDefault()); }).Start();

            }
        }

        public void AddTorrent(string path)
        {
            new Task(async () =>
            {
                if (!File.Exists(path))
                    throw new Exception("Torrent file not found");

                using (var fstream = File.OpenRead(path))
                {
                    byte[] filebytes = new byte[fstream.Length];
                    fstream.Read(filebytes, 0, Convert.ToInt32(fstream.Length));

                    string encodedData = Convert.ToBase64String(filebytes);

                    var torrent = new NewTorrent
                    {
                        Metainfo = encodedData,
                        Paused = false
                    };

                    while (true)
                    {
                        Debug.WriteLine($"Start Adding torrent {torrent.Filename}");
                        Log.Info($"Start adding torrent {torrent.Filename}");

                        if (Dispatcher.HasShutdownStarted) return;
                        try
                        {
                            var info = await transmissionClient.TorrentAddAsync(torrent);

                            switch (info)
                            {
                                case Transmission.API.RPC.Entity.Enums.AddResult.Error:
                                case Transmission.API.RPC.Entity.Enums.AddResult.Duplicate:
                                case Transmission.API.RPC.Entity.Enums.AddResult.Added:
                                    {
                                        Debug.WriteLine($"Torrent added {torrent.Filename}");
                                        Log.Info($"Torrent added {torrent.Filename}");
                                        return;
                                    }
                                case Transmission.API.RPC.Entity.Enums.AddResult.ResponseNull:
                                    {
                                        Debug.WriteLine($"Adding torrent result Null {torrent.Filename}");
                                        Log.Info($"Adding torrent result Null {torrent.Filename}");

                                        await Task.Delay(100);
                                        continue;
                                    }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);
                            return;
                        }
                    }

                }

            }).Start();

        }



        public void ShowCm()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                cmWindow = new ConnectionManager(ref dbServers);
                cmWindow.ShowDialog();
            });


        }
        private async void TryConnect(Server server)
        {
            Log.Info("Start Connection");
            IsErrorOccuredWhileConnecting = false;
            IsConnectedStatusText = "Connecting";
            try
            {
                IsConnecting = true;
                var pass = string.Empty;
                if (server.AskForPassword)
                {
                    pass = await GetPassword();
                    if (string.IsNullOrEmpty(pass)) return;

                }

                try
                {
                    transmissionClient = new TransmissionClient(server.FullUriPath, null, server.UserName, server.AskForPassword ? pass : SecureStorage.DecryptStringAndUnSecure(server.Password));

                    sessionInfo = await UpdateSessionInfo();
                    IsConnectedStatusText = $"Transmission {sessionInfo?.Version} (RPC:{sessionInfo?.RpcVersion})";

                    StartWhileCycle();
                    ConnectedServer = server;
                    IsConnected = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, ex);
                    IsConnectedStatusText = ex.Message;
                    IsConnected = false;
                    IsErrorOccuredWhileConnecting = true;
                }
            }
            finally
            {
                IsConnecting = false;
            }



        }
        private async Task<string> GetPassword()
        {
            var pass = string.Empty;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var dialog = new DialogPassword();
                pass = dialog.ShowDialog() == true ? dialog.ResponseText : string.Empty;
            });
            return pass;
        }


        private void GetAllServer()
        {
            dbServers = StorageRepository.ServersList();
            serversList = new List<Server>(dbServers.FindAll() ?? new List<Server>());
            serversList.LogServers();
        }

        private void StartWhileCycle()
        {

            if (whileCycleTask != null)
                cancelTokenSource.Cancel();
            whileCycleTask?.Dispose();

            cancelTokenSource = new CancellationTokenSource();
            var token = cancelTokenSource.Token;



            whileCycleTask = new Task(async () =>
            {
                try
                {
                    while (IsConnected && !token.IsCancellationRequested)
                    {

                        if (Application.Current.Dispatcher.HasShutdownStarted) return;

                        Debug.WriteLine("Start updating stats");
                        stats = await UpdateStats();
                        Debug.WriteLine("Stats updated");

                        //sessionInfo = await UpdateSessionInfo();

                        Debug.WriteLine("Start updating torrents");
                        var newTorrentsDataList = await UpdateTorrentList();
                        Debug.WriteLine("Torrents updated");


                        var torrents = newTorrentsDataList?.Torrents.ToList();

                        UpdateTorrentListAtUI(ref torrents);
                        UpdateStatsInfo();
                        await Task.Delay(3000, token);
                    }
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }, token);
            whileCycleTask.Start();
        }

        #region GetTorrentServerData
        private async Task<TransmissionTorrents> UpdateTorrentList()
        {
            return await ExecuteLongTask(transmissionClient.TorrentGetAsync, "Getting torrents");
        }
        private async Task<Statistic> UpdateStats()
        {
            return await ExecuteLongTask(transmissionClient.GetSessionStatisticAsync, "Updating stats");
        }


        private async Task<SessionInfo> UpdateSessionInfo()
        {
            return await ExecuteLongTask(transmissionClient.GetSessionInformationAsync, "Connectiong to session");

        }


        private async Task<dynamic> ExecuteLongTask(Func<dynamic> asyncTask, string longStatusText)
        {
            while (true)
            {
                Debug.WriteLine($"Start getting {nameof(asyncTask)}");
                if (Dispatcher.HasShutdownStarted) return null;
                var resp = await asyncTask();
                if (resp == null)
                {
                    Debug.WriteLine($"w8ing for response");
                    IsDoingStuff = true;
                    LongStatusText = longStatusText;
                    await Task.Delay(100);
                }
                else
                {
                    IsDoingStuff = false;
                    LongStatusText = string.Empty;
                    Debug.WriteLine($"Response geted");
                    return resp;
                }
            }
        }
        #endregion


        #region ParsersToUserFriendlyFormat
        private void UpdateTorrentListAtUI(ref List<TorrentInfo> list)
        {

            if (list == null) return;

            //add new one or update old
            foreach (var item in list)
            {
                if (TorrentList.Any(x => x.ID == item.ID))
                {
                    var data = TorrentList.First(x => x.ID == item.ID);
                    var ind = TorrentList.IndexOf(data);
                    UpdateTorrentData(ind, item);
                }
                else
                {
                    Dispatcher.Invoke(() => { TorrentList.Add(item); });
                }
            }


            //remove removed torrent
            foreach (var item in TorrentList.ToList())
            {
                if (!list.Any(x => x.ID == item.ID))
                {
                    var data = TorrentList.First(x => x.ID == item.ID);
                    var ind = TorrentList.IndexOf(data);
                    if (SelectedTorrent.ID == data.ID) SelectedTorrent = null;
                    Dispatcher.Invoke(() => { TorrentList.RemoveAt(ind); });

                }
            }


        }
        private void UpdateTorrentData(int index, TorrentInfo newData)
        {
            if (SelectedTorrent == null) return;
            if (SelectedTorrent.ID == newData.ID) SelectedTorrent = newData;

            Type myType = typeof(TorrentInfo);
            var props = myType.GetProperties();
            foreach (var item in props)
            {
                TorrentList[index].Set(item.Name, newData.Get(item.Name));
            }
            //TorrentList[index].RateUpload = newData.RateUpload;
            //TorrentList[index].RateDownload = newData.RateDownload;
            //TorrentList[index].AddedDate = newData.AddedDate;
            //TorrentList[index].Pieces = newData.Pieces;
            //TorrentList[index].PieceCount = newData.PieceCount;
            //TorrentList[index].DownloadedEver = newData.DownloadedEver;
            TorrentList[index].PercentDone = newData.PercentDone;

        }

        private void UpdateStatsInfo()
        {
            var dSpeedText = BytesToUserFriendlySpeed.GetSizeString(stats.downloadSpeed);
            var uSpeedText = BytesToUserFriendlySpeed.GetSizeString(stats.uploadSpeed);

            var dSpeed = string.IsNullOrEmpty(dSpeedText) ? "0 b/s" : dSpeedText;
            var uSpeed = string.IsNullOrEmpty(uSpeedText) ? "0 b/s" : uSpeedText;

            DownloadSpeed = $"D: {dSpeed}";
            UploadSpeed = $"U: {uSpeed}";
        }
        #endregion




        #region Events

        private void RetryConnection_ButtonCLick(object sender, RoutedEventArgs e)
        {
            new Task(() => { TryConnect(serversList.FirstOrDefault()); }).Start();

        }
        #endregion


    }
}
