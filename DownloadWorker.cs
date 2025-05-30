using System;
using System.Net.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace DownSarSoftApp.DownloadCore
{
    public class DownloadWorker : IDisposable
    {
        private readonly HttpClient _client;
        private CancellationTokenSource _cts;
        private bool _disposed;

        public event Action<long> OnBytesReceived;
        public event Action OnCompleted;
        public event Action<Exception> OnError;

        public DownloadWorker()
        {
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task StartDownloadAsync(string url, string savePath, long startByte = 0, CancellationToken? externalToken = null)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("الرابط غير صالح");

            if (string.IsNullOrWhiteSpace(savePath))
                throw new ArgumentException("مسار الحفظ غير صالح");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken ?? CancellationToken.None);

            try
            {
                string tempPath = savePath + ".temp";
                bool isResume = File.Exists(tempPath);

                Directory.CreateDirectory(Path.GetDirectoryName(savePath));

                var request = new HttpRequestMessage(HttpMethod.Head, url);
                using (var response = await _client.SendAsync(request, _cts.Token))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        RaiseOnError(new HttpRequestException($"فشل في الوصول للملف: {response.StatusCode}"));
                        return;
                    }

                    long totalSize = response.Content.Headers.ContentLength ?? -1;
                    if (totalSize <= 0)
                    {
                        RaiseOnError(new InvalidOperationException("حجم الملف غير معروف"));
                        return;
                    }
                }

                if (isResume)
                {
                    startByte = new FileInfo(tempPath).Length;
                    request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startByte, null);
                }
                else
                {
                    request = new HttpRequestMessage(HttpMethod.Get, url);
                }

                using (var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cts.Token))
                {
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fs = new FileStream(isResume ? tempPath : savePath,
                        isResume ? FileMode.Append : FileMode.Create,
                        FileAccess.Write,
                        FileShare.None))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        long totalBytesRead = startByte;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, bytesRead, _cts.Token);
                            totalBytesRead += bytesRead;
                            RaiseOnBytesReceived(bytesRead);

                            _cts.Token.ThrowIfCancellationRequested();
                        }

                        if (isResume)
                        {
                            File.Move(tempPath, savePath, true);
                        }
                    }
                }

                RaiseOnCompleted();
            }
            catch (OperationCanceledException)
            {
                try
                {
                    string tempPath = savePath + ".temp";
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch { }
                throw;
            }
            catch (Exception ex)
            {
                RaiseOnError(ex);
                throw;
            }
            finally
            {
                _cts?.Dispose();
            }
        }

        // دوال مساعدة صحيحة لاستدعاء الأحداث
        protected virtual void RaiseOnBytesReceived(long bytes)
        {
            OnBytesReceived?.Invoke(bytes);
        }
        protected virtual void RaiseOnCompleted()
        {
            OnCompleted?.Invoke();
        }
        protected virtual void RaiseOnError(Exception ex)
        {
            OnError?.Invoke(ex);
        }

        public void Cancel()
        {
            _cts?.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client?.Dispose();
                    _cts?.Dispose();
                }
                _disposed = true;
            }
        }

        ~DownloadWorker()
        {
            Dispose(false);
        }
    }
}