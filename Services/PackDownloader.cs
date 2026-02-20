using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Hawkbat.Services
{
    public static class PackDownloader
    {
        public static async Task<bool> DownloadFileAsync(string url, string outputPath)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                byte[] data = await client.GetByteArrayAsync(url);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                await File.WriteAllBytesAsync(outputPath, data);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
