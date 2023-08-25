using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.converter
{
    // configuration node, holds reference to its parent and child nodes
    class ConfigNode
    {
        private ConfigNode parent { get; set; }
        private List<ConfigNode> childs { get; set; }
        private string className { get; set; }
        private string property { get; set; }
        private string value { get; set; }
        private string collectionType { get; set; }
        private string collection { get; set; }
    }
}
