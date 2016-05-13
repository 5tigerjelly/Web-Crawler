using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary2
{
    class lastten : TableEntity
    {
        public lastten()
        {
            this.PartitionKey = "recenttenitems";
            this.RowKey = "1";
        }


        public lastten(string addten)
        {
            this.PartitionKey = "recenttenitems";
            this.RowKey = addten;
        }
    }
}
