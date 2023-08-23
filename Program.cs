using Newtonsoft.Json.Linq;
using System.Data;
using System.Reflection;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Boolean = System.Boolean;

namespace EDIConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string directoryPath = "D:\\Giwrgos\\programming\\c#\\EDIConverter\\resources\\";
            string xmlFileName = "input.xml";
            string jsonFileName = "config.json"; 


            string xmlInput = ReadFile(Path.Combine(directoryPath,xmlFileName));
            string configFile = ReadFile(Path.Combine(directoryPath, jsonFileName));

            if (xmlInput == null || configFile == null)
            {
                Console.WriteLine("cannot read this file");
            }


            // parse configuration file
            JObject config = JObject.Parse(configFile);

            // convert to model
            Model model = convertToModel(config, xmlInput);
        }

        static Model convertToModel(JObject config, string xmlInput)
        {
            Model model = new Model();

            // fetch mappings list
            List<JToken> mappingsList = ((JArray) config["mappings"]).ToList();
            mappingsList.Reverse();

            // initialize mappings stack
            Stack<JToken> mappings = new Stack<JToken>(mappingsList);
            Stack<CollectionInfo> collectionMappings = new Stack<CollectionInfo>();

            // set first child as current
            JToken currentMapping = null;

            // stored objects
            object parentObject = model;

            // traverse the mappings
            while (mappings.Count > 0)
            {
                // get current mapping
                currentMapping = mappings.Peek();

                // check if current mapping is collection type
                if (IsCollectionTypeMapping(currentMapping))
                {
                    // already visited mapping
                    if (IsCollectionAlreadyVisited(collectionMappings, currentMapping))
                    {
                        // update index and check
                        CollectionInfo collectionInfo = collectionMappings.Peek();
                        collectionInfo.index++;
                        if (collectionInfo.index >= collectionInfo.count)
                        {
                            mappings.Pop();
                            collectionMappings.Pop();
                            continue;
                        }
                        else
                        {
                            parentObject = collectionInfo.parentObject;
                        }
                    }
                    // first visit to mapping
                    else
                    {
                        CollectionInfo collectionInfo = new CollectionInfo()
                        {
                            parentObject = parentObject,
                            mapping = currentMapping,
                            collection = InitializeProperty(parentObject, currentMapping["collectionProperty"].ToString()),
                            count = FetchCollectionCount(currentMapping, xmlInput)
                        };

                        // push current mapping to collection mappings
                        collectionMappings.Push(collectionInfo);
                    }
                }
                else
                {
                    // pop current mapping
                    mappings.Pop();
                }

                // simple mapping
                if (IsSimpleMapping(currentMapping))
                {
                    // get the mapped value from input
                    string value = GetPropertyValue(currentMapping["value"].ToString(), xmlInput);
                    // set the model property
                    if (IsCollectionTypeMapping(currentMapping))
                    {
                        SetListPropertyValue(parentObject, currentMapping["collectionProperty"].ToString(), value);
                    }
                    else
                    {
                        SetPropertyValue(parentObject, currentMapping["property"].ToString(), value);
                    }
                }
                // complex mapping
                else
                {
                    // initialize the property, and set it as the parent object
                    if (IsCollectionTypeMapping(currentMapping))
                    {
                        parentObject = InitializeListProperty(parentObject, currentMapping["collectionProperty"].ToString(), currentMapping["class"].ToString());
                    }
                    else
                    {
                        parentObject = InitializeProperty(parentObject, currentMapping["property"].ToString());
                    }

                    // include child mappings
                    List<JToken> childMappings = currentMapping["mappings"].ToList();
                    childMappings.Reverse();
                    foreach (JToken childMapping in childMappings)
                    {
                        mappings.Push(childMapping);
                    }
                }
            }
            return model;
        }

        private static bool IsCollectionAlreadyVisited(Stack<CollectionInfo> collectionMappings, JToken currentMapping)
        {
            return collectionMappings.FirstOrDefault(c => c.mapping == currentMapping) != null;
        }

        static Boolean IsSimpleMapping(JToken mapping)
        {
            return mapping["mappings"] == null;
        }

        static Boolean IsCollectionTypeMapping(JToken mapping)
        {
            JToken collectionType = mapping["collectionType"];
            if (collectionType == null)
            {
                return false;
            }
            return collectionType.ToString() == "List";
        }

        static int FetchCollectionCount(JToken mapping, string xmlInput)
        {
            var path = mapping["value"].ToString();
            var list = path.Split('.').ToList();

            XDocument doc = XDocument.Parse(xmlInput);
            XElement parentCollection = doc.Descendants(list[list.Count-2])?.FirstOrDefault();

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

        static string GetPropertyValue(string path, string content)
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

            return foundElement?.Value;
        }

        static void SetPropertyValue(object parentObject, string property, object value)
        {
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(property);
            propertyInfo.SetValue(parentObject, Convert.ChangeType(value, propertyInfo.PropertyType));
        }
        static void SetListPropertyValue(object parentObject, string collectionProperty, object value)
        {
            PropertyInfo propertyInfo = parentObject.GetType().GetProperty(collectionProperty);
            object collection = propertyInfo.GetValue(parentObject, null);
            collection.GetType().GetMethod("Add").Invoke(collection, new[] { value });
        }

        static object InitializeProperty(object parentObject, string property)
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

        static object InitializeListProperty(object parentObject, string collectionProperty, string className)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var type = assembly.GetTypes()
                .First(t => t.Name == className);
            object instance = Activator.CreateInstance(type);
            SetListPropertyValue(parentObject, collectionProperty, instance);
            return instance;
        }

        private static object CreateInstance(string className)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var type = assembly.GetTypes()
                .First(t => t.Name == className);
            return Activator.CreateInstance(type);
        }

        static string ReadFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file{ex.Message}");
                return null;
            }
        
        }

    }
}
