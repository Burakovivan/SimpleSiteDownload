using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleSiteDownload.App.Helper
{
    public static class DownloadHelper
    {
        public static async Task<HttpResponseMessage> Download(string url)
        {
            using (var client = new HttpClient())
            {
                return await client.GetAsync(url);
            }
        }
    }
}
