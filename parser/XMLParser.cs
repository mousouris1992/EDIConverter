using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EDIConverter.parser
{
    /// <summary>
    ///  XML implmentation of FileParser
    ///  TODO: consider converting every input file to JSON,
    ///  and implement only a JSON parser if possible.
    /// </summary>
    public class XMLParser : FileParser
    {
        private XDocument? XDoc;

        private XDocument? CurrentElement;

        public void Parse(string content)
        {
            XDoc = XDocument.Parse(content);
            CurrentElement = new XDocument(XDoc);
        }

        public string FetchValue(string property, int index)
        {
            ValidateParsedDocument();
            return FetchElement(property, index)?.Value;
        }
        
        public bool HasProperty(string property)
        {
            ValidateParsedDocument();
            return FetchElement(property) != null;
        }

        public int FetchCollectionCount(string property)
        {
            ValidateParsedDocument();
            int i = 0;
            while(true)
            {
                if (FetchElement(property, i) != null)
                    i++;
                else
                    break;
            }
            return i;
        }
        public void SetContext(string property, int index = 0)
        {
            if (property == null)
                CurrentElement = new XDocument(XDoc);
            else
                CurrentElement = new XDocument(FetchElement(property, index));
        }

        public void SetContext(object property)
        {
            if(property == null)
                CurrentElement = new XDocument(XDoc);
            else
                CurrentElement = new XDocument((XDocument)property);
        }

        public object GetContext()
        {
            return CurrentElement;
        }

        private XElement FetchElement(string property, int index = 0)
        {
            List<string> pathElementsList = property.Split('.').ToList();
            string propertyName = pathElementsList.Last();
            pathElementsList.Reverse();
            Stack<string> pathElements = new Stack<string>(pathElementsList);
            XElement foundElement = null;
            int depth = 0;
            int maxDepth = pathElements.Count;
            XDocument tempDoc = CurrentElement;
            while (depth < maxDepth)
            {
                string currentPathElement = pathElements.Pop();
                IEnumerable<XElement> children = tempDoc.Descendants(currentPathElement);
                foundElement = children.FirstOrDefault();
                if (depth + 1 == maxDepth)
                {
                    int i = 0;
                    foreach (XElement child in children)
                    {
                        if (child.Name == propertyName)
                        {
                            if (i == index)
                                return child ;
                            i++;
                        }
                    }
                    if(i >= index)
                        foundElement = null;
                }

                if (foundElement != null && pathElements.Count > 0)
                    tempDoc = new XDocument(foundElement);
                depth++;
            }
            return foundElement;
        }

        private void ValidateParsedDocument()
        {
            if (XDoc == null)
                throw new NullReferenceException("parse the document first!");
        }
    }
}
