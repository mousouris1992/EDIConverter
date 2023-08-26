using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.parser
{
    /// <summary>
    /// Factory pattern for FileParser implementations
    /// </summary>
    class FileParserFactory
    {
        private const string XML = "xml";
        private FileParserFactory() { }

        /// <summary>
        /// Creates an instance of FileParser based on given fileType
        /// </summary>
        /// <param name="fileType"></param>
        /// <returns>An instance of FileParser</returns>
        /// <exception cref="ArgumentException"></exception>
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
