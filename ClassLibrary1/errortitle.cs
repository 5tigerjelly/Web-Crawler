using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class errortitle : TableEntity
    {
        public errortitle() { }
        public errortitle(string address, string errorlog)
        {
            this.PartitionKey = "error";
            this.RowKey = new md5coding(address).encoded;
            this.urlLink = address;
            this.errorlog = errorlog;
        }

        public string urlLink { get; set; }
        public string errorlog { get; set; }
    }
}
