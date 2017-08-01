using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bot_ApplicationCTest
{
    public class AccessoriesFamily
    {
        public AccessoriesFamily(string name, string image)
        {
            accFamilyName = name;
            accFamilyImage = image;
        }

        public string accFamilyName { get; set; }
        public string accFamilyImage { get; set; }
    }
}