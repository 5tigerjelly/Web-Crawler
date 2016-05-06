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

            CloudQueue queue = getCloudQueue("xmlque");

            WebClient client = new WebClient();
            Stream stream = client.OpenRead("http://cnn.com/robots.txt");
            StreamReader reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                string temp = reader.ReadLine();
                if (temp.StartsWith("Sitemap:"))
                {
                    temp.Replace("Sitemap: ", "");
                    CloudQueueMessage message = new CloudQueueMessage(temp);
                    queue.AddMessage(message);
                }
                else if (temp.StartsWith("Disallow:"))
                {
                    temp.Replace("Disallow: ", "");
                    disallowList.Add(temp);
                }
                else if (temp.StartsWith("Allow:"))
                {
                    temp.Replace("Allow: ", "");
                    allowList.Add(temp);
                }
                
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
    }
}
