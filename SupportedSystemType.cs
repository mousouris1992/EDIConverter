using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter
{
    internal class SupportedSystemType
    {
        private static readonly Tuple<string, string> STRING = Tuple.Create("String", "System.String");
        private static readonly Tuple<string, string> LIST = Tuple.Create("List", "System.Collections.Generic.List`1");

        private static Dictionary<string, string> Map = InitMap();

        private static Dictionary<string, string> InitMap()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d.Add(STRING.Item1, STRING.Item2);
            d.Add(LIST.Item1, LIST.Item2);
            return d;
        }

        public static string Of(string type)
        {
            string name = Map[type];
            if(name == null)
                throw new ArgumentException(string.Format("could not map type: {0}", type));
            return name;
        }
    }
}
