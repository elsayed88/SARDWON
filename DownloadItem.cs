// DownloadItem.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DownSarSoftApp.DownloadCore
{
    public enum DownloadStatus
    {
        NotStarted,   // لم يبدأ التنزيل بعد
        Pending,      // جاهز للتحميل (معلق أو انتظار)
        Downloading,  // جاري التنزيل
        Paused,       // مؤقتًا متوقف
        Completed,    // تم التنزيل بنجاح
        Failed,       // فشل التنزيل
        Cancelled     // تم الإلغاء
    }

    public class DownloadItem
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public string SavePath { get; set; }
        public long TotalSize { get; set; } = 0;
        public DownloadStatus Status { get; set; } = DownloadStatus.NotStarted;
        public bool IsResumeSupported { get; set; } = false;

        // قائمة الأجزاء التي يتم تنزيلها (يمكن أن تكون فارغة لو التحميل غير مقسم)
        public List<DownloadPart> Parts { get; set; } = new List<DownloadPart>();

        private long _downloadedSizeInternal = 0;
        private CancellationTokenSource _cancellationTokenSource;
        private object _syncLock = new object();

        // الأحداث التي كانت ناقصة
        public event Action<long> OnBytesReceived;
        public event Action OnCompleted;
        public event Action<Exception> OnError;

        // خاصية قراءة فقط: مجموع الحجم الذي تم تنزيله
        public long DownloadedSize =>
            (Parts != null && Parts.Count > 0) ? Parts.Sum(p => p.BytesDownloaded) : _downloadedSizeInternal;

        // سرعة النقل بالبايت في الثانية (يتم تحديثها في مكان تحميل البيانات)
        public double TransferRate { get; set; } = 0;

        // الوقت المتبقي بناء على سرعة التحميل
        public TimeSpan TimeLeft
        {
            get
            {
                var bytesLeft = TotalSize - DownloadedSize;
                if (TransferRate > 0)
                {
                    var secondsLeft = bytesLeft / TransferRate;
                    return TimeSpan.FromSeconds(secondsLeft);
                }
                else
                {
                    return TimeSpan.MaxValue;
                }
            }
        }

        // النسبة المئوية للتحميل
        public int ProgressPercentage
        {
            get
            {
                if (TotalSize == 0) return 0;
                return (int)((DownloadedSize * 100) / TotalSize);
            }
        }

        private DownloadWorker _worker;

        public string ErrorMessage { get; private set; }
        public void SetError(string message)
        {
            ErrorMessage = message;
        }

        public DownloadItem(string url, string savePath)
        {
            Url = url;
            SavePath = savePath;
            FileName = FileHelper.GetFileNameFromUrl(url);

            if (string.IsNullOrWhiteSpace(savePath) || !Directory.Exists(savePath))
            {
                throw new ArgumentException("مسار الحفظ غير صالح أو غير موجود");
            }

            _worker = new DownloadWorker();
            _cancellationTokenSource = new CancellationTokenSource();

            // تحديث حجم التحميل عند استلام البيانات
            _worker.OnBytesReceived += bytes =>
            {
                lock (_syncLock)
                {
                    _downloadedSizeInternal += bytes;
                    Status = DownloadStatus.Downloading;
                    OnBytesReceived?.Invoke(bytes);
                }
            };

            _worker.OnCompleted += () =>
            {
                lock (_syncLock)
                {
                    Status = DownloadStatus.Completed;
                    OnCompleted?.Invoke();
                }
            };

            _worker.OnError += ex =>
            {
                lock (_syncLock)
                {
                    Status = DownloadStatus.Failed;
                    ErrorMessage = ex.Message;
                    OnError?.Invoke(ex);
                }
            };

            // إزالة event handlers عند التخلص من الكائن
            GC.SuppressFinalize(this);
        }

        ~DownloadItem()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            // لا تحاول إلغاء اشتراك الأحداث المجهولة
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        // بدء أو استئناف التحميل
        public async Task StartAsync()
        {
            if (Status == DownloadStatus.Downloading) return;

            Status = DownloadStatus.Downloading;

            string fullPath = Path.Combine(SavePath, FileName);
            await _worker.StartDownloadAsync(Url, fullPath, _downloadedSizeInternal);
        }

        // إيقاف التحميل مؤقتًا
        public void Pause()
        {
            _worker.Cancel();
            Status = DownloadStatus.Paused;
        }
    }
}