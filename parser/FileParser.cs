using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.parser
{
    /* 
     * Defines basic operations over an input file
    */ 
    public interface FileParser
    {
        /*
        * Parses given content
        */
        void Parse(string content);

        /*
         *
         */
        string FetchValue(string property);

        int FetchCollectionCount(string property);

        bool HasProperty(string property);

        void SetIndex(string property, int index);
    }
}
