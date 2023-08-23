using Newtonsoft.Json.Linq;
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
            string xmlInput = "";//ReadAllText("input.txt"); //"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<Order>\r\n\t<Header>\r\n\t\t<ReferenceNumber>4500251763</ReferenceNumber>\r\n\t\t<IssueDate>20230724</IssueDate>\r\n\t\t<DocumentType>Order</DocumentType>\r\n\t\t<Supplier>\r\n\t\t\t<Addresses>\r\n\t\t\t\t<Address>\r\n\t\t\t\t\t<AddressLine>Priamou 134</AddressLine>\r\n\t\t\t\t\t<PostalCode>13122</PostalCode>\r\n\t\t\t\t</Address>\r\n\t\t\t\t<Address>\r\n\t\t\t\t\t<AddressLine>Axilleos 80</AddressLine>\r\n\t\t\t\t\t<PostalCode>13123</PostalCode>\r\n\t\t\t\t</Address>\r\n\t\t\t</Addresses>\r\n\t\t\t<Name>Thodoris</Name>\r\n\t\t\t<Vat>123456789</Vat>\r\n\t\t\t<SupplierCode>0000021879</SupplierCode>\r\n\t\t</Supplier>\r\n\t</Header>\r\n</Order>\r\n\r\n";
            string configFile = "";//ReadAllText("config.txt"); //"{\r\n  \"fileType\": \"xml\",\r\n  \"mappings\": [\r\n    {\r\n      \"type\": \"String\",\r\n      \"property\": \"ReferenceNumber\",\r\n      \"value\": \"Order.Header.ReferenceNumber\"\r\n    },\r\n    {\r\n      \"type\": \"Supplier\",\r\n      \"property\": \"Supplier\",\r\n      \"mappings\": [\r\n        {\r\n          \"type\": \"String\",\r\n          \"property\": \"Name\",\r\n          \"value\": \"Order.Header.Supplier.Name\"\r\n        },\r\n        {\r\n          \"type\": \"String\",\r\n          \"property\": \"Vat\",\r\n          \"value\": \"Order.Header.Supplier.Vat\"\r\n        },\r\n        {\r\n          \"type\": \"Address\",\r\n          \"property\": \"Address\",\r\n          \"mappings\": [\r\n            {\r\n              \"type\": \"String\",\r\n              \"property\": \"AddressLine\",\r\n              \"value\": \"Order.Header.Supplier.Addresses.Address.AddressLine\"\r\n            },\r\n            {\r\n              \"type\": \"String\",\r\n              \"property\": \"PostalCode\",\r\n              \"value\": \"Order.Header.Supplier.Addresses.Address.PostalCode\"\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    }\r\n  ]\r\n}";

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
            Stack<JToken> collectionMappings = new Stack<JToken>();
            Dictionary<JToken, int> collectionCount = new Dictionary<JToken, int>();
            Dictionary<JToken, int> collectionIndex = new Dictionary<JToken, int>();

            // set first child as current
            JToken currentMapping = null;

            // stored objects
            object parentObject = model;
            object collectionObject = null;

            // traverse the mappings
            while (mappings.Count > 0)
            {
                // get current mapping
                currentMapping = mappings.Peek();

                // check if current mapping is collection type
                if (IsCollectionTypeMapping(currentMapping))
                {
                    // already visited mapping
                    if (collectionMappings.Contains(currentMapping))
                    {
                        // add created collection object
                        //addObjectToCollection(parentObject, collectionObject)
                        // update index and check
                        collectionIndex[currentMapping]++;
                        // revisit childs if index < count
                    }
                    // first visit to mapping
                    else
                    {
                        // push current mapping to collection mappings
                        collectionMappings.Push(currentMapping);
                        // get collection count
                        collectionCount[currentMapping] = FetchCollectionCount(currentMapping, xmlInput);
                        collectionIndex[currentMapping] = 0;
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
                    SetPropertyValue(parentObject, currentMapping["property"].ToString(), value);
                }
                // complex mapping
                else
                {
                    // initialize the property, and set it as the parent object
                    parentObject = InitializeProperty(parentObject, currentMapping["property"].ToString());

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

        static Boolean IsSimpleMapping(JToken mapping)
        {
            return mapping["value"] != null;
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

        static int FetchCollectionCount(JToken mapping, object xmlInput)
        {
            return 0;
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

    }
}
