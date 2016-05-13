﻿using System.Security.Cryptography;
using System.Text;

namespace ClassLibrary1
{
    public class md5coding
    {
        public  md5coding(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            this.encoded =  sb.ToString();
        }

        public string encoded { get; set; }
    }
}
