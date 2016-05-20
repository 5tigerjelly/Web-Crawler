using ClassLibrary1;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Web.Services;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.Web.Script.Services;

namespace WebRole1
{

    //questions
    //how to deal with cloudqueue and table as public fields

    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]

    [System.Web.Script.Services.ScriptService]
    public class admin2 : System.Web.Services.WebService
    {

        private CloudTable table;
        private CloudTable recentten;
        private CloudTable errortable;
        private CloudQueue htmlqueue;
        private CloudQueue xmlqueue;
        private CloudQueue stopgo;
        private bool start;

        public admin2()
        {
            table = getCloudTable("resulttable");
            recentten = getCloudTable("lastten");
            errortable = getCloudTable("errortable1");
            htmlqueue = getCloudQueue("htmlque");
            xmlqueue = getCloudQueue("xmlque");
            stopgo = getCloudQueue("stopgo");
            start = true;
        }

        //starts the crwal, if the user hits for the first time the message with all the robots.txt will go 
        //into the queue. If the process was stopped, on the go queue will go in.
        [WebMethod]
        public string restart()
        {
            if (start)
            {
                table = getCloudTable("resulttable");
                recentten = getCloudTable("lastten");
                errortable = getCloudTable("errortable1");

                CloudQueueMessage message = new CloudQueueMessage("http://cnn.com/robots.txt");
                CloudQueueMessage message1 = new CloudQueueMessage("http://bleacherreport.com/robots.txt");
                CloudQueueMessage message2 = new CloudQueueMessage("http://www.imdb.com/robots.txt");
                CloudQueueMessage message3 = new CloudQueueMessage("https://en.wikipedia.org/robots.txt");
                CloudQueueMessage message4 = new CloudQueueMessage("http://www.bbc.com/robots.txt");
                CloudQueueMessage message5 = new CloudQueueMessage("http://espn.go.com/robots.txt");
                CloudQueueMessage message6 = new CloudQueueMessage("http://www.forbes.com/robots.txt");
                
                xmlqueue.AddMessage(message);
                xmlqueue.AddMessage(message1);
                xmlqueue.AddMessage(message3);
                xmlqueue.AddMessage(message4);
                xmlqueue.AddMessage(message5);
                xmlqueue.AddMessage(message6);
                start = false;
            }
            CloudQueueMessage go = new CloudQueueMessage("go");
            stopgo.AddMessage(go);
            return "GO";
        }

        //sends a message to stop the crawl
        [WebMethod]
        public string stopCrawl()
        {
            stopgo = getCloudQueue("stopgo");
            CloudQueueMessage stop = new CloudQueueMessage("stop");
            stopgo.AddMessage(stop);
            return "STOP";
        }

        //clears the index, deletes all the tables and clears the queues sleeps for 40sec.
        [WebMethod]
        public string clearIndex()
        {
            table.DeleteIfExists();
            recentten.DeleteIfExists();
            errortable.DeleteIfExists();
            xmlqueue.Clear();
            htmlqueue.Clear();
            Thread.Sleep(40000);
            start = true;
            return "Done Clearing";
        }

        //helper method for cloudqueue returns a cloudqueue
        private CloudQueue getCloudQueue(string name)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(name);
            queue.CreateIfNotExists();

            return queue;
        }

        //grabs the cloud table, if the cloud table does not exist creats a new table
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

        //grabs a long string which holds the last ten urls inserted
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string lasttenpages()
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<resenturl>("lastten", "rowkey");
            TableResult retrievedResult = recentten.Execute(retrieveOperation);
            if (retrievedResult.Result != null)
            {
                return new JavaScriptSerializer().Serialize(retrievedResult.Result);
            }
            return new JavaScriptSerializer().Serialize(new pagetitle());
        }

        //retrieves the CPU and memory data from the cloud table
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string graphData()
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<resenturl>("memory", "cpu");
            TableResult retrievedResult = recentten.Execute(retrieveOperation);
            if (retrievedResult.Result != null)
            {
                return new JavaScriptSerializer().Serialize(retrievedResult.Result);
            }
            return new JavaScriptSerializer().Serialize(new pagetitle());
        }

        //retrieves the error urls from cloud table
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getErrortable()
        {
            Dictionary<string, string> errordic = new Dictionary<string, string>();
            TableQuery<errortitle> query = new TableQuery<errortitle>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "error"));
            foreach (errortitle entity in errortable.ExecuteQuery(query))
            {
                errordic.Add(entity.urlLink, entity.errorlog);
            }
            return new JavaScriptSerializer().Serialize(errordic);
        }

        //this method searches the cloud table to retrieve the title.
        //If not found, the url not found is sent
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchURL(string search)
        {
            string stripwww = formateURL(search);
            string encoded = new md5coding(stripwww).encoded;
            TableOperation retrieveOperation = TableOperation.Retrieve<pagetitle>("title", encoded);
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result != null)
            {
                return new JavaScriptSerializer().Serialize(retrievedResult.Result);
            }
            else
            {
                return search + " was not found.";
            }

        }

        //the user can add additional websites to parse.
        //It can be in the form of cnn.com, www.cnn.com, http://cnn.com it will all get http://www.cnn.com/robots.txt
        //then adds it to the xml queue to read
        [WebMethod]
        public string addURL(string search)
        {
            string stripwww = formateURL(search);
            if (!stripwww.EndsWith("robots.txt"))
            {
                stripwww = stripwww + "/robots.txt";
            }
            xmlqueue.AddMessage(new CloudQueueMessage(stripwww));

            return "done";
        }

        private string formateURL(string formatURL)
        {
            if (!formatURL.StartsWith("http"))
            {
                formatURL = "http://" + formatURL;
            }
            Uri tempuri = new Uri(formatURL);
            string stripwww = tempuri.Host + tempuri.AbsolutePath;
            if (stripwww.StartsWith("www"))
            {
                stripwww = stripwww.Replace("www.", "");
            }
            stripwww = "http://" + stripwww;
            return stripwww;
        }

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
    }
}
