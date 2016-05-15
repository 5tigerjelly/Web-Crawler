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
            xmlqueue.AddMessage(message);
            xmlqueue.AddMessage(message1);
            restart();
            //regex
            // ^\/\/.*\..*$ for links without http in the front but has //
            // ^(http)s?(:\/\/).*$ for links that start with http://
            // ^.*cnn\.com.*$ for links that has cnn.com (within http or https)
            // ^.*bleacherreport\.com\/nba.*$ for links that are bleacherreport.com/nba (within http or https)
            // ^\/[\w|.|\/|-]+$ for relative links that starts with /
            return "DONE";
        }

        [WebMethod]
        public string restart()
        {
            CloudQueue stopgo = getCloudQueue("stopgo");
            CloudQueueMessage message = new CloudQueueMessage("go");
            stopgo.AddMessage(message);
            return "GO";
        }

        [WebMethod]
        public string stopCrawl()
        {
            CloudQueue stopgo = getCloudQueue("stopgo");
            CloudQueueMessage message = new CloudQueueMessage("stop");
            stopgo.AddMessage(message);
            return "STOP";
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
        public List<string> lasttenpages()
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

            if (!search.StartsWith("http"))
            {
                search = "http://" + search;
            }
            Uri tempuri = new Uri(search);
            string stripwww = tempuri.Host + tempuri.PathAndQuery;
            if (stripwww.StartsWith("www"))
            {
                stripwww = stripwww.Replace("www.", "");
            }
            stripwww = "http://" + stripwww;
            return stripwww;
            string encoded = new md5coding(stripwww).encoded;
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
