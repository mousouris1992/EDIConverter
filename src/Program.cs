using Newtonsoft.Json.Linq;
using System.Data;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Boolean = System.Boolean;

namespace EDIConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string resourcesPath = "D:\\Giwrgos\\programming\\c#\\EDIConverter\\resources\\";
            string xmlInput = File.ReadAllText(Path.Combine(resourcesPath, "input.xml"));
            string configFile = File.ReadAllText(Path.Combine(resourcesPath, "config.json"));

            // parse configuration file
            JObject config = JObject.Parse(configFile);

            // convert to model
            Model model = new Converter().ToModel(config, xmlInput);
        }

    }
}
