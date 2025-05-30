using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownSarSoftApp.DownloadCore
{
    public class DownloadSettings
    {
        public string DefaultDownloadPath { get; set; } = "C:\\Downloads";
        public List<string> Categories { get; set; } = new List<string>
        {
            "General",
            "Compressed",
            "Programs",
            "Video",
            "Documents"
        };

        // يمكن إضافة تحميل وحفظ الإعدادات من ملف config هنا لاحقًا
    }
}
