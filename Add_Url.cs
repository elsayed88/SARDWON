using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DownSarSoftApp
{
    public partial class Add_Url : Form
    {
        public Add_Url()
        {
            InitializeComponent();
            BT_OK.Click += BT_OK_Click;
            BT_Cancel.Click += BT_Cancel_Click;
        }

        public string UrlText => Add_UrlDownloded.Text; // الخاصية المطلوبة

        private void BT_OK_Click(object? sender, EventArgs e)
        {
            string url = Add_UrlDownloded.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                MessageBox.Show("Please enter a valid URL.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                MessageBox.Show("The URL format is invalid.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Download_file_info infoForm = new Download_file_info(url);
            infoForm.Show();
            this.Hide();
        }

        private void BT_Cancel_Click(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}