using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using System.Xml.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using HtmlAgilityPack;
using System.IO;
using ClassLibrary1;
using RobotsTxt;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private static Dictionary<Uri, Robots> robotsdic;

        private static CloudTable table;
        private static CloudTable recentten;
        private static CloudTable errortable;
        private static CloudQueue htmlqueue;
        private static CloudQueue xmlqueue;
        private static CloudQueue stopgo;
        private static List<int> memlist;
        private static List<int> cpulist;

        private static int totalurl;
        private static HashSet<string> htmllist;

        public override void Run()
        {
            memlist = new List<int>();
            cpulist = new List<int>();
            table = getCloudTable("resulttable");
            recentten = getCloudTable("lastten");
            errortable = getCloudTable("errortable1");
            htmlqueue = getCloudQueue("htmlque");
            xmlqueue = getCloudQueue("xmlque");
            stopgo = getCloudQueue("stopgo");

            totalurl = 0;
            htmllist = new HashSet<string>();
            robotsdic = new Dictionary<Uri, Robots>();

            bool checkstoporgo = true;
            while (true)
            {
                checkstoporgo = checkgostop(checkstoporgo);
                checkstoporgo = getXML(checkstoporgo);
                checkstoporgo = getHref(checkstoporgo);
            }
        }

        private CloudQueue getCloudQueue(string name)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(name);
            queue.CreateIfNotExists();

            return queue;
        }

        private CloudTable getCloudTable(string name)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(name);
            table.CreateIfNotExists();

            return table;
        }

        public bool getXML(bool check)
        {
            if (check)
            {
                while (check)
                {
                    Thread.Sleep(100);
                    new Task(getSysInfo).Start();
                    CloudQueueMessage xmlLink = xmlqueue.GetMessage();

                    if (xmlLink == null) { break; }
                    else if (xmlLink.AsString.EndsWith("robots.txt"))
                    {
                        addRobotstxt(xmlLink.AsString);
                        xmlqueue.DeleteMessage(xmlLink);
                    }
                    else
                    {
                        try
                        {
                            //xml
                            XElement sitemap = XElement.Load(xmlLink.AsString);
                            xmlqueue.DeleteMessage(xmlLink);

                            XName url = XName.Get("url", "http://www.sitemaps.org/schemas/sitemap/0.9");
                            XName urlX = XName.Get("url", "http://www.google.com/schemas/sitemap/0.9");
                            XName loc = XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9");
                            XName locX = XName.Get("loc", "http://www.google.com/schemas/sitemap/0.9");
                            XName sitemaps = XName.Get("sitemap", "http://www.sitemaps.org/schemas/sitemap/0.9");
                            XName time = XName.Get("lastmod", "http://www.sitemaps.org/schemas/sitemap/0.9");
                            XName news = XName.Get("news", "http://www.google.com/schemas/sitemap-news/0.9");
                            XName newspubdate = XName.Get("publication_date", "http://www.google.com/schemas/sitemap-news/0.9");
                            XName video = XName.Get("video", "http://www.google.com/schemas/sitemap-video/1.1");
                            XName videopuddate = XName.Get("publication_date", "http://www.google.com/schemas/sitemap-video/1.1");
                            XName temp = url;
                            DateTime fixedDate = new DateTime(2016, 3, 1);

                            //check if the url isnt used, then change to sitemap
                            if (sitemap.Elements(urlX).Count() != 0) { temp = urlX; }
                            else if (sitemap.Elements(sitemaps).Count() != 0) { temp = sitemaps; }
                            DateTime pubdate;
                            foreach (var urlElement in sitemap.Elements(temp))
                            {
                                if (urlElement.Element(loc) == null) { loc = locX; }
                                string locElement = urlElement.Element(loc).Value;
                                if (urlElement.Element(time) != null)
                                {
                                    pubdate = DateTime.Parse(urlElement.Element(time).Value);
                                }
                                else if (urlElement.Element(news) != null)
                                {
                                    pubdate = DateTime.Parse(urlElement.Element(news).Element(newspubdate).Value);
                                }
                                else if (urlElement.Element(video) != null)
                                {
                                    pubdate = DateTime.Parse(urlElement.Element(video).Element(videopuddate).Value);
                                }
                                else
                                {
                                    //no date found, just add
                                    pubdate = DateTime.Today;
                                }

                                //check if younger than 2 months
                                if (fixedDate < pubdate)
                                {
                                    if (locElement.EndsWith(".xml") || locElement.EndsWith(".gz"))
                                    {
                                        CloudQueueMessage message1 = new CloudQueueMessage(locElement);
                                        xmlqueue.AddMessageAsync(message1);
                                    }
                                    else
                                    {
                                        //.html or no .html links
                                        if (!htmllist.Contains(locElement))
                                        {
                                            htmllist.Add(locElement);
                                            CloudQueueMessage message1 = new CloudQueueMessage(locElement);
                                            htmlqueue.AddMessageAsync(message1);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e) { string errormsg = e.Message; }
                    }
                    check = checkgostop(check);
                }
            }
            return check;
        }

        private bool checkgostop(bool currentstate)
        {
            CloudQueueMessage stoporgo = stopgo.GetMessage();
            if (stoporgo == null)
            {
                return currentstate;
            }
            else if (stoporgo.AsString.Equals("go"))
            {
                stopgo.DeleteMessage(stoporgo);
                return true;
            }
            else
            {
                stopgo.DeleteMessage(stoporgo);
                return false;
            }
        }

        public bool getHref(bool check)
        {
            if (check)
            {
                HashSet<string> urlList = new HashSet<string>();
                HashSet<string> tableList = new HashSet<string>();
                List<string> lasttenadded = new List<string>();
                HtmlWeb hw = new HtmlWeb();
                pagetitle urlTableElement;
                HtmlDocument webpage = new HtmlDocument();
                while (check)
                {
                    Thread.Sleep(100);
                    new Task(getSysInfo).Start();
                    CloudQueueMessage xmlLink = htmlqueue.GetMessage();
                    if (xmlLink == null) { break; }
                    else if (!tableList.Contains(xmlLink.AsString))
                    {
                        string url = xmlLink.AsString;
                        tableList.Add(url);
                        ///
                        using (var client = new WebClient())
                        {
                            try
                            {
                                string linking = client.DownloadString(url);
                                webpage.LoadHtml(linking);

                                DateTime pubdate1;
                                string temptitle = webpage.DocumentNode.SelectSingleNode("//head/title").InnerText ?? "";
                                HtmlNode pubdate = webpage.DocumentNode.SelectSingleNode("//head/meta[@name='lastmod']");
                                if (pubdate != null)
                                {
                                    pubdate1 = DateTime.Parse(pubdate.Attributes["content"].Value);
                                }
                                else
                                {
                                    pubdate1 = DateTime.Today;
                                }

                                Uri tempuri = new Uri(url);
                                string stripwww = tempuri.Host + tempuri.PathAndQuery;
                                if (stripwww.StartsWith("www"))
                                {
                                    stripwww = stripwww.Replace("www.", "");
                                }
                                stripwww = "http://" + stripwww;

                                urlTableElement = new pagetitle(temptitle, stripwww, pubdate1);
                                TableOperation insertOp = TableOperation.InsertOrReplace(urlTableElement);
                                table.Execute(insertOp);
                                lasttenadded = updateUrl(lasttenadded, url, recentten, totalurl);
                                if (webpage.DocumentNode.SelectNodes("//a[@href]") != null)
                                {
                                    foreach (HtmlNode link in webpage.DocumentNode.SelectNodes("//a[@href]"))
                                    {
                                        totalurl++;
                                        string templink = link.Attributes["href"].Value;
                                        if (templink.StartsWith("//"))
                                        {
                                            templink = "http:" + templink;
                                        }
                                        else if (templink.StartsWith("/"))
                                        {
                                            templink = "http://" + tempuri.Host + templink;
                                        }
                                        if (templink.StartsWith("http"))
                                        {
                                            Uri currentUri2 = new Uri(templink);
                                            foreach (Uri root in robotsdic.Keys)
                                            {
                                                if (root.IsBaseOf(currentUri2) && !urlList.Contains(templink))
                                                {
                                                    if (robotsdic[root].IsPathAllowed("*", currentUri2.AbsolutePath))
                                                    {
                                                        urlList.Add(templink);
                                                        CloudQueueMessage message1 = new CloudQueueMessage(templink);
                                                        htmlqueue.AddMessageAsync(message1);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                //404 or website not found error
                                if (!urlList.Contains(xmlLink.AsString))
                                {
                                    urlList.Add(xmlLink.AsString);
                                    errortitle urlerrorElement = new errortitle(xmlLink.AsString, e.Message);
                                    TableOperation insertOp = TableOperation.InsertOrReplace(urlerrorElement);
                                    errortable.Execute(insertOp);
                                }
                            }
                        }
                    }
                    htmlqueue.DeleteMessage(xmlLink);
                    check = checkgostop(check);
                }
            }
            return check;
        }

        //updating the table with last ten urls
        private List<string> updateUrl(List<string> lasttenadded, string link, CloudTable recentten, int totalurl)
        {
            if (lasttenadded.Count < 10)
            {
                lasttenadded.Add(link);
            }
            else
            {
                lasttenadded.RemoveAt(0);
                lasttenadded.Add(link);
            }
            TableOperation retrieveOperation = TableOperation.Retrieve<resenturl>("lastten", "rowkey");
            TableResult retrievedResult = recentten.Execute(retrieveOperation);
            resenturl updateURL = (resenturl)retrievedResult.Result;
            if (updateURL != null)
            {
                updateURL.lastitems = string.Join(",", lasttenadded.ToArray());
                updateURL.count = updateURL.count + 1;
                updateURL.totalurl = totalurl;
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(updateURL);
                recentten.Execute(insertOrReplaceOperation);
            }
            else
            {
                resenturl lasttentitles = new resenturl(lasttenadded.ToString(), totalurl);
                TableOperation insertOp = TableOperation.InsertOrReplace(lasttentitles);
                recentten.Execute(insertOp);
            }
            return lasttenadded;
        }

        private void addRobotstxt(string robotslink)
        {
            List<string> xmllist = new List<string>();
            WebClient client = new WebClient();
            Stream stream = client.OpenRead(robotslink);
            StreamReader reader = new StreamReader(stream);
            robotslink = robotslink.Replace("/robots.txt", "");
            Uri rootdomain = new Uri(robotslink);
            bool sitemapnotfound = true;
            while (!reader.EndOfStream)
            {
                string temp = reader.ReadLine();
                if (temp.StartsWith("Sitemap:"))
                {
                    temp = temp.Replace("Sitemap: ", "");
                    if (!xmllist.Contains(temp) && (temp.Contains("cnn.com") || temp.Contains("/nba") || temp.Contains("imdb.com")
                        || temp.Contains("bbc.com") || temp.Contains("espn.go.com")
                        || temp.Contains("forbes.com") || temp.Contains("wikipedia.org")))
                    {
                        xmllist.Add(temp);
                        sitemapnotfound = false;
                        CloudQueueMessage message = new CloudQueueMessage(temp);
                        xmlqueue.AddMessageAsync(message);
                    }
                }
            }
            if (sitemapnotfound)
            {
                CloudQueueMessage message = new CloudQueueMessage("http://" + rootdomain.Host);
                htmlqueue.AddMessageAsync(message);
                htmllist.Add(rootdomain.Host);
            }
            Stream stream2 = client.OpenRead(robotslink);
            StreamReader reader2 = new StreamReader(stream2);
            string contents = reader2.ReadToEnd();
            Robots robot = Robots.Load(contents);
            robotsdic.Add(rootdomain, robot);
        }


        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }
        private void getSysInfo()
        {
            
            if (memlist.Count > 100)
            {
                memlist.RemoveAt(0);
                cpulist.RemoveAt(0);
            }
            PerformanceCounter theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes");
            theCPUCounter.NextValue();
            Thread.Sleep(500);
            cpulist.Add((int)theCPUCounter.NextValue());
            memlist.Add((int)theMemCounter.NextValue());

            TableOperation retrieveOperation = TableOperation.Retrieve<resenturl>("memory", "cpu");
            TableResult retrievedResult = recentten.Execute(retrieveOperation);
            resenturl updateURL = (resenturl)retrievedResult.Result;
            if (updateURL != null)
            {
                updateURL.memorylist = string.Join(",", memlist.ToArray());
                updateURL.cpulist = string.Join(",", cpulist.ToArray());
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(updateURL);
                recentten.Execute(insertOrReplaceOperation);
            }
            else
            {
                resenturl lasttentitles = new resenturl(string.Join(",", memlist.ToArray()), 
                    string.Join(",", cpulist.ToArray()));
                TableOperation insertOp = TableOperation.InsertOrReplace(lasttentitles);
                recentten.Execute(insertOp);
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
