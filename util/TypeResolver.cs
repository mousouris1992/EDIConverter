using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.util
{

    public class TypeResolver
    {
        class SupportedSystemType
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

            // returns the fully qualified name of given type
            public static string FullyQualifiedNameOf(string type)
            {
                string name = Map[type];
                if (name == null)
                    throw new ArgumentException(string.Format("could not map type: {0}", type));
                return name;
            }
        }

        // tries to resolve to a Type given a class name
        public static Type resolve(string className)
        {
            var assembly = Assembly.GetExecutingAssembly();
            Type type = assembly.GetTypes().FirstOrDefault(t => t.Name == className);
            return type != null ? type : Type.GetType(SupportedSystemType.FullyQualifiedNameOf(className));
        }
    }
}
