using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.parser
{
    /// <summary>
    ///  JSON implementation of FileParser
    /// </summary>
    public class JSONParser : FileParser
    {
        public int FetchCollectionCount(string property)
        {
            throw new NotImplementedException();
        }

        public string FetchValue(string property, int index = 0)
        {
            throw new NotImplementedException();
        }

        public object GetContext()
        {
            throw new NotImplementedException();
        }

        public bool HasProperty(string property)
        {
            throw new NotImplementedException();
        }

        public void Parse(string content)
        {
            throw new NotImplementedException();
        }

        public void SetContext(string property, int index = 0)
        {
            throw new NotImplementedException();
        }

        public void SetContext(object obj)
        {
            throw new NotImplementedException();
        }
    }
}
