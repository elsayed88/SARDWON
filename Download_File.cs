using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using DownSarSoftApp.classes;
using DownSarSoftApp.DownloadCore;

namespace DownSarSoftApp
{
    public partial class Download_File : Form
    {
        private DownloadItem _downloadItem;
        private System.Timers.Timer _uiTimer;
        private CancellationTokenSource _cancellationTokenSource;
        private DownloadManager _downloadManager;

        public Download_File(DownloadItem item, DownloadManager downloadManager = null)
        {
            InitializeComponent();
            _downloadItem = item ?? throw new ArgumentNullException(nameof(item));
            _downloadManager = downloadManager;
            InitializeUI();
            InitializeTimer();
            InitializeEvents();
        }

        private void InitializeEvents()
        {
            _downloadItem.OnBytesReceived += bytes => Invoke(new Action(() => UpdateProgress()));
            _downloadItem.OnCompleted += () => Invoke(new Action(() => {
                progressBar1.Value = 100;
                Start_but.Enabled = false;
                Cancel_but.Enabled = false;
            }));
            _downloadItem.OnError += ex => Invoke(new Action(() => {
                MessageBox.Show($"حدث خطأ: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }));
        }

        private void InitializeUI()
        {
            Url_Downlod_Name.Text = _downloadItem.Url;
            Status_name.Text = _downloadItem.Status.ToString();
            File_Size.Text = FormatSize(_downloadItem.TotalSize);
            Resume_capability.Text = _downloadItem.IsResumeSupported ? "Yes" : "No";

            UpdateProgress();
            FillPartsGrid();
        }

        private void InitializeTimer()
        {
            _uiTimer = new System.Timers.Timer(1000); // يحدث كل ثانية
            _uiTimer.Elapsed += (s, e) => Invoke(new Action(UpdateProgress));
            _uiTimer.Start();
        }

        private void UpdateProgress()
        {
            SizeDown.Text = FormatSize(_downloadItem.DownloadedSize);
            Downloded100.Text = $"{_downloadItem.ProgressPercentage:0.##} %";
            Transfer_rate.Text = FormatSpeed(_downloadItem.TransferRate);
            Tiem_left.Text = FormatTime(_downloadItem.TimeLeft);
            Status_name.Text = _downloadItem.Status.ToString();

            progressBar1.Value = Math.Min((int)_downloadItem.ProgressPercentage, 100);

            UpdatePartsGrid();
        }

        private void FillPartsGrid()
        {
            dataGridView1.Rows.Clear();
            foreach (var part in _downloadItem.Parts)
            {
                dataGridView1.Rows.Add(part.PartNumber, part.PartName, $"{part.Progress:0.##} %");
            }
        }

        private void UpdatePartsGrid()
        {
            for (int i = 0; i < _downloadItem.Parts.Count; i++)
            {
                dataGridView1.Rows[i].Cells[2].Value = $"{_downloadItem.Parts[i].Progress:0.##} %";
            }
        }

        private string FormatSize(long bytes)
        {
            double size = bytes;
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return $"{size:0.##} {units[unit]}";
        }

        private string FormatSpeed(double speed)
        {
            return $"{FormatSize((long)speed)}/s";
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalSeconds <= 0) return "N/A";
            return time.ToString(@"hh\:mm\:ss");
        }

        private async void Start_but_Click(object sender, EventArgs e)
        {
            if (_downloadItem.Status == DownloadStatus.Paused || _downloadItem.Status == DownloadStatus.NotStarted)
            {
                await _downloadItem.StartAsync();
                Start_but.Text = "Pause";
            }
            else if (_downloadItem.Status == DownloadStatus.Downloading)
            {
                _downloadItem.Pause();
                Start_but.Text = "Resume";
            }
        }

        private void Cancel_but_Click(object sender, EventArgs e)
        {
            _uiTimer?.Stop();

            if (_downloadManager != null && _downloadItem != null)
            {
                _downloadManager.CancelDownload(_downloadItem);
            }

            this.Close();
        }

        private void Download_File_FormClosing(object sender, FormClosingEventArgs e)
        {
            _uiTimer?.Stop();
            _uiTimer?.Dispose();
        }

        // الدوال المطلوبة للفورم
        public void PauseDownload()
        {
            _downloadItem.Pause();
        }

        public void CancelDownload()
        {
            _downloadItem.Pause(); // أو أضف دالة إلغاء حقيقية
        }
    }
}