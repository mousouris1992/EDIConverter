using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Formatting = Newtonsoft.Json.Formatting;

namespace EDIConverter.parser
{
    public class JSONParser : FileParser
    {
        private JObject? Json;

        private Dictionary<JToken, int> Indexes = new Dictionary<JToken, int>();

        private Dictionary<string, int> Indexes2 = new Dictionary<string, int>();

        public void Parse(string content)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            Json = JObject.Parse(JsonConvert.SerializeXmlNode(doc));
        }

        public int FetchCollectionCount(string property)
        {
            List<string> paths = new List<string>(property.Split("."));
            JToken token = Json;
            int i = 0;
            while (i < paths.Count)
            {
                if (token.Type == JTokenType.Array)
                {
                    int idx = 0;
                    Indexes.TryGetValue(token, out idx);
                    token = ((JArray)token)[idx];
                    continue;
                }
                else
                    token = token[paths[i]];
                i++;
            }
            return ((JArray)token).ToList().Count;
        }

        public string FetchValue(string property)
        {
            List<string> paths = new List<string>(property.Split("."));
            JToken token = Json;
            int i = 0;
            Boolean isCollection = false;
            while (i < paths.Count)
            {
                if (token.Type == JTokenType.Array)
                {
                    int idx = 0;
                    Indexes.TryGetValue(token, out idx);
                    token = ((JArray)token)[idx];
                    continue;
                }
                else
                    token = token[paths[i]];
                i++;
            }
            if (token.Type == JTokenType.Array)
            {
                int idx = 0;
                Indexes.TryGetValue(token, out idx);
                return ((JArray)token)[idx].ToString();
            }
            return token.ToString();           
        }

        public bool HasProperty(string property)
        {
            List<string> paths = new List<string>(property.Split("."));
            JToken token = Json;
            int i = 0;
            while (i < paths.Count - 1)
            {
                if (token.Type == JTokenType.Array)
                {
                    int idx = 0;
                    Indexes.TryGetValue(token, out idx);
                    token = ((JArray)token)[idx];
                    continue;
                }
                else
                    token = token[paths[i]];
                i++;
            }
            if (token.Type == JTokenType.Array)
            {
                int idx = 0;
                Indexes.TryGetValue(token, out idx);
                JArray array = (JArray)token;
                return array.Count > idx;
            }
            return token[paths.Last()] != null;
        }

        public void SetIndex(string property, int index = 0)
        {
            List<string> paths = new List<string>(property.Split("."));
            JToken token = Json;
            int i = 0;
            while(i < paths.Count)
            {
                if (token.Type == JTokenType.Array)
                {
                    int idx = 0;
                    Indexes.TryGetValue(token, out idx);
                    token = ((JArray)token)[idx];
                    continue;
                }
                else
                    token = token[paths[i]];
                i++;
            }
            if (token.Type == JTokenType.Array)
            {
                Indexes[token] = index;
                Indexes2[property] = index;
            }
        }
    }
}
