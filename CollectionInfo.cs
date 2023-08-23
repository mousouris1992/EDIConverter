using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter
{
    internal class CollectionInfo
    {
        public required JToken mapping { get; set; }
        public required object collection { get; set; }
        public object? collectionItem { get; set; }
        public int index { get; set; } = 0;
        public int count { get; set; } = 0;
    }
}
