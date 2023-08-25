using EDIConverter.src;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter
{
    class FileParserFactory
    {
        private const string XML = "xml";
        private FileParserFactory() { } 
        public static FileParser Create(string fileType)
        {
            switch (fileType)
            {
                case XML:
                    return new XMLParser();
            }
            throw new ArgumentException(string.Format("could not map to a FileParser for fileType {0}", fileType));
        }
    }
}
