using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EDIConverter
{
    public class Converter
    {
        private Stack<JToken> childs = new Stack<JToken>();
        
        private Stack<CollectionInfo> collections = new Stack<CollectionInfo>();

        private JToken? currentNode;

        private object? parentObject;

        private string? inputContent;

        public Model ToModel(String configFile, String inputContent)
        {
            Model model = new Model();
            JObject config = JObject.Parse(configFile);
            InitializeState(config, inputContent, model);
            TraverseNodes();
            return model;
        }

        private void InitializeState(JObject config, string inputContent, Model model)
        {
            this.inputContent = inputContent;

            // get childs list
            List<JToken> childsList = ((JArray)config["childs"]).ToList();
            childsList.Reverse();

            // initialize stacks
            childs = new Stack<JToken>(childsList);
            collections.Clear();

            // initialize objects 
            currentNode = null;
            parentObject = model;
        }

        private void TraverseNodes()
        {
            while (childs.Count > 0)
            {
                currentNode = childs.Peek();
                if (IsCollectionNode())
                {
                    bool skip = HandleCollectionNode();
                    if (skip)
                        continue;
                }
                HandleNodeProperty();
                if (!IsCollectionNode())
                    childs.Pop();
                PushChildNodes();
            }
        }
      
        private void PushChildNodes()
        {
            List<JToken> currentNodeChilds = currentNode["childs"] != null ? currentNode["childs"].ToList() : new List<JToken>();
            currentNodeChilds.Reverse();
            foreach (JToken child in currentNodeChilds)
                childs.Push(child);
        }

        private bool HandleCollectionNode()
        {
            bool skip = false;
            if (IsCollectionNodeVisited())
            {
                CollectionInfo collectionInfo = collections.Peek();
                collectionInfo.index++;
                if (collectionInfo.index >= collectionInfo.count)
                {
                    childs.Pop();
                    collections.Pop();
                    skip = true;
                }
                else
                    parentObject = collectionInfo.parentObject;
            }
            else
            {
                object collection = InitializeCollection();
                SetProperty(collection);
                collections.Push(new CollectionInfo()
                {
                    parentObject = parentObject,
                    node = currentNode,
                    collection = collection,
                    count = FetchCollectionCount()
                });
            }
            return skip;
        }

        //TODO refactor
        private void HandleNodeProperty()
        {
            object property;
            if (IsSimpleNode())
                property = FetchProperty();
            else
                property = InitializeProperty();
            if (IsCollectionNode())
                SetCollectionItem(property);
            else
                SetProperty(property);
            if (!IsSimpleNode())
                parentObject = property;
        }
        private bool IsCollectionNodeVisited()
        {
            return collections.FirstOrDefault(c => c.node == currentNode) != null;
        }

        private bool IsSimpleNode()
        {
            return currentNode["childs"] == null;
        }

        private bool IsCollectionNode()
        {
            return currentNode["collectionType"] != null;
        }

        private int FetchCollectionCount()
        {
            var list = currentNode["value"].ToString().Split('.').ToList();
            XDocument doc = XDocument.Parse(inputContent);
            XElement parentCollection = doc.Descendants(list[list.Count - 2])?.FirstOrDefault();
            if (parentCollection != null)
            {
                var childElemets = parentCollection.Elements(list[list.Count - 1]);
                return childElemets.Count();
            }
            return 0;
        }

        private string FetchProperty()
        {
            string path = currentNode["value"].ToString();
            XDocument doc = XDocument.Parse(inputContent);
            Stack<string> pathElements = new Stack<string>(path.Split('.').Reverse());
            XElement foundElement = null;
            int depth = 0;
            int maxDepth = pathElements.Count;
            while (depth < maxDepth)
            {
                string currentPathElement = pathElements.Pop();
                IEnumerable<XElement> children = doc.Descendants(currentPathElement);
                foundElement = children.FirstOrDefault();
                if (foundElement != null && pathElements.Count > 0)
                    doc = new XDocument(foundElement);
                depth++;
            }
            return foundElement?.Value;
        }

        private object InitializeCollection()
        {
            Type collectionType = FindType(currentNode["collectionType"].ToString());
            Type[] typeArgs = { FindType(currentNode["class"].ToString()) };
            return Activator.CreateInstance(collectionType.MakeGenericType(typeArgs));
        }

        private object InitializeProperty()
        {
            string className = currentNode["class"].ToString();
            return Activator.CreateInstance(FindType(className));
        }

        private Type FindType(string clazz)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Type type = assembly.GetTypes().FirstOrDefault(t => t.Name == clazz);
            return type != null ? type : Type.GetType(SupportedSystemType.Of(clazz));
        }

        private void SetProperty(object value)
        {
            string property = currentNode["property"].ToString();
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            propertyInfo.SetValue(parentObject, Convert.ChangeType(value, propertyInfo.PropertyType));
        }

        private void SetCollectionItem(object value)
        {
            string property = currentNode["property"].ToString();
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            object collection = propertyInfo.GetValue(parentObject, null);
            collection.GetType().GetMethod("Add").Invoke(collection, new[] { value });
        }
    }
}
