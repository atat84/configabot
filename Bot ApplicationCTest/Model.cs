using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bot_ApplicationCTest
{
    public class Model
    {
        public Model(string id, string name, string image, string preconfigurationid)
        {
            modelId = id;
            modelName = name;
            modelImage = image;
            preconfigurationId = preconfigurationid;
        }

        public string modelId { get; set; }
        public string modelName { get; set; }
        public string modelImage { get; set; }
        public string preconfigurationId { get; set; }
    }
}