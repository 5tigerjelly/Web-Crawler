using HtmlAgilityPack;
using Microsoft.Language.Xml;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using RobotsTxt;
using System;
using System.Collections.Generic;
using System.Configuration;
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
            /*Queue<string> que = new Queue<string>();
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("myurl");
            queue.CreateIfNotExists();

            CloudQueueMessage message = new CloudQueueMessage("http://cnn.com/robots.txt");
            queue.AddMessage(message);
            
            

            string content = new WebClient().DownloadString("http://cnn.com/robots.txt");
            Robots robot = Robots.Load(content);*/
            List<string> resultlist = new List<string>();
            HtmlWeb hw = new HtmlWeb();
            HtmlDocument doc = hw.Load("http://www.cnn.com/2016/05/05/asia/australia-isis-leader-killed/index.html");
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                HtmlAttribute att = link.Attributes["href"];
                resultlist.Add(att.Value);
            }
            //var root = Parser.ParseText("http://www.cnn.com/sitemaps/sitemap-interactive.xml");
            //root.Name;
            return resultlist;
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
            return "Hello World";
        }

        [WebMethod]
        public string getPageTitle()
        {
            //type in URL
            //grab title of the html from table index
            return "Hello World";
        }
    }
}
