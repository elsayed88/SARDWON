using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DownSarSoftApp.classes;
using DownSarSoftApp.DownloadCore;

namespace DownSarSoftApp
{
    public partial class MinPage : Form
    {
        private DownloadManager downloadManager;
        private Dictionary<DownloadItem, Download_File> activeDownloadForms;

        public MinPage()
        {
            InitializeComponent();

            downloadManager = new DownloadManager();
            activeDownloadForms = new Dictionary<DownloadItem, Download_File>();

            Add_Url.Click += Add_Url_Click;
            Resume.Click += Resume_Click;
            Stop.Click += Stop_Click;
            Stop_All.Click += Stop_All_Click;
            Delete.Click += Delete_Click;
            Delete_All.Click += Delete_All_Click;
            Options.Click += Options_Click;

            SetupDataGridView();

            RefreshDownloadList();
        }

        private void SetupDataGridView()
        {
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.AllowUserToAddRows = false;

            dataGridView1.DataSource = new BindingSource { DataSource = downloadManager.Downloads };
        }

        private void RefreshDownloadList()
        {
            dataGridView1.Refresh();
        }

        private void Add_Url_Click(object sender, EventArgs e)
        {
            using (Add_Url addUrlForm = new Add_Url())
            {
                var result = addUrlForm.ShowDialog();
                if (result == DialogResult.OK)
                {
                    using (Download_file_info infoForm = new Download_file_info(addUrlForm.UrlText.Trim()))
                    {
                        if (infoForm.ShowDialog() == DialogResult.OK)
                        {
                            string finalPath = infoForm.FinalDownloadPath ?? "";
                            if (string.IsNullOrEmpty(finalPath))
                            {
                                MessageBox.Show("Please select a valid download path.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            downloadManager.AddDownload(infoForm.UrlName, System.IO.Path.GetDirectoryName(finalPath));
                            RefreshDownloadList();
                        }
                    }
                }
            }
        }

        private void Resume_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0) return;
            var item = dataGridView1.SelectedRows[0].DataBoundItem as DownloadItem;
            if (item == null) return;

            if (item.Status == DownloadStatus.Paused || item.Status == DownloadStatus.Failed)
            {
                if (!activeDownloadForms.ContainsKey(item))
                {
                    var downloadForm = new Download_File(item, downloadManager); // ÅÕáÇÍ åäÇ
                    activeDownloadForms[item] = downloadForm;
                    downloadForm.Show();
                }

                downloadManager.StartDownloadAsync(item).ContinueWith(t =>
                {
                    Invoke(new Action(() => RefreshDownloadList()));
                });
            }
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0) return;
            var item = dataGridView1.SelectedRows[0].DataBoundItem as DownloadItem;
            if (item == null) return;

            if (activeDownloadForms.ContainsKey(item))
            {
                activeDownloadForms[item].PauseDownload();
                RefreshDownloadList();
            }
        }

        private void Stop_All_Click(object sender, EventArgs e)
        {
            foreach (var form in activeDownloadForms.Values)
            {
                form.PauseDownload();
            }
            RefreshDownloadList();
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0) return;
            var item = dataGridView1.SelectedRows[0].DataBoundItem as DownloadItem;
            if (item == null) return;

            if (activeDownloadForms.ContainsKey(item))
            {
                activeDownloadForms[item].CancelDownload();
                activeDownloadForms[item].Close();
                activeDownloadForms.Remove(item);
            }

            downloadManager.RemoveDownload(item);
            RefreshDownloadList();
        }

        private void Delete_All_Click(object sender, EventArgs e)
        {
            foreach (var item in new List<DownloadItem>(downloadManager.Downloads))
            {
                if (activeDownloadForms.ContainsKey(item))
                {
                    activeDownloadForms[item].CancelDownload();
                    activeDownloadForms[item].Close();
                    activeDownloadForms.Remove(item);
                }
                downloadManager.RemoveDownload(item);
            }
            RefreshDownloadList();
        }

        private void Options_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Options clicked - add your options form here.");
        }
    }
}