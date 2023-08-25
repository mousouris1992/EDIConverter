using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EDIConverter.parser
{
    // XML implementation of FileParser
    public class XMLParser : FileParser
    {
        private XDocument? doc;

        public void Parse(string content)
        {
            doc = XDocument.Parse(content);
        }

        public string FetchValue(string property)
        {
            ValidateParsedDocument();
            return FetchElement(property)?.Value;
        }

        public string FetchValue(string property, int index)
        {
            ValidateParsedDocument();
            throw new NotImplementedException();
        }

        public int FetchCollectionCount(string property)
        {
            ValidateParsedDocument();
            var list = property.Split('.').ToList();
            XElement parentCollection = doc.Descendants(list[list.Count - 2])?.FirstOrDefault();
            if (parentCollection != null)
            {
                var childElemets = parentCollection.Elements(list[list.Count - 1]);
                return childElemets.Count();
            }
            return 0;
        }

        public bool HasProperty(string property)
        {
            ValidateParsedDocument();
            return FetchElement(property) != null;
        }

        private XElement FetchElement(string property)
        {
            Stack<string> pathElements = new Stack<string>(property.Split('.').Reverse());
            XElement foundElement = null;
            int depth = 0;
            int maxDepth = pathElements.Count;
            XDocument xdoc = doc;
            while (depth < maxDepth)
            {
                string currentPathElement = pathElements.Pop();
                IEnumerable<XElement> children = xdoc.Descendants(currentPathElement);
                foundElement = children.FirstOrDefault();
                if (foundElement != null && pathElements.Count > 0)
                    xdoc = new XDocument(foundElement);
                depth++;
            }
            return foundElement;
        }

        private void ValidateParsedDocument()
        {
            if (doc == null)
                throw new NullReferenceException("parse the document first!");
        }
    }
}
