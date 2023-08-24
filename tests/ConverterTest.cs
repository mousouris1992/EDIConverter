using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace EDIConverter
{
    [TestClass]
    public class ConverterTest
    {
        string xmlInput = File.ReadAllText(@"../../../resources/input.xml");
        string configFile = File.ReadAllText(@"../../../resources/config.json");

        [TestMethod]
        public void ToModel()
        {
            // given
            JObject config = JObject.Parse(configFile);
            Converter converter = new Converter();

            // when
            Model model = converter.ToModel(config, xmlInput);

            // then
            Assert.IsNotNull(model);
        }
    }
}