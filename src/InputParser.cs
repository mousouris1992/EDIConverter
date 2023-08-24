using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.src
{
    public interface InputParser
    {
        object getProperty(string property);
        object getProperty(string property, int index);
    }
}
