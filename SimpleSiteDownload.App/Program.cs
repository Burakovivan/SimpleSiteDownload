using SimpleSiteDownload.App.Service;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSiteDownload.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var processor = new PageProcesor("https://provegas.ru/help/", "C:\\Users\\burak\\source\\repos\\SimpleSiteDownload\\SimpleSiteDownload.App\\site");
            processor.Boot(15);
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
