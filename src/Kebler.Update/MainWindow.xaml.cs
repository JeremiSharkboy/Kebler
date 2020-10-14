﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Mime;
using System.Windows;
using Kebler.Const;

namespace Kebler.Update
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        private readonly Uri uri;
        private MyWebClient _webClient;
        private string tempfile;

        public MainWindow(Uri uri)
        {
            InitializeComponent();
            this.uri = uri;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _webClient = new MyWebClient();

            tempfile = Path.GetTempFileName();

            _webClient.DownloadProgressChanged += OnDownloadProgressChanged;
            _webClient.DownloadFileCompleted += WebClientOnDownloadFileCompleted;

            _webClient.DownloadFileTaskAsync(uri, tempfile);
        }

        private void WebClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                if (e.Cancelled)
                {
                    Close();
                    Application.Current.Shutdown();
                    Environment.Exit(0);
                }


                App.Log("Download completed");

                ContentDisposition contentDisposition = null;
                if (_webClient.ResponseHeaders?["Content-Disposition"] != null)
                    contentDisposition = new ContentDisposition(_webClient.ResponseHeaders["Content-Disposition"]);

                var fileName = contentDisposition.FileName;

                var pth = Path.Combine(Path.GetTempPath(), fileName);
                if (File.Exists(pth)) File.Delete(pth);
                File.Move(tempfile, pth);

                var zip = new ZipArchive(new FileStream(pth, FileMode.Open));
                zip.ExtractToDirectory(ConstStrings.KeblerRoamingFolder, true);
                DialogResult = true;
            }
            catch
            {
                Close();
                Application.Current.Shutdown();
                Environment.Exit(0);
            }
            finally
            {
                Close();
                Application.Current.Shutdown();
                Environment.Exit(0);
            }
            return;
        }

        public static void DeleteDirectory(string path)
        {
            foreach (var directory in Directory.GetDirectories(path)) DeleteDirectory(directory);

            try
            {
                Directory.Delete(path, true);
            }
            catch (IOException)
            {
                Directory.Delete(path, true);
            }
            catch (UnauthorizedAccessException)
            {
                Directory.Delete(path, true);
            }
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //if (_startedAt == default)
            //{
            //    _startedAt = DateTime.Now;
            //}
            //else
            //{
            //    var timeSpan = DateTime.Now - _startedAt;
            //    long totalSeconds = (long)timeSpan.TotalSeconds;
            //    if (totalSeconds > 0)
            //    {
            //        var bytesPerSecond = e.BytesReceived / totalSeconds;
            //        //Speed.Content = string.Format(BytesToString(bytesPerSecond)+"/s");
            //    }
            //}
          
            Size.Content = $@"{BytesToString(e.BytesReceived)} / {BytesToString(e.TotalBytesToReceive)}";
            PB.Value = e.ProgressPercentage;
        }

        private static string BytesToString(long byteCount)
        {
            string[] suf = {"B", "KB", "MB", "GB", "TB", "PB", "EB"};
            if (byteCount == 0)
                return "0" + suf[0];
            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{(Math.Sign(byteCount) * num).ToString(CultureInfo.InvariantCulture)} {suf[place]}";
        }

        private void CustomizableWindow_Closing(object sender, CancelEventArgs e)
        {
            _webClient.CancelAsync();

            _webClient.DownloadProgressChanged -= OnDownloadProgressChanged;
            _webClient.DownloadFileCompleted -= WebClientOnDownloadFileCompleted;

            _webClient.Dispose();
            Application.Current.Shutdown();
            Environment.Exit(0);
        }
    }

    public class MyWebClient : WebClient
    {
        /// <summary>
        ///     Response Uri after any redirects.
        /// </summary>
        public Uri ResponseUri;

        /// <inheritdoc />
        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            var webResponse = base.GetWebResponse(request, result);
            ResponseUri = webResponse.ResponseUri;
            return webResponse;
        }
    }
}