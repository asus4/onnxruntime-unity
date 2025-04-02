// Requires Awaitable support
#if UNITY_2023_1_OR_NEWER

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Microsoft.ML.OnnxRuntime.Unity
{
    /// <summary>
    /// Simple remote file download and cache system.
    /// Not for production use.
    /// </summary>
    [Serializable]
    public class RemoteFile : IProgress<float>
    {
        public enum DownloadLocation
        {
            Persistent,
            Cache,
        }

        public string url;
        public DownloadLocation downloadLocation = DownloadLocation.Persistent;

        public event Action<float> OnDownloadProgress;

        public string LocalPath
        {
            get
            {
                string dir = downloadLocation switch
                {
                    DownloadLocation.Persistent => Application.persistentDataPath,
                    DownloadLocation.Cache => Application.temporaryCachePath,
                    _ => throw new Exception($"Unknown download location {downloadLocation}"),
                };
                // make hash from url
                string ext = GetExtension(url);
                string fileName = $"{url.GetHashCode():X8}{ext}";
                return Path.Combine(dir, fileName);
            }
        }

        public bool HasCache => File.Exists(LocalPath);

        public RemoteFile() { }

        public RemoteFile(string url, DownloadLocation location = DownloadLocation.Persistent)
        {
            this.url = url;
            downloadLocation = location;
        }

        // IProgress<float>
        public void Report(float value)
        {
            OnDownloadProgress?.Invoke(value);
        }

        public async Awaitable<byte[]> Load(CancellationToken cancellationToken)
        {
            string localPath = LocalPath;

            if (HasCache)
            {
                Log($"Cache Loading file from local: {localPath}");
                using var handler = new DownloadHandlerBuffer();
                if (!localPath.StartsWith("file:/"))
                {
                    localPath = $"file://{localPath}";
                }
                using var request = new UnityWebRequest(localPath, "GET", handler, null);
                await LoadWithProgress(request, this, cancellationToken);
                return handler.data;
            }
            else
            {
                Log($"Cache not found at {localPath}. Loading from: {url}");
                using var handler = new DownloadHandlerFile(localPath);
                handler.removeFileOnAbort = true;
                using var request = new UnityWebRequest(url, "GET", handler, null);
                await LoadWithProgress(request, this, cancellationToken);
                return await File.ReadAllBytesAsync(localPath, cancellationToken);
            }
        }

        static async Awaitable LoadWithProgress(UnityWebRequest request, IProgress<float> progress, CancellationToken cancellationToken)
        {
            progress.Report(0.0f);
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Awaitable.NextFrameAsync();
                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    throw new TaskCanceledException();
                }
                progress.Report(operation.progress);
            }

            progress.Report(1.0f);

            if (request.result != UnityWebRequest.Result.Success)
            {
                request.Abort();
                throw new Exception($"Failed to download {request.downloadProgress}: {request.error}");
            }
        }

        static string GetExtension(string url)
        {
            string ext = Path.GetExtension(url);
            if (ext.Contains('?'))
            {
                ext = ext[..ext.IndexOf('?')];
            }
            return ext;
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        static void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }
    }
}
#endif // UNITY_2023_1_OR_NEWER
