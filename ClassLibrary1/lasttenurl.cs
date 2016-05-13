using Microsoft.WindowsAzure.Storage.Table;

namespace ClassLibrary1
{
    class lasttenurl : TableEntity
    {
        public lasttenurl()
        {
            this.PartitionKey = "tenurl";
            this.RowKey = "";
        }


    }
}
