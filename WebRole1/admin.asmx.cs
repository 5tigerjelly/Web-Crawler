using ClassLibrary1;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Web.Services;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.Web.Script.Services;

namespace WebRole1
{

    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]

    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {
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
            stopCrawl();
            Thread.Sleep(5000);
            //table storage
            CloudQueue xmlqueue = getCloudQueue("xmlque");
            CloudQueue htmlqueue = getCloudQueue("htmlque");
            CloudTable table = getCloudTable("resulttable");
            CloudTable recentten = getCloudTable("lastten2");
            CloudTable errortable = getCloudTable("errortable1");
            table.DeleteIfExists();
            recentten.DeleteIfExists();
            errortable.DeleteIfExists();
            xmlqueue.Clear();
            htmlqueue.Clear();
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
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string lasttenpages()
        {
            CloudTable recentten = getCloudTable("lastten2");
            TableOperation retrieveOperation = TableOperation.Retrieve<resenturl>("lastten", "rowkey");
            TableResult retrievedResult = recentten.Execute(retrieveOperation);
            if (retrievedResult.Result != null)
            {
                var json = new JavaScriptSerializer().Serialize(retrievedResult.Result);
                return json;
            }
            return new JavaScriptSerializer().Serialize(new pagetitle());
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
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string XmlQueueCount()
        {
            return new JavaScriptSerializer().Serialize(queueCounter("xmlque"));
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string HTMLQueueCount()
        {
            return new JavaScriptSerializer().Serialize(queueCounter("htmlque"));
        }

        private int queueCounter(string quename)
        {
            CloudQueue q = getCloudQueue(quename);
            q.FetchAttributes();
            int? queueCount = q.ApproximateMessageCount;
            return (int)queueCount;
        }

        [WebMethod]
        public int getCPUPerformance()
        {
            PerformanceCounter theCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            theCPUCounter.NextValue();
            Thread.Sleep(500);
            return (int)theCPUCounter.NextValue();
            
        }


        [WebMethod]
        public int getPerformance()
        {
            using (PerformanceCounter theMemCounter = new PerformanceCounter("Memory", "Available MBytes"))
            {
                return (int)theMemCounter.NextValue();
            }
        }
    }
}
