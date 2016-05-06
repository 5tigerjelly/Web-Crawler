using HtmlAgilityPack;
using Microsoft.Language.Xml;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using RobotsTxt;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
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

        [WebMethod]
        public List<string> startCrawl()
        {
            /*

            CloudQueueMessage message = new CloudQueueMessage("http://cnn.com/robots.txt");
            queue.AddMessage(message);
            
            

            
            Robots robot = Robots.Load(content);*/
            List<string> allowList = new List<string>();
            List<string> disallowList = new List<string>();
            /*Regex regex = new Regex(@"\d+");
            HtmlWeb hw = new HtmlWeb();
            HtmlDocument doc = hw.Load("http://bleacherreport.com/nba");
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                HtmlAttribute att = link.Attributes["href"];
                resultlist.Add(att.Value);
            }*/
            //string content = new WebClient().DownloadString("http://cnn.com/robots.txt");

            CloudQueue xmlqueue = getCloudQueue("xmlque");

            WebClient client = new WebClient();
            Stream stream = client.OpenRead("http://cnn.com/robots.txt");
            StreamReader reader = new StreamReader(stream);
            string domain = "http://cnn.com";
            while (!reader.EndOfStream)
            {
                string temp = reader.ReadLine();
                if (temp.StartsWith("Sitemap:"))
                {
                    //sitemap .xml files
                    temp = temp.Replace("Sitemap: ", "");
                    CloudQueueMessage message = new CloudQueueMessage(temp);
                    xmlqueue.AddMessage(message);
                }
                else if (temp.StartsWith("Disallow:"))
                {
                    temp = temp.Replace("Disallow: ", "");
                    disallowList.Add(domain + temp);
                }
                else if (temp.StartsWith("Allow:"))
                {
                    temp = temp.Replace("Allow: ", "");
                    allowList.Add(domain + temp);
                }
                
            }

            while (true)
            {
                var tempval = xmlqueue.GetMessage();
                if (tempval != null)
                {

                } 
                break;
            }
            //resultlist.Add(content);
            //var root = Parser.ParseText("http://www.cnn.com/sitemaps/sitemap-interactive.xml");
            //root.Name;
            //regex
            // ^\/\/.*\..*$ for links without http in the front but has //
            // ^(http)s?(:\/\/).*$ for links that start with http://
            // ^.*cnn\.com.*$ for links that has cnn.com (within http or https)
            // ^.*bleacherreport\.com\/nba.*$ for links that are bleacherreport.com/nba (within http or https)
            // ^\/[\w|.|\/|-]+$ for relative links that starts with /


            return disallowList;
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
            xmlqueue.Clear();
            return "Done Clearing";
        }

        [WebMethod]
        public string getPageTitle()
        {
            //type in URL
            //grab title of the html from table index
            return "Hello World";
        }

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

        [WebMethod]
        public List<string> getXML()
        {
            List<string> result = new List<string>();
            CloudQueue xmlqueue = getCloudQueue("xmlque");
            CloudQueueMessage message = new CloudQueueMessage("http://www.cnn.com/sitemaps/sitemap-index.xml");
            xmlqueue.AddMessage(message);
            while (true)
            {
                CloudQueueMessage xmlLink = xmlqueue.GetMessage();
                if (xmlLink == null) { return result; }
                //string links = xmlLink.AsString;
                
                XElement sitemap = XElement.Load(xmlLink.AsString);

                XName url = XName.Get("url", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName loc = XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9");
                XName sitemaps = XName.Get("sitemap", "http://www.sitemaps.org/schemas/sitemap/0.9");

                XName temp = url;
                xmlqueue.DeleteMessage(xmlLink);
                //check if the url isnt used change to sitemap
                if (sitemap.Elements(temp).Count() == 0) { temp = sitemaps; }
                foreach (var urlElement in sitemap.Elements(temp))
                {
                    string locElement = urlElement.Element(loc).Value;
                    if (locElement.EndsWith(".xml"))
                    {
                        CloudQueueMessage message1 = new CloudQueueMessage(locElement);
                        xmlqueue.AddMessage(message1);
                    }
                    else {
                        //.html or no .html links but not XML links
                        result.Add(locElement);
                    }
                }
            }
        }
    }
}
