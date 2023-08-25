using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.converter
{
    // configuration node, holds reference to its parent and child nodes
    class ConfigNode
    {
        public ConfigNode parent { get; set; }
        public List<ConfigNode> childs { get; set; }
        public string className { get; set; }
        public string property { get; set; }
        public string value { get; set; }
        public string collectionType { get; set; }
        public string collection { get; set; }
        public object parentObj { get; set; }
        public int index { get; set; } = 0;
        public int count { get; set; } = 0;
        public ConfigNode(JToken configNode, ConfigNode parent = null, object parentObj = null)
        {
            this.parent = parent;
            this.childs = new List<ConfigNode>();
            this.className = configNode["class"]?.ToString();
            this.property = configNode["property"]?.ToString();
            this.value = configNode["value"]?.ToString();
            this.collectionType = configNode["collectionType"]?.ToString();
            this.collection = configNode["collection"]?.ToString();
            this.parentObj = parentObj;
            if (configNode["childs"] != null)
            {
                List<JToken> childsList = ((JArray)configNode["childs"]).ToList();
                childsList.Reverse();
                foreach (JToken childConfig in childsList)
                    childs.Add(new ConfigNode(childConfig, this));
            }
        }

        public bool IsCollection()
        {
            return collectionType != null;
        }

        public bool IsPrimitiveType()
        {
            return childs.Count == 0;
        }
    }
}
