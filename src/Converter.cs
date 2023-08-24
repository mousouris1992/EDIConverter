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
        public Model ToModel(JObject config, String inputContent)
        {
            Model model = new Model();

            // fetch childs list
            List<JToken> childsList = ((JArray)config["childs"]).ToList();
            childsList.Reverse();

            // initialize childs
            Stack<JToken> childs = new Stack<JToken>(childsList);
            Stack<CollectionInfo> collections = new Stack<CollectionInfo>();

            // current node 
            JToken currentNode = null;

            // stored objects
            object parentObject = model;

            // traverse childs
            while (childs.Count > 0)
            {
                // get current node
                currentNode = childs.Peek();

                // check if current node is collection type
                if (IsCollection(currentNode))
                {
                    // already visited node
                    if (IsCollectionVisited(collections, currentNode))
                    {
                        // update index
                        CollectionInfo collectionInfo = collections.Peek();
                        collectionInfo.index++;

                        // check if collection is done
                        if (collectionInfo.index >= collectionInfo.count)
                        {
                            childs.Pop();
                            collections.Pop();
                            continue;
                        }
                        // reset parent object, we will revisit collection childs
                        else
                        {
                            parentObject = collectionInfo.parentObject;
                        }
                    }
                    // collection first time visit
                    else
                    {
                        // push new collection to collections stack
                        collections.Push(new CollectionInfo()
                        {
                            parentObject = parentObject,
                            node = currentNode,
                            collection = InitializeProperty(parentObject, currentNode["property"].ToString()),
                            count = FetchCollectionCount(currentNode, inputContent)
                        });
                    }
                }

                // simple node
                if (IsSimpleType(currentNode))
                {
                    // get the mapped value from input
                    string value = GetPropertyValue(currentNode["value"].ToString(), inputContent);
                    // set the model property
                    if (IsCollection(currentNode))
                    {
                        SetListPropertyValue(parentObject, currentNode["property"].ToString(), value);
                    }
                    else
                    {
                        SetPropertyValue(parentObject, currentNode["property"].ToString(), value);
                    }
                }
                // complex node
                else
                {
                    // initialize the property, and set it as the parent object
                    // TODO: merge both logics into one, maybe need to set class for all properties in config
                    if (IsCollection(currentNode))
                    {
                        parentObject = InitializeListProperty(parentObject, currentNode["property"].ToString(), currentNode["class"].ToString());
                    }
                    else
                    {
                        parentObject = InitializeProperty(parentObject, currentNode["property"].ToString());
                    }
                }

                // pop current node
                if (!IsCollection(currentNode))
                {
                    childs.Pop();
                }

                // push child nodes
                List<JToken> currentNodeChilds = currentNode["childs"] != null ? currentNode["childs"].ToList() : new List<JToken>();
                currentNodeChilds.Reverse();
                foreach (JToken child in currentNodeChilds)
                {
                    childs.Push(child);
                }
            }
            return model;
        }

        private bool IsCollectionVisited(Stack<CollectionInfo> collections, JToken node)
        {
            return collections.FirstOrDefault(c => c.node == node) != null;
        }

        private bool IsSimpleType(JToken node)
        {
            return node["childs"] == null;
        }

        private bool IsCollection(JToken node)
        {
            return node["collectionType"] == null;
        }

        private int FetchCollectionCount(JToken node, string xmlInput)
        {
            var list = node["value"].ToString().Split('.').ToList();
            XDocument doc = XDocument.Parse(xmlInput);
            XElement parentCollection = doc.Descendants(list[list.Count - 2])?.FirstOrDefault();
            if (parentCollection != null)
            {
                var childElemets = parentCollection.Elements(list[list.Count - 1]);
                return childElemets.Count();
            }
            else
            {
                return 0;
            }


        }
        private string GetPropertyValue(string path, string content)
        {
            XDocument doc = XDocument.Parse(content);
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
                {
                    doc = new XDocument(foundElement);
                }

                depth++;
            }
            return foundElement.Value;
        }

        private void SetPropertyValue(object parentObject, string property, object value)
        {
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            propertyInfo.SetValue(parentObject, Convert.ChangeType(value, propertyInfo.PropertyType));
        }

        private void SetListPropertyValue(object parentObject, string property, object value)
        {
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            object collection = propertyInfo.GetValue(parentObject, null);
            collection.GetType().GetMethod("Add").Invoke(collection, new[] { value });
        }

        private object InitializeProperty(object parentObject, string property)
        {
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            var propertyValue = propertyInfo.GetValue(parentObject);
            if (propertyValue == null)
            {
                propertyValue = Activator.CreateInstance(propertyInfo.PropertyType);
                propertyInfo.SetValue(parentObject, propertyValue);
            }
            return propertyValue;
        }

        private object InitializeListProperty(object parentObject, string property, string className)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var type = assembly.GetTypes().First(t => t.Name == className);
            object instance = Activator.CreateInstance(type);
            SetListPropertyValue(parentObject, property, instance);
            return instance;
        }
    }
}
