using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class pagetitle : TableEntity
    {
        public pagetitle() { }
        public pagetitle(string address)
        {
            this.PartitionKey = "error";
            this.RowKey = address;
            this.pubdate = DateTime.Today;
        }

        public pagetitle(string title, string address, DateTime date)
        {
            this.PartitionKey = "title";
            this.RowKey = address;
            this.title = title;
            this.pubdate = date;
        }

        public string urlLink { get; set; }
        public DateTime pubdate { get; set; }
        public string title { get; set; }
        public int count { get; set; }
    }
}
