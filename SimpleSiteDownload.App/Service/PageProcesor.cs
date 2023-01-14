using SimpleSiteDownload.App.Helper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleSiteDownload.App.Service
{
    public class PageProcesor
    {
        private readonly string Site_Url;
        private readonly string LocalStoreBase;
        private long totalCount = 0;
        private long processedCount = 0;
        private ConcurrentQueue<KeyValuePair<string, string>> Queue = new ConcurrentQueue<KeyValuePair<string, string>> { };
        private ConcurrentDictionary<string, string> ProcessedLinkMap = new ConcurrentDictionary<string, string> { };

        public PageProcesor(string siteUrl, string localStoreBase)
        {
            Site_Url = siteUrl;
            LocalStoreBase = localStoreBase;
        }

        public void Boot(int parallelThread)
        {
            Queue.Enqueue(new KeyValuePair<string, string>("", "index.html"));

            Enumerable.Range(0, parallelThread).ToList().ForEach(_ => Task.Factory.StartNew(() =>
              {

                  while (true)
                  {
                      if (Queue.TryDequeue(out var item))
                      {
                          ProcessPage(item.Key, item.Value);
                      }
                      else { Thread.Sleep(200); }

                  }
              }));
        }



        public async Task ProcessPage(string sitePath, string localRef)
        {

            var uri = JoinUriSegments(Site_Url, sitePath);
            if (ProcessedLinkMap.ContainsKey(uri)) return;
            var response = await DownloadHelper.Download(uri);
            if (response.Content.Headers.GetValues("Content-Type").FirstOrDefault()?.Contains("text/html") ?? false)
            {
                var pageContent = await response.Content.ReadAsStringAsync();
                SetProcessed(uri, localRef);
                Console.WriteLine($"[{pageContent.Length}] {uri}");
                var linkList = RetrieveCrossLinkList(ref pageContent);
                EnqueueItems(linkList);
                FileSaveHelper.WriteFile(Encoding.UTF8.GetBytes(pageContent), Path.Combine(LocalStoreBase, localRef));
            }
            else
            {
                FileSaveHelper.WriteFile(await response.Content.ReadAsByteArrayAsync(), Path.Combine(LocalStoreBase, localRef));
            }

        }

        private void EnqueueItems(List<KeyValuePair<string, string>> itemList)
        {
            foreach (var item in itemList)
                if (!Queue.Any(x => x.Key == item.Key))
                {
                    Queue.Enqueue(item);
                    totalCount++;
                }
        }

        private void SetProcessed(string key, string value)
        {
            ProcessedLinkMap.TryAdd(key, value);
            processedCount++; 
            if (processedCount % 10 == 0)
            {
                Console.Title = $"[{Math.Round(totalCount / (double)processedCount, 2)}%] {processedCount} of {totalCount}; {FileSaveHelper.TotalBytesSavedString()} saved";
            }

        }


        private Random rand = new Random();

        private string GetRandomString(string orignalName, int length = 20)
        {
            var ext = orignalName.Split('.').LastOrDefault();

            if (ext != null)
            {
                ext = new Regex("^([A-z]*).*").Match(ext).Groups[1].Value;
            }
            else
            {
                ext = "html";
            }
            string charbase = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            return new string(Enumerable.Range(0, length)
                   .Select(_ => charbase[rand.Next(charbase.Length)])
                   .ToArray()) + $".{ext}";
        }

        private List<string> LinkAttributeList = new List<string>
        {
            "href","src"
        };
        public string JoinUriSegments(string uri, params string[] segments)
        {
            if (string.IsNullOrWhiteSpace(uri))
                return null;

            if (segments == null || segments.Length == 0)
                return uri;

            return segments.Aggregate(uri, (current, segment) => $"{current.TrimEnd('/')}/{segment.TrimStart('/')}");
        }
        private List<KeyValuePair<string, string>> RetrieveCrossLinkList(ref string pageContent)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(pageContent);
            var hrefNodeList = doc.QuerySelectorAll(string.Join(",", LinkAttributeList.Select(x => $"[{x}]")));
            var map = hrefNodeList.SelectMany(node => node.Attributes.Where(attr => LinkAttributeList.Contains(attr.Name.ToLower())))
                                .Select(x => x.Value).Distinct()
                                .ToDictionary(x => x, x => GetRandomString(x));

            doc.QuerySelectorAll(string.Join(",", LinkAttributeList.Select(x => $"[{x}]")))
                .ToList()
                .ForEach(node =>
                {
                    foreach (var attr in node.Attributes.Where(attr => LinkAttributeList.Contains(attr.Name.ToLower())))
                    {
                        attr.Value = map[attr.Value];
                    }
                });
            pageContent = doc.DocumentNode.OuterHtml;
            return map.ToList();
        }
    }
}
