using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.src
{
    // defines basic operations over an input file.
    public interface FileParser
    {
        // parses given content
        void Parse(string content);

        // fetches the value of given property
        string FetchValue(string property);

        // fetches the value of given property indexed
        string FetchValue(string property, int index);

        // fetches collection's count
        int FetchCollectionCount(string property);
    }
}
