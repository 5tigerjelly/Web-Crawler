using HtmlAgilityPack;
using Microsoft.Language.Xml;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using RobotsTxt;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Services;
using System.Xml.Linq;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
        //I think this can be private field outside the method because it 
        //does not have to do with multithreding.
        private List<string> allowList;
        private List<string> disallowList;

        [WebMethod]
        public string startCrawl(string address)
        {
            if (!address.StartsWith("http"))
            {
                address = "http://" + address;
            }
            Uri rootdomain = new Uri(address);
            string result = rootdomain.GetLeftPart(UriPartial.Authority) + "/robots.txt";
            CloudQueue xmlqueue = getCloudQueue("xmlque");
            CloudQueueMessage message = new CloudQueueMessage(result);
            xmlqueue.AddMessage(message);



            //resultlist.Add(content);
            //var root = Parser.ParseText("http://www.cnn.com/sitemaps/sitemap-interactive.xml");
            //root.Name;
            //regex
            // ^\/\/.*\..*$ for links without http in the front but has //
            // ^(http)s?(:\/\/).*$ for links that start with http://
            // ^.*cnn\.com.*$ for links that has cnn.com (within http or https)
            // ^.*bleacherreport\.com\/nba.*$ for links that are bleacherreport.com/nba (within http or https)
            // ^\/[\w|.|\/|-]+$ for relative links that starts with /


            return result;
        }

        [WebMethod]
        public string stopCrawl()
        {
            return "Hello World";
        }

        [WebMethod]
        public string clearIndex()
        {
            //table storage
            CloudQueue xmlqueue = getCloudQueue("xmlque");
            CloudQueue htmlqueue = getCloudQueue("htmlque");
            CloudQueue totalxml = getCloudQueue("totalxml");
            xmlqueue.Clear();
            htmlqueue.Clear();
            totalxml.Clear();
            allowList = null;
            disallowList = null;
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
        public string getXML(string urladdress)
        {
            clearIndex(); //delete this later!!!!!!!!!!!!!!
            HashSet<string> htmllist = new HashSet<string>();
            if (allowList == null || disallowList == null)
            {
                allowList = new List<string>();
                disallowList = new List<string>();
            }
            CloudQueue xmlqueue = getCloudQueue("xmlque");
            CloudQueue htmlqueue = getCloudQueue("htmlque");
            CloudQueue totalxml = getCloudQueue("totalxml");
            CloudQueueMessage message = new CloudQueueMessage(urladdress);
            xmlqueue.AddMessage(message);
            int tempcount = 0;
            while (true)
            {
                CloudQueueMessage xmlLink = xmlqueue.GetMessage();
                if (xmlLink == null) { break; }
                else if (xmlLink.AsString.EndsWith("robots.txt"))
                {
                    addRobotstxt(xmlLink.AsString);
                    xmlqueue.DeleteMessage(xmlLink);
                }
                else
                {
                    //xml
                    XElement sitemap = XElement.Load(xmlLink.AsString);
                    xmlqueue.DeleteMessage(xmlLink);

                    XName url = XName.Get("url", "http://www.sitemaps.org/schemas/sitemap/0.9");
                    XName urlX = XName.Get("url", "http://www.google.com/schemas/sitemap/0.9");
                    XName loc = XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9");
                    XName sitemaps = XName.Get("sitemap", "http://www.sitemaps.org/schemas/sitemap/0.9");
                    XName time = XName.Get("lastmod", "http://www.sitemaps.org/schemas/sitemap/0.9");
                    XName news = XName.Get("news", "http://www.google.com/schemas/sitemap-news/0.9");
                    XName newspubdate = XName.Get("publication_date", "http://www.google.com/schemas/sitemap-news/0.9");
                    XName video = XName.Get("video", "http://www.google.com/schemas/sitemap-video/1.1");
                    XName videopuddate = XName.Get("publication_date", "http://www.google.com/schemas/sitemap-video/1.1");
                    XName temp = url;
                    DateTime fixedDate = new DateTime(2016, 3, 1);

                    //check if the url isnt used, then change to sitemap
                    if (sitemap.Elements(temp).Count() == 0) { temp = sitemaps; }
                    else if (sitemap.Elements(temp).Count() == 0) { temp = urlX; }
                    DateTime pubdate;
                    foreach (var urlElement in sitemap.Elements(temp))
                    {
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
                                totalxml.AddMessage(message1);
                            }
                            else
                            {
                                //.html or no .html links but not XML links
                                if (locElement.Equals("http://www.cnn.com/profiles/thom-patterson-profile"))
                                {
                                    tempcount++;
                                    if (tempcount == 3)
                                    {
                                        string what = "the_heck";
                                    }
                                }
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
            }
            return "done";
        }

        private void addRobotstxt(string robotslink)
        {
            CloudQueue xmlqueue = getCloudQueue("xmlque");
            CloudQueue totalxml = getCloudQueue("totalxml");
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
                    CloudQueueMessage message = new CloudQueueMessage(temp);
                    xmlqueue.AddMessage(message);
                    totalxml.AddMessage(message);

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
        public string exportQue()
        {
            CloudQueue xmlqueue = getCloudQueue("xmlque");
            using (StreamWriter writer = new StreamWriter("C:/Users/iGuest/Desktop/quelist.txt"))
            {
                while (true)
                {
                    CloudQueueMessage xmlLink = xmlqueue.GetMessage();
                    if (xmlLink != null)
                    {
                        writer.WriteLine(xmlLink.AsString);
                    }
                    else { break; }
                }
            }

            xmlqueue = getCloudQueue("totalxml");
            using (StreamWriter writer = new StreamWriter("C:/Users/iGuest/Desktop/totalXML.txt"))
            {
                while (true)
                {
                    CloudQueueMessage xmlLink = xmlqueue.GetMessage();
                    if (xmlLink != null)
                    {
                        writer.WriteLine(xmlLink.AsString);
                    }
                    else { break; }
                }
            }

            xmlqueue = getCloudQueue("htmlque");
            using (StreamWriter writer = new StreamWriter("C:/Users/iGuest/Desktop/htmlist.txt"))
            {
                while (true)
                {
                    CloudQueueMessage xmlLink = xmlqueue.GetMessage();
                    if (xmlLink != null)
                    {
                        writer.WriteLine(xmlLink.AsString);
                    }
                    else { break; }
                }
            }

            
            return "done";
        }

        [WebMethod]
        public List<string> getHref(string urladdress)
        {
            List<string> result = new List<string>();
            HashSet<string> urlList = new HashSet<string>();
            HashSet<string> tableList = new HashSet<string>();
            clearIndex(); //delete this later!!!!!!!!!!!!!!
            CloudTable table = getCloudTable("resulttable");
            CloudTable errortable = getCloudTable("errortable");
            CloudQueue htmlqueue = getCloudQueue("htmlque");
            CloudQueueMessage message = new CloudQueueMessage(urladdress);
            htmlqueue.AddMessage(message);
            HtmlWeb hw = new HtmlWeb();
            HtmlDocument webpage;
            Uri currentUri;
            pagetitle urlTableElement;
            while (true)
            {
                Thread.Sleep(500);
                CloudQueueMessage xmlLink = htmlqueue.GetMessage();
                if (xmlLink == null) { break; }
                else if (!tableList.Contains(xmlLink.AsString))
                {
                    tableList.Add(xmlLink.AsString);
                    webpage = hw.Load(xmlLink.AsString);
                    if (hw.StatusCode == HttpStatusCode.OK)
                    {
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


                            urlTableElement = new pagetitle(temptitle, xmlLink.AsString, pubdate1);
                            TableOperation insertOp = TableOperation.Insert(urlTableElement);
                            table.Execute(insertOp);

                            foreach (HtmlNode link in webpage.DocumentNode.SelectNodes("//a[@href]"))
                            {
                                string templink = link.Attributes["href"].Value;
                                if (templink.StartsWith("//"))
                                {
                                    templink = "http:" + templink;
                                }
                                else if (templink.StartsWith("/"))
                                {
                                    currentUri = new Uri(xmlLink.AsString);
                                    templink = "http://" + currentUri.Host + templink;
                                }

                                if (templink.StartsWith("http") && (templink.Contains("cnn.com")
                                    || templink.Contains("bleacherreport.com/nba")))
                                {
                                    if (!urlList.Contains(templink))
                                    {
                                        urlList.Add(templink);
                                        result.Add(templink);
                                        CloudQueueMessage message1 = new CloudQueueMessage(templink);
                                        htmlqueue.AddMessage(message1);
                                    }

                                }
                            }
                        }
                        else
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
                        }
                        
                        
                    }
                    else
                    {
                        //404 or website not found error
                        urlTableElement = new pagetitle(xmlLink.AsString);
                        TableOperation insertOp = TableOperation.Insert(urlTableElement);
                        table.Execute(insertOp);
                    }
                }
                htmlqueue.DeleteMessage(xmlLink);
            }
            return result;
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
    }
}
