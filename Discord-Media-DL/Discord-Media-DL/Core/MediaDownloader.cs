﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Discord_Media_DL.Core
{
    public delegate void MediaDownloaderMediaDownloaded(string name, double progress);
    public delegate void MediaDownloaderProgressUpdateHandler(double progress);
    public delegate void MediaDownloaderErrorHandler(Exception e, string url);

    public class MediaDownloader
    {
        public event MediaDownloaderMediaDownloaded OnDownloaded;
        public event MediaDownloaderProgressUpdateHandler OnProgressUpdated;
        public event MediaDownloaderErrorHandler OnDownloadError;

        private readonly object lck = new();

        public async Task<Task> Download(string[] urls, string destination)
        {
            if (urls.Length == 0 || string.IsNullOrEmpty(destination))
                return Task.CompletedTask;

            // if its a file, get the path
            if (File.Exists(destination))
                destination = Path.GetDirectoryName(destination);

            if (destination.EndsWith("\\") == false)
                destination += "\\";

            try
            {
                if (Directory.Exists(destination))
                    Directory.Delete(destination, recursive: true);
            }
            catch { }

            if (Directory.Exists(destination) == false)
                Directory.CreateDirectory(destination);

            // It is extremely unlikely that there are 2 medias with the exact same size. if thats the case it has to be a duplicate.
            List<int> allSizes = new();

            Task t = new(() =>
            {
                List<string> queue = urls.ToList();

                var pointer = urls.Length - 1;

                while (pointer >= 0)
                {
                    var fileUrl = "";

                    fileUrl = urls[pointer];

                    pointer--;

                    var progress = 100.0 / urls.Length * (urls.Length - 1.0 - pointer);

                    try
                    {
                        WebClient client = new();
                        var data = client.DownloadData(fileUrl);

                        if (allSizes.Contains(data.Length) == false)
                        {
                            allSizes.Add(data.Length);

                            var extension = Path.GetExtension(new Uri(fileUrl).LocalPath);
                            var path = destination + Guid.NewGuid().ToString() + extension;

                            if (File.Exists(path) == false)
                                File.Create(path).Close();

                            File.WriteAllBytes(path, data);

                            OnDownloaded?.Invoke(fileUrl, progress);

                            client.Dispose();
                        }
                        else
                            OnDownloadError?.Invoke(new Exception("Duplicate Media found and avoided."), fileUrl);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        Console.WriteLine($"Failed to Download: {fileUrl}");
                        Console.WriteLine(e);
#endif
                        OnDownloadError?.Invoke(e, fileUrl);
                    }

                    OnProgressUpdated?.Invoke(progress);
                }
            });

            t.Start();

            await t;

            return t;
        }
    }
}
