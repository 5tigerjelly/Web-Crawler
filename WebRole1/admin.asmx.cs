using ClassLibrary1;
using HtmlAgilityPack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Services;
using System.Xml.Linq;

namespace WebRole1
{

    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]

    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        //I think this can be private field outside the method because it 
        //does not have to do with multithreding.
        private List<string> allowList;
        private List<string> disallowList;

        [WebMethod]
        public string addtwolinks()
        {
            CloudQueue xmlqueue = getCloudQueue("xmlque");
            CloudQueueMessage message = new CloudQueueMessage("http://cnn.com/robots.txt");
            CloudQueueMessage message1 = new CloudQueueMessage("http://bleacherreport.com/robots.txt");
            //xmlqueue.AddMessage(message);
            xmlqueue.AddMessage(message1);
            //regex
            // ^\/\/.*\..*$ for links without http in the front but has //
            // ^(http)s?(:\/\/).*$ for links that start with http://
            // ^.*cnn\.com.*$ for links that has cnn.com (within http or https)
            // ^.*bleacherreport\.com\/nba.*$ for links that are bleacherreport.com/nba (within http or https)
            // ^\/[\w|.|\/|-]+$ for relative links that starts with /
            return "DONE";
        }

        [WebMethod]
        public void run()
        {
            bool checkstoporgo = true;         
            while (true)
            {
                checkstoporgo = checkgostop(checkstoporgo);
                checkstoporgo = getXML(checkstoporgo);
                checkstoporgo = getHref(checkstoporgo);
            }
        }

        [WebMethod]
        public string restart()
        {
            CloudQueue stopgo = getCloudQueue("stopgo");
            CloudQueueMessage message = new CloudQueueMessage("go");
            stopgo.AddMessage(message);
            return "GOOOOOOOO";
        }

        [WebMethod]
        public string stopCrawl()
        {
            CloudQueue stopgo = getCloudQueue("stopgo");
            CloudQueueMessage message = new CloudQueueMessage("stop");
            stopgo.AddMessage(message);
            return "STTTOOOOOOOPPPP";
        }

        [WebMethod]
        public string clearIndex()
        {
            //table storage
            CloudQueue xmlqueue = getCloudQueue("xmlque");
            CloudQueue htmlqueue = getCloudQueue("htmlque");
            CloudTable table = getCloudTable("resulttable");
            CloudTable recentten = getCloudTable("lastten");
            CloudTable errortable = getCloudTable("errortable1");
            table.Delete();
            recentten.Delete();
            errortable.Delete();
            xmlqueue.Clear();
            htmlqueue.Clear();
            allowList = null;
            disallowList = null;
            Thread.Sleep(40000);
            return "Done Clearing";
        }

        [WebMethod]
        public string getPageTitle()
        {
            //type in URL
            //grab title of the html from table index
            return "Hello World";
        }

        //helper method for cloudqueue returns a cloudqueue
        private CloudQueue getCloudQueue(string name)
        {
            Queue<string> que = new Queue<string>();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(name);
            queue.CreateIfNotExists();

            return queue;
        }

        private CloudTable getCloudTable(string name)
        {
            Queue<string> que = new Queue<string>();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(name);
            table.CreateIfNotExists();

            return table;
        }

        [WebMethod]
        public bool getXML(bool check)
        {
            if (check)
            {
                CloudQueue htmlqueue = getCloudQueue("htmlque");
                CloudQueue xmlqueue = getCloudQueue("xmlque");
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
            CloudQueue stopgo = getCloudQueue("stopgo");
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

        [WebMethod]
        public bool getHref(bool check)
        {
            if (check)
            {
                CloudTable table = getCloudTable("resulttable");
                CloudTable recentten = getCloudTable("lastten");
                CloudTable errortable = getCloudTable("errortable1");
                CloudQueue htmlqueue = getCloudQueue("htmlque");
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

                                    urlTableElement = new pagetitle(temptitle, url, pubdate1);
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
                                                templink = "http://" + new Uri(url).Host + templink;
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
            CloudQueue xmlqueue = getCloudQueue("xmlque");
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

        [WebMethod]
        public List<string> readTable()
        {
            List<string> result = new List<string>();
            CloudTable table = getCloudTable("resulttable");
            TableQuery<pagetitle> rangequery = new TableQuery<pagetitle>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.NotEqual, "error"));
            foreach (pagetitle entaty in table.ExecuteQuery(rangequery))
            {
                result.Add(entaty.PartitionKey +" "+ entaty.urlLink +" "+ entaty.pubdate);
            }
            return result;
        }



        [WebMethod]
        public List<string> getpageTitle()
        {
            CloudTable recentten = getCloudTable("lastten");
            TableOperation retrieveOperation = TableOperation.Retrieve<resenturl>("lastten", "rowkey");
            TableResult retrievedResult = recentten.Execute(retrieveOperation);
            if (retrievedResult.Result != null)
            {
                return ((resenturl)retrievedResult.Result).lastitems.Split(',').ToList();
            }
            return  new List<string>();
        }

        [WebMethod]
        public string searchURL(string search)
        {
            CloudTable table = getCloudTable("resulttable");
            string encoded = new md5coding(search).encoded;
            TableOperation retrieveOperation = TableOperation.Retrieve<pagetitle>("title", encoded);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result != null)
            {
                return ((pagetitle)retrievedResult.Result).title;
            }
            else
            {
                return search + " was not found.";
            }
            
        }

        /*        
        public string HelloWorld()
        {
            string thing = "/pointrollfdsa/";
            WebClient client = new WebClient();
            string thing2 = client.DownloadString("http://cnn.com/robots.txt");
            Robots robot = Robots.Load(thing2);
            if (robot.IsPathAllowed("*", thing))
            {
                return "yes";
            }
            else
            {
                return "no";
            }
        }*/

        [WebMethod]
        public int XmlQueueCount()
        {
            return queueCounter("xmlque");
        }

        [WebMethod]
        public int HTMLQueueCount()
        {
            return queueCounter("htmlque");
        }

        private int queueCounter(string quename)
        {
            CloudQueue q = getCloudQueue(quename);
            q.FetchAttributes();
            int? queueCount = q.ApproximateMessageCount;
            return (int)queueCount;
        }
    }
}
