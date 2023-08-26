using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.parser
{
    /// <summary>
    /// Defines basic operations over an input file
    /// </summary>
    public interface FileParser
    {
        /// <summary>
        /// Parses given content
        /// </summary>
        /// <param name="content"></param>
        void Parse(string content);

        /// <summary>
        /// Fetches the value of given property
        /// </summary>
        /// <param name="property"></param>
        /// <param name="index"></param>
        /// <returns>the property value</returns>
        string FetchValue(string property, int index = 0);

        /// <summary>
        /// Fetches collection's count
        /// </summary>
        /// <param name="property"></param>
        /// <returns>the collection count</returns>
        int FetchCollectionCount(string property);

        /// <summary>
        /// Decides if given property exists
        /// </summary>
        /// <param name="property"></param>
        /// <returns>true if property exists</returns>
        bool HasProperty(string property);

        /// <summary>
        /// Sets context
        /// </summary>
        /// <param name="property"></param>
        /// <param name="index"></param>
        void SetContext(string property, int index = 0);

        /// <summary>
        /// Sets context
        /// </summary>
        /// <param name="obj"></param>
        void SetContext(object obj);

        /// <summary>
        /// Returns context
        /// </summary>
        /// <returns>context</returns>
        object GetContext();
    }
}
