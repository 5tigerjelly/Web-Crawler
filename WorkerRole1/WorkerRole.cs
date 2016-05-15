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

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private List<string> allowList;
        private List<string> disallowList;
        private CloudTable table;
        private CloudTable recentten;
        private CloudTable errortable;
        private CloudQueue htmlqueue;
        private CloudQueue xmlqueue;
        private CloudQueue stopgo;
        private CloudStorageAccount storageAccount;

        public override void Run()
        {
            storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            table = getCloudTable("resulttable");
            recentten = getCloudTable("lastten");
            errortable = getCloudTable("errortable1");
            htmlqueue = getCloudQueue("htmlque");
            xmlqueue = getCloudQueue("xmlque");
            stopgo = getCloudQueue("stopgo");
            
            bool checkstoporgo = false;
            while (true)
            {
                checkstoporgo = checkgostop(checkstoporgo);
                checkstoporgo = getXML(checkstoporgo);
                checkstoporgo = getHref(checkstoporgo);
            }
        }

        private CloudQueue getCloudQueue(string name)
        {
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(name);
            queue.CreateIfNotExists();

            return queue;
        }

        private CloudTable getCloudTable(string name)
        {
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(name);
            table.CreateIfNotExists();

            return table;
        }

        public bool getXML(bool check)
        {
            if (check)
            {
                HashSet<string> htmllist = new HashSet<string>();
                while (check)
                {
                    Thread.Sleep(100);
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
                                    if (locElement.EndsWith(".xml"))
                                    {
                                        CloudQueueMessage message1 = new CloudQueueMessage(locElement);
                                        xmlqueue.AddMessage(message1);
                                    }
                                    else
                                    {
                                        //.html or no .html links but not XML links
                                        if (!htmllist.Contains(locElement))
                                        {
                                            htmllist.Add(locElement);
                                            CloudQueueMessage message1 = new CloudQueueMessage(locElement);
                                            htmlqueue.AddMessage(message1);
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
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
                List<string> result = new List<string>();
                Uri cnn = new Uri("http://cnn.com/");
                Uri bleach = new Uri("http://bleacherreport.com/");
                HashSet<string> urlList = new HashSet<string>();
                HashSet<string> tableList = new HashSet<string>();

                List<string> lasttenadded = new List<string>();
                HtmlWeb hw = new HtmlWeb();
                pagetitle urlTableElement;
                HtmlDocument webpage = new HtmlDocument();
                while (check)
                {
                    Thread.Sleep(100);
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
                                HtmlNode pagetitlestring = webpage.DocumentNode.SelectSingleNode("//head/title");
                                if (pagetitlestring != null)
                                {
                                    string temptitle = pagetitlestring.InnerText;
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
                                    TableOperation insertOp = TableOperation.Insert(urlTableElement);
                                    table.Execute(insertOp);
                                    lasttenadded = updateUrl(lasttenadded, url, recentten);
                                    if (webpage.DocumentNode.SelectNodes("//a[@href]") != null)
                                    {
                                        foreach (HtmlNode link in webpage.DocumentNode.SelectNodes("//a[@href]"))
                                        {
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
                                                if (cnn.IsBaseOf(currentUri2) || bleach.IsBaseOf(currentUri2))
                                                {
                                                    if (!urlList.Contains(templink))
                                                    {
                                                        bool checkdisallow = true;
                                                        foreach (string disallowlink in disallowList)
                                                        {
                                                            if (templink.Contains(disallowlink))
                                                            {
                                                                checkdisallow = false;
                                                                foreach (string allowlink in allowList)
                                                                {
                                                                    if (templink.Contains(allowlink))
                                                                    {
                                                                        checkdisallow = true;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        if (checkdisallow)
                                                        {

                                                            urlList.Add(templink);
                                                            result.Add(templink);
                                                            CloudQueueMessage message1 = new CloudQueueMessage(templink);
                                                            htmlqueue.AddMessage(message1);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                /*else
                                {
                                    //redirecting page with no title
                                    string newpage = webpage.DocumentNode.SelectSingleNode("//head/link").Attributes["href"].Value;
                                    if (newpage.StartsWith("http") && (newpage.Contains("cnn.com")
                                            || newpage.Contains("bleacherreport.com/nba")))
                                    {
                                        if (!urlList.Contains(newpage))
                                        {
                                            urlList.Add(newpage);
                                            result.Add(newpage);
                                            CloudQueueMessage message1 = new CloudQueueMessage(newpage);
                                            htmlqueue.AddMessage(message1);
                                        }

                                    }
                                }*/


                            }
                            catch (Exception e)
                            {
                                //404 or website not found error
                                if (!urlList.Contains(xmlLink.AsString))
                                {
                                    urlList.Add(xmlLink.AsString);
                                    errortitle urlerrorElement = new errortitle(xmlLink.AsString, e.Message);
                                    TableOperation insertOp = TableOperation.Insert(urlerrorElement);
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
        private List<string> updateUrl(List<string> lasttenadded, string link, CloudTable recentten)
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
                TableOperation insertOrReplaceOperation = TableOperation.InsertOrReplace(updateURL);
                recentten.Execute(insertOrReplaceOperation);
            }
            else
            {
                resenturl lasttentitles = new resenturl(lasttenadded.ToString());
                TableOperation insertOp = TableOperation.Insert(lasttentitles);
                recentten.Execute(insertOp);
            }
            return lasttenadded;
        }

        private void addRobotstxt(string robotslink)
        {
            if (allowList == null || disallowList == null)
            {
                allowList = new List<string>();
                disallowList = new List<string>();
            }
            WebClient client = new WebClient();
            Stream stream = client.OpenRead(robotslink);
            StreamReader reader = new StreamReader(stream);
            //string domain = robotslink.Replace("/robots.txt", "");
            Uri domain = new Uri(robotslink);
            while (!reader.EndOfStream)
            {
                string temp = reader.ReadLine();
                if (temp.StartsWith("Sitemap:"))
                {
                    //sitemap .xml files
                    temp = temp.Replace("Sitemap: ", "");
                    if (temp.Contains("cnn.com") || temp.Contains("/nba"))
                    {
                        CloudQueueMessage message = new CloudQueueMessage(temp);
                        xmlqueue.AddMessage(message);
                    }
                }
                else if (temp.StartsWith("Disallow:"))
                {
                    temp = temp.Replace("Disallow: ", "");
                    disallowList.Add(domain.Host + temp);
                }
                else if (temp.StartsWith("Allow:"))
                {
                    temp = temp.Replace("Allow: ", "");
                    allowList.Add(domain.Host + temp);
                }
            }
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
