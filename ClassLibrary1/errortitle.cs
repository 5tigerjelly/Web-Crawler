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
            this.RowKey = CalculateMD5Hash(address);
            this.urlLink = address; //delete later
            this.errorlog = errorlog;
        }

        private string CalculateMD5Hash(string address)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(address);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
           return sb.ToString();
        }

        public string urlLink { get; set; }
        public string errorlog { get; set; }
    }
}
