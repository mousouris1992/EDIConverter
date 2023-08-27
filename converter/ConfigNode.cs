using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.converter
{
    /// <summary>
    /// A Tree Node implementation, that additionally holds configuration data.
    /// </summary>
    public class ConfigNode
    {
        private class CollectionData
        {
            public string CollectionType { get; set; }
            public string Collection { get; set; }
            public int Index { get; set; } = 0;
            public int Count { get; set; } = 1;
        }
        public ConfigNode Parent { get; set; }
        public List<ConfigNode> Childs { get; set; } = new List<ConfigNode>();
        public string ClassName { get; set; }
        public string Property { get; set; }
        public string Value { get; set; }
        public object ModelContext { get; set; }

        private CollectionData collectionData = new CollectionData();

        private Dictionary<int, object> ParserContext = new Dictionary<int, object>();

        /// <summary>
        /// Constructs a ConfigNode instance from a JToken configuration,
        /// a ConfigNode parent and a ModelParentObj can be also provided optionally.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="parent"></param>
        /// <param name="ModelContext"></param>
        public ConfigNode(JToken config, ConfigNode parent = null, object modelContext = null)
        {
            Parent = parent;
            ModelContext = modelContext;
            ProcessConfiguration(config);
        }

        private void ProcessConfiguration(JToken config)
        {
            ClassName = config["class"]?.ToString();
            Property = config["property"]?.ToString();
            Value = config["value"]?.ToString();
            collectionData.CollectionType = config["collectionType"]?.ToString();
            collectionData.Collection = config["collection"]?.ToString();
            ProcessChilds(config);
        }

        private void ProcessChilds(JToken config)
        {
            if (config["childs"] != null)
            {
                List<JToken> childConfigs = ((JArray)config["childs"]).ToList();
                childConfigs.Reverse();
                foreach (JToken childConfig in childConfigs)
                    // TODO stack traversal maybe instead of recursion
                    Childs.Add(new ConfigNode(childConfig, this));
            }
        }

        public void SetParserContext(object obj)
        {
            ParserContext[collectionData.Index] = obj;
        }

        public object GetParserContext()
        {
            object obj = ParserContext.GetValueOrDefault(collectionData.Index);
            if (obj == null && IsCollection())
                obj = ParserContext.GetValueOrDefault(0);
            return obj;
        }

        public void Visit()
        {
            collectionData.Index++;
        }

        public bool IsVisited()
        {
            return collectionData.Index > 0;
        }

        public bool CanVisit()
        {
            return collectionData.Index < collectionData.Count;
        }

        public string GetCollectionType()
        {
            return collectionData.CollectionType;
        }

        public string GetCollection()
        {
            return collectionData.Collection;
        }

        public int GetIndex()
        {
            return collectionData.Index;
        }

        public void SetCount(int count)
        {
            collectionData.Count = count;
        }

        public int GetCount()
        {
            return collectionData.Count;
        }

        public void IncrementIndex()
        {
            collectionData.Index++;
        }

        public void ResetIndex()
        {
            collectionData.Index = 0;
            foreach (ConfigNode child in Childs)
                child.ResetIndex();
        }

        public void SetIndex(int index)
        {
            collectionData.Index = index;
        }

        public bool IsCollection()
        {
            return collectionData.CollectionType != null;
        }

        public bool IsFinal()
        {
            return Childs.Count == 0;
        }
    }
}
