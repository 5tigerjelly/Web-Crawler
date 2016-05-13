using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary1
{
    public class resenturl : TableEntity
    {

        public resenturl()
        {
            PartitionKey = "lastten";
            RowKey = "rowkey";
            lastitems = "";
        }

        public resenturl(string link)
        {
            PartitionKey = "lastten";
            RowKey = "rowkey";
            lastitems = link;
        }

        public string lastitems { get; set; }
    }
}
