using System.ComponentModel;

namespace DownSarSoftApp
{
    public partial class Download_file_info : Form
    {
        private string fileUrl;
        private string downloadPath = string.Empty;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? FinalDownloadPath { get; private set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? SelectedCategory { get; private set; }

        public string UrlName => Url_Name.Text; // الخاصية المطلوبة

        public Download_file_info(string url)
        {
            InitializeComponent();
            fileUrl = url;
            FinalDownloadPath = string.Empty;
            InitializeControls();
            Start_Download.Click += Start_Download_Click;
            Cancel_Download.Click += Cancel_Download_Click;
            Browes.Click += Browes_Click;
            Category_Name.SelectedIndexChanged += Category_Name_SelectedIndexChanged;
        }

        private void InitializeControls()
        {
            Url_Name.Text = fileUrl;
            Url_Name.Enabled = false;

            Category_Name.Items.AddRange(new string[]
            {
                "General", "Compressed", "Programs", "Video", "Documents"
            });
            Category_Name.SelectedIndex = 0;

            path_Downloads.Enabled = false;

            if (Properties.Settings.Default.RememberPath)
            {
                Remember.Checked = true;
                downloadPath = Properties.Settings.Default.LastDownloadPath;
            }
            else
            {
                downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Downloads");
            }

            UpdatePathTextBox();
        }

        private void Browes_Click(object? sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    downloadPath = dialog.SelectedPath;
                    UpdatePathTextBox();
                }
            }
        }

        private void Category_Name_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string category = Category_Name.SelectedItem?.ToString() ?? "General";
            SelectedCategory = category;
            string fileName = Path.GetFileName(new Uri(fileUrl).AbsolutePath);

            string baseFolder = downloadPath;

            if (!string.IsNullOrWhiteSpace(category))
                baseFolder = Path.Combine(downloadPath, category);

            FinalDownloadPath = Path.Combine(baseFolder, fileName);
            path_Downloads.Text = FinalDownloadPath;
        }

        private void Start_Download_Click(object? sender, EventArgs e)
        {
            if (Remember.Checked)
            {
                Properties.Settings.Default.RememberPath = true;
                Properties.Settings.Default.LastDownloadPath = downloadPath;
                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.RememberPath = false;
                Properties.Settings.Default.LastDownloadPath = "";
                Properties.Settings.Default.Save();
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void Cancel_Download_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void UpdatePathTextBox()
        {
            string category = Category_Name.SelectedItem?.ToString() ?? "General";
            string fileName = Path.GetFileName(new Uri(fileUrl).AbsolutePath);
            FinalDownloadPath = Path.Combine(downloadPath, category, fileName);
            path_Downloads.Text = FinalDownloadPath;
        }
    }
}