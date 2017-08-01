using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bot_ApplicationCTest
{
    public class Configuration
    {

        public Configuration(string confid, string image, string price)
        {
            cid = confid;
            confImage = image;
            confPrice = price;
        }

        public string cid { get; set; }
        public string confImage { get; set; }
        public string confPrice { get; set; }

    }
}