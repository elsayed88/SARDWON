using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace DownSarSoftApp.DownloadCore
{
    public enum PartStatus
    {
        NotStarted,
        Downloading,
        Completed,
        Failed,
        Cancelled,
        Paused,
        Merging
    }

    public class DownloadPart : IDisposable
    {
        public int PartNumber { get; private set; }
        public long StartByte { get; private set; }
        public long EndByte { get; private set; }
        public long BytesDownloaded { get; private set; }
        public PartStatus Status { get; private set; } = PartStatus.NotStarted;
        public string ErrorMessage { get; private set; } = string.Empty;

        // اسم الجزء (للاستخدام مثلاً في واجهة المستخدم)
        public string PartName => $"Part {PartNumber}";

        // نسبة التقدم بالتحميل (0-100)
        public int Progress
        {
            get
            {
                long partSize = EndByte - StartByte + 1;
                if (partSize == 0) return 0;
                return (int)((BytesDownloaded * 100) / partSize);
            }
        }

        private string url;
        private string tempFilePath;
        private CancellationTokenSource cancellationTokenSource;
        private HttpClient httpClient;
        private bool disposed;

        public event Action<DownloadPart> ProgressChanged;
        public event Action<DownloadPart> StatusChanged;

        public DownloadPart(string url, int partNumber, long startByte, long endByte, string tempFilePath)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("الرابط غير صالح");

            if (partNumber <= 0)
                throw new ArgumentException("رقم الجزء يجب أن يكون أكبر من صفر");

            if (startByte < 0 || endByte < startByte)
                throw new ArgumentException("قيم البايت غير صالحة");

            if (string.IsNullOrWhiteSpace(tempFilePath))
                throw new ArgumentException("مسار الملف المؤقت غير صالح");

            this.url = url;
            PartNumber = partNumber;
            StartByte = startByte;
            EndByte = endByte;
            this.tempFilePath = tempFilePath;

            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            cancellationTokenSource = new CancellationTokenSource();

            // إنشاء مجلد الملف المؤقت إذا لم يكن موجوداً
            Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath));
        }

        public async Task StartAsync()
        {
            if (Status == PartStatus.Downloading || Status == PartStatus.Merging)
                return;

            Status = PartStatus.Downloading;
            StatusChanged?.Invoke(this);

            try
            {
                // التحقق من وجود الملف المؤقت
                if (File.Exists(tempFilePath))
                {
                    BytesDownloaded = new FileInfo(tempFilePath).Length;
                }

                var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await httpClient.SendAsync(request, cancellationTokenSource.Token);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"فشل في الوصول للملف: {response.StatusCode}");
                }

                // التحقق من دعم التحميل الجزئي
                var acceptRanges = response.Headers.AcceptRanges.FirstOrDefault();
                if (acceptRanges != "bytes")
                {
                    throw new NotSupportedException("الخادم لا يدعم التحميل الجزئي");
                }

                // التحقق من حجم الجزء
                long totalSize = response.Content.Headers.ContentLength ?? -1;
                if (totalSize <= 0)
                {
                    throw new InvalidOperationException("حجم الملف غير معروف");
                }

                if (EndByte >= totalSize)
                {
                    EndByte = totalSize - 1;
                }

                // التحميل الفعلي
                request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(StartByte + BytesDownloaded, EndByte);

                using var downloadResponse = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token);
                downloadResponse.EnsureSuccessStatusCode();

                using var stream = await downloadResponse.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(tempFilePath,
                    File.Exists(tempFilePath) ? FileMode.Append : FileMode.Create,
                    FileAccess.Write,
                    FileShare.None);

                byte[] buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token)) > 0)
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationTokenSource.Token);
                    BytesDownloaded += bytesRead;
                    ProgressChanged?.Invoke(this);
                }

                Status = PartStatus.Completed;
                StatusChanged?.Invoke(this);
            }
            catch (OperationCanceledException)
            {
                Status = PartStatus.Paused;
                StatusChanged?.Invoke(this);
            }
            catch (Exception ex)
            {
                Status = PartStatus.Failed;
                ErrorMessage = ex.Message;
                StatusChanged?.Invoke(this);
                throw;
            }
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    cancellationTokenSource?.Dispose();
                    httpClient?.Dispose();
                }
                disposed = true;
            }
        }
    }
}