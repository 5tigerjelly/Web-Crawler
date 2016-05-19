using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace ClassLibrary1
{
    public class resenturl : TableEntity
    {

        public resenturl()
        {
            PartitionKey = "lastten";
            RowKey = "rowkey";
            lastitems = "";
            count = 1;
            totalurl = 1;
        }

        public resenturl(string link, int total)
        {
            PartitionKey = "lastten";
            RowKey = "rowkey";
            lastitems = link;
            count = 1;
            totalurl = total;
            Timestamp = DateTime.Now;
        }

        public resenturl(string cpu, string memory)
        {
            PartitionKey = "memory";
            RowKey = "cpu";
            memorylist = memory;
            cpulist = cpu;
            Timestamp = DateTime.Now;
        }

        public string lastitems { get; set; }
        public int count { get; set; }
        public int totalurl { get; set; }
        public string memorylist { get; set; }
        public string cpulist { get; set; }
    }
}
