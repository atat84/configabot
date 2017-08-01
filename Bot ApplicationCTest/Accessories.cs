using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bot_ApplicationCTest
{
    public class Accessories
    {
        public Accessories(string id, string name, string image)
        {
            accId = id;
            accName = name;
            accImage = image;
        }

        public string accId { get; set; }
        public string accName { get; set; }
        public string accImage { get; set; }

    }
}