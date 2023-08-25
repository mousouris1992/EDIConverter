using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
            traverseNodes();
            return model;
        }

        private void traverseNodes()
        {
            while (childs.Count > 0)
            {
                // peek current node
                currentNode = childs.Peek();
                // handle collection node
                if (IsCollectionNode())
                {
                    bool skip = HandleCollectionNode();
                    if (skip)
                        continue;
                }
                SetNodeProperty();
                // pop current node
                if (!IsCollectionNode())
                    childs.Pop();
                // push child nodes
                PushChildNodes();
            }
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
                // push new collection to collections stack
                collections.Push(new CollectionInfo()
                {
                    parentObject = parentObject,
                    node = currentNode,
                    collection = InitializeProperty(),
                    count = FetchCollectionCount()
                });
            }
            return skip;
        }

        private void SetNodeProperty()
        {
            if (IsSimpleNode())
                SetSimpleNodeProperty();
            else
                SetComplexNodeProperty();
        }

        private void SetSimpleNodeProperty()
        {
            string value = GetPropertyValue();
            if (IsCollectionNode())
                SetListPropertyValue(value);
            else
                SetPropertyValue(value);
        }

        private void SetComplexNodeProperty()
        {
            // initialize the property, and set it as the parent object
            // TODO: merge both logics into one, maybe need to set class for all properties in config
            if (IsCollectionNode())
                parentObject = InitializeListProperty();
            else
                parentObject = InitializeProperty();
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

        private string GetPropertyValue()
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

        private void SetPropertyValue(object value)
        {
            string property = currentNode["property"].ToString();
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            propertyInfo.SetValue(parentObject, Convert.ChangeType(value, propertyInfo.PropertyType));
        }

        private void SetListPropertyValue(object value)
        {
            string property = currentNode["property"].ToString();
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            object collection = propertyInfo.GetValue(parentObject, null);
            collection.GetType().GetMethod("Add").Invoke(collection, new[] { value });
        }

        private object InitializeProperty()
        {
            string property = currentNode["property"].ToString();
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            var propertyValue = propertyInfo.GetValue(parentObject);
            if (propertyValue == null)
            {
                propertyValue = Activator.CreateInstance(propertyInfo.PropertyType);
                propertyInfo.SetValue(parentObject, propertyValue);
            }
            return propertyValue;
        }

        private object InitializeListProperty()
        {
            string property = currentNode["property"].ToString();
            string className = currentNode["class"].ToString();
            var assembly = Assembly.GetExecutingAssembly();
            var type = assembly.GetTypes().First(t => t.Name == className);
            object instance = Activator.CreateInstance(type);
            SetListPropertyValue(instance);
            return instance;
        }
    }
}
