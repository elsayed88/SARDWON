using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace DownSarSoftApp.DownloadCore
{
    public class DownloadManager
    {
        private readonly object _lock = new object();
        private readonly List<DownloadItem> _downloads = new List<DownloadItem>();
        private readonly Dictionary<string, CancellationTokenSource> _cancellationTokens = new Dictionary<string, CancellationTokenSource>();

        public IReadOnlyList<DownloadItem> Downloads => _downloads.AsReadOnly();

        public void AddDownload(string url, string savePath)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("الرابط غير صالح");

            if (string.IsNullOrWhiteSpace(savePath))
                throw new ArgumentException("مسار الحفظ غير صالح");

            if (!Directory.Exists(savePath))
                throw new DirectoryNotFoundException("مسار الحفظ غير موجود");

            lock (_lock)
            {
                if (_downloads.Any(d => d.Url == url))
                    throw new InvalidOperationException("هذا الرابط مضاف بالفعل");

                var item = new DownloadItem(url, savePath);
                _downloads.Add(item);
                _cancellationTokens[item.Url] = new CancellationTokenSource();
            }
        }

        public void RemoveDownload(DownloadItem item)
        {
            lock (_lock)
            {
                if (_downloads.Contains(item))
                {
                    if (item.Status == DownloadStatus.Downloading)
                    {
                        if (_cancellationTokens.TryGetValue(item.Url, out var cts))
                        {
                            cts.Cancel();
                            item.Status = DownloadStatus.Cancelled;
                        }
                    }
                    _downloads.Remove(item);
                    _cancellationTokens.Remove(item.Url);
                }
            }
        }

        public async Task StartDownloadAsync(DownloadItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (!Downloads.Contains(item)) throw new ArgumentException("التحميل غير موجود");

            lock (_lock)
            {
                if (item.Status == DownloadStatus.Downloading)
                    return;

                if (!_cancellationTokens.TryGetValue(item.Url, out var cts))
                    return;

                var token = cts.Token;
                var worker = new DownloadWorker();

                worker.OnBytesReceived += bytes =>
                {
                    if (token.IsCancellationRequested) return;
                    item.Status = DownloadStatus.Downloading;
                    // لو تحتاج حساب الحجم أضف item.AddDownloadedBytes(bytes) مثلاً
                };

                worker.OnCompleted += () =>
                {
                    if (token.IsCancellationRequested) return;
                    item.Status = DownloadStatus.Completed;
                };

                worker.OnError += ex =>
                {
                    if (token.IsCancellationRequested) return;
                    item.Status = DownloadStatus.Failed;
                    item.SetError(ex.Message); // التصحيح هنا
                };

                try
                {
                    worker.StartDownloadAsync(item.Url,
                        Path.Combine(item.SavePath, item.FileName),
                        0, // يمكنك تحسينها لدعم الاستئناف
                        token).Wait();
                }
                catch (OperationCanceledException)
                {
                    item.Status = DownloadStatus.Cancelled;
                }
                catch (Exception ex)
                {
                    item.Status = DownloadStatus.Failed;
                    item.SetError(ex.Message); // التصحيح هنا أيضًا
                }
            }
        }

        // دالة الإلغاء المطلوبة
        public void CancelDownload(DownloadItem item)
        {
            lock (_lock)
            {
                if (_cancellationTokens.TryGetValue(item.Url, out var cts))
                {
                    cts.Cancel();
                    item.Status = DownloadStatus.Cancelled;
                }
            }
        }
    }
}