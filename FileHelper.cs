using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownSarSoftApp.DownloadCore
{
    public static class FileHelper
    {
        public static string GetFileNameFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "unknown.file";

            try
            {
                Uri uri = new Uri(url);
                string filename = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrEmpty(filename))
                    filename = "unknown.file";
                return filename;
            }
            catch
            {
                return "unknown.file";
            }
        }

        public static void MergeFiles(string[] partFiles, string destinationFile)
        {
            using (var destStream = new FileStream(destinationFile, FileMode.Create))
            {
                foreach (var part in partFiles)
                {
                    using (var sourceStream = new FileStream(part, FileMode.Open))
                    {
                        sourceStream.CopyTo(destStream);
                    }
                }
            }
        }
    }
}