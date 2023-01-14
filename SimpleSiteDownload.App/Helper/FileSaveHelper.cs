using System.IO;

namespace SimpleSiteDownload.App.Helper
{
    public static class FileSaveHelper
    {
        public static long TotalBytesSaved = 0;
        public static void WriteFile(byte[] content, string filePath)
        {
            EnsurePathExists(filePath);
            File.WriteAllBytes(filePath, content);
            TotalBytesSaved += content.Length;
        }

        public static void EnsurePathExists(string pathToFile)
        {
            try
            {

                var fileInfo = new FileInfo(pathToFile);
                if (!fileInfo.Directory.Exists) { fileInfo.Directory.Create(); }
            }
            catch { }
        }

        public static void EnsureHaveExtension(ref string pathToFile, string ext)
        {
            pathToFile = string.IsNullOrEmpty(new FileInfo(pathToFile).Extension) ? $"{pathToFile}.{ext}" : pathToFile;
        }

        public static string TotalBytesSavedString()
        {
            string[] result = new string[2];
            string[] sizes = { "B", "KB", "MB", "GB", "GB" }; 
            double adjustedSize = TotalBytesSaved;
            double testSize = 0;
            int order = 0;
            while (order < sizes.Length - 1)
            {
                testSize = adjustedSize / 1024;
                if (testSize >= 1) { adjustedSize = testSize; order++; }
                else { break; }
            }
            result[0] = $"{adjustedSize:f2}";
            result[1] = sizes[order];
            return string.Join("", result);
        }
    }
}
