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

        private HashSet<JToken> visitedChilds = new HashSet<JToken>();

        private JToken? currentNode;

        private object? parentObject;

        private string? inputContent;

        // converts to Model given a configuration and input file
        public Model ToModel(String configFile, String inputContent)
        {
            Model model = new Model();
            JObject config = JObject.Parse(configFile);
            InitializeState(config, inputContent, model);
            TraverseNodes();
            return model;
        }

        // initializes converter's state
        private void InitializeState(JObject config, string inputContent, Model model)
        {
            this.inputContent = inputContent;

            // get childs list
            List<JToken> childsList = ((JArray)config["childs"]).ToList();
            childsList.Reverse();

            // initialize stacks
            childs = new Stack<JToken>(childsList);
            collections.Clear();
            visitedChilds.Clear();

            // initialize objects 
            currentNode = null;
            parentObject = model;
        }

        // traverses configuration nodes
        private void TraverseNodes()
        {
            while (childs.Count > 0)
            {
                currentNode = childs.Peek();
                if (IsCollectionNode())
                {
                    bool skip = HandleCollectionInfo();
                    if (skip)
                        continue;
                }
                HandleNode();
                if (!IsCollectionNode())
                    childs.Pop();
                PushChildNodes();
                visitedChilds.Add(currentNode);
            }
        }
      
        // pushes current node's childs into the childs stack
        private void PushChildNodes()
        {
            List<JToken> currentNodeChilds = currentNode["childs"] != null ? currentNode["childs"].ToList() : new List<JToken>();
            currentNodeChilds.Reverse();
            foreach (JToken child in currentNodeChilds)
                childs.Push(child);
        }

        // handles current node as a collection node.
        private bool HandleCollectionInfo()
        {
            bool skip = false;
            if (IsNodeVisited())
            {
                CollectionInfo collectionInfo = collections.Peek();
                collectionInfo.index++;
                if (collectionInfo.index >= collectionInfo.count)
                {
                    childs.Pop();
                    collections.Pop();
                    visitedChilds.Remove(currentNode);
                    skip = true;
                }
                else
                    parentObject = collectionInfo.parentObject;
            }
            else
            {
                object collection = CreateCollection();
                SetProperty(collection);
                collections.Push(new CollectionInfo()
                {
                    parentObject = parentObject,
                    collection = collection,
                    count = FetchCollectionCount()
                });
            }
            return skip;
        }

        // handles current node's property. Current node is either a simple or a complex node.
        // In case of simple node, the property is fetched from the input file, and set to the model.
        // In case of a complex node, the property is first initialized and then set to the model.
        //TODO refactor
        private void HandleNode()
        {
            object obj;
            if (IsSimpleNode())
                obj = FetchObject();
            else
                obj = CreateInstance();
            if (IsCollectionNode())
                SetCollectionItem(obj);
            else
                SetProperty(obj);
            if (!IsSimpleNode())
                parentObject = obj;
        }

        // decides if current node has been visited
        private bool IsNodeVisited()
        {
            return visitedChilds.FirstOrDefault(child => child == currentNode) != null;
        }

        // decides if current node is a simple node
        private bool IsSimpleNode()
        {
            return currentNode["childs"] == null;
        }

        // decides if current node is a collection node
        private bool IsCollectionNode()
        {
            return currentNode["collectionType"] != null;
        }

        // fetches the collection count corresponding to current node's value, by looking at the input file.
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

        // fetches the input object corresponding to current node's value, by looking at the input file
        private string FetchObject()
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

        // creates an instance of current node's collectionType
        private object CreateCollection()
        {
            Type collectionType = FindType(currentNode["collectionType"].ToString());
            Type[] typeArgs = { FindType(currentNode["class"].ToString()) };
            return Activator.CreateInstance(collectionType.MakeGenericType(typeArgs));
        }

        // creates an instance of current node's class
        private object CreateInstance()
        {
            string className = currentNode["class"].ToString();
            return Activator.CreateInstance(FindType(className));
        }

        // sets the model property corresponding to current node, with given value
        private void SetProperty(object value)
        {
            string property = currentNode["property"].ToString();
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            propertyInfo.SetValue(parentObject, Convert.ChangeType(value, propertyInfo.PropertyType));
        }

        // sets the model collection item corresponding to current node, with given value
        private void SetCollectionItem(object value)
        {
            string property = currentNode["property"].ToString();
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            object collection = propertyInfo.GetValue(parentObject, null);
            collection.GetType().GetMethod("Add").Invoke(collection, new[] { value });
        }

        // tries to find a type by given class name
        private Type FindType(string clazz)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Type type = assembly.GetTypes().FirstOrDefault(t => t.Name == clazz);
            return type != null ? type : Type.GetType(SupportedSystemType.FullyQualifiedNameOf(clazz));
        }
    }
}
