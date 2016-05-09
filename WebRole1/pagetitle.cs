using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class pagetitle : TableEntity
    {
        public pagetitle() { }
        public pagetitle(string address)
        {
            this.PartitionKey = "error";
            this.RowKey = Guid.NewGuid().ToString();
            this.urlLink = address;
            this.pubdate = DateTime.Today;
        }

        public pagetitle(string title, string address, DateTime date)
        {
            this.PartitionKey = title;
            this.RowKey = Guid.NewGuid().ToString();
            this.urlLink = address;
            this.pubdate = date;
        }

        public string urlLink { get; set; }

        public DateTime pubdate { get; set; }
    }
}