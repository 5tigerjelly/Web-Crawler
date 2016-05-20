using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class pagetitle : TableEntity
    {
        public pagetitle() { }

        public pagetitle(string title, string address, DateTime date)
        {
            this.PartitionKey = "title";
            this.RowKey = new md5coding(address).encoded;
            this.title = title;
            this.pubdate = date;
            this.urlLink = address; //delete later
            this.Timestamp = DateTime.Now;
        }

        public string urlLink { get; set; }
        public DateTime pubdate { get; set; }
        public string title { get; set; }
        public int count { get; set; }
    }
}
