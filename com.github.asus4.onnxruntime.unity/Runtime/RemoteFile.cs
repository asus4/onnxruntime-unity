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

        /// <summary>
        /// Ensures the file is downloaded and cached locally, returning its local path.
        /// On iOS, the file is excluded from iCloud backup.
        /// </summary>
        public async Awaitable<string> EnsureLocal(CancellationToken cancellationToken)
        {
            string localPath = LocalPath;

            if (File.Exists(localPath))
            {
                Log($"Cache hit: {localPath}");
                ExcludeFromBackup(localPath);
                return localPath;
            }

            Log($"Cache miss for {localPath}, downloading from: {url}");
            string tempPath = $"{localPath}.{Guid.NewGuid():N}.tmp";

            try
            {
                using var handler = new DownloadHandlerFile(tempPath);
                handler.removeFileOnAbort = true;
                using var request = new UnityWebRequest(url, "GET", handler, null);
                await SendAsync(request, this, cancellationToken);

                File.Delete(localPath);
                File.Move(tempPath, localPath);
            }
            catch
            {
                File.Delete(tempPath);
                throw;
            }

            ExcludeFromBackup(localPath);
            return localPath;
        }

        /// <summary>
        /// Returns the file size in bytes.
        /// </summary>
        public async Awaitable<long> GetSize(CancellationToken cancellationToken)
        {
            string localPath = LocalPath;
            if (File.Exists(localPath))
            {
                return new FileInfo(localPath).Length;
            }

            // Range GET is more reliable than HEAD with UnityWebRequest.
            using var handler = new DownloadHandlerBuffer();
            using var request = new UnityWebRequest(url, "GET", handler, null);
            request.SetRequestHeader("Range", "bytes=0-0");
            await SendAsync(request, null, cancellationToken);

            string contentRange = request.GetResponseHeader("Content-Range");
            if (!string.IsNullOrEmpty(contentRange))
            {
                int slashIdx = contentRange.LastIndexOf('/');
                if (slashIdx >= 0 && slashIdx < contentRange.Length - 1
                    && long.TryParse(contentRange[(slashIdx + 1)..], out long total))
                {
                    return total;
                }
            }

            string contentLength = request.GetResponseHeader("Content-Length");
            if (!string.IsNullOrEmpty(contentLength) && long.TryParse(contentLength, out long length))
            {
                return length;
            }

            throw new Exception($"Could not determine file size for {url} (no Content-Range or Content-Length)");
        }

        /// <summary>
        /// Downloads (if needed) and returns the full file bytes.
        /// Convenience wrapper around <see cref="EnsureLocal"/> for callers that want
        /// a managed byte[]. For large models prefer <see cref="EnsureLocal"/> and pass
        /// the returned path to <c>InferenceSession</c> directly so ORT can mmap the file.
        /// </summary>
        public async Awaitable<byte[]> Load(CancellationToken cancellationToken)
        {
            var path = await EnsureLocal(cancellationToken);
            return await File.ReadAllBytesAsync(path, cancellationToken);
        }

        static void ExcludeFromBackup(string path)
        {
#if UNITY_IOS && !UNITY_EDITOR
            UnityEngine.iOS.Device.SetNoBackupFlag(path);
#endif
        }

        static async Awaitable SendAsync(UnityWebRequest request, IProgress<float> progress, CancellationToken cancellationToken)
        {
            progress?.Report(0.0f);
            var operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                await Awaitable.NextFrameAsync();
                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    throw new TaskCanceledException();
                }
                progress?.Report(operation.progress);
            }

            progress?.Report(1.0f);

            if (request.result != UnityWebRequest.Result.Success)
            {
                request.Abort();
                throw new Exception($"Failed to download from {request.url}: {request.error}");
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
