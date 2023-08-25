using EDIConverter.src;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EDIConverter
{
    // XML implementation of FileParser
    public class XMLParser : FileParser
    {
        private XDocument? doc;

        public void Parse(string content)
        {
            this.doc = XDocument.Parse(content);
        }

        public string FetchValue(string property)
        {
            ValidateParsedDocument();
            Stack<string> pathElements = new Stack<string>(property.Split('.').Reverse());
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
        private void ValidateParsedDocument()
        {
            if (doc == null)
                throw new NullReferenceException("parse the document first!");
        }
    }
}
