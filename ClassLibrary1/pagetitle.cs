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
        public pagetitle(string address)
        {
            this.PartitionKey = "error";
            this.RowKey = CalculateMD5Hash(address);
            this.pubdate = DateTime.Today;
        }

        public pagetitle(string title, string address, DateTime date)
        {
            this.PartitionKey = "title";
            this.RowKey = CalculateMD5Hash(address);
            this.title = title;
            this.pubdate = date;
        }

        private string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public string urlLink { get; set; }
        public DateTime pubdate { get; set; }
        public string title { get; set; }
        public int count { get; set; }
    }
}
