using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace EDIConverter.parser
{
    /// <summary>
    ///  JSON implementation of FileParser
    /// </summary>
    //TODO: Implement this
    public class JSONParser : FileParser
    {
        private JObject? Json;

        public void Parse(string content)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            Json = JObject.Parse(JsonConvert.SerializeXmlNode(doc));
            //Json = JObject.Parse(content);
        }

        public int FetchCollectionCount(string property)
        {
            List<string> paths = new List<string>(property.Split("."));
            JToken token = Json;
            for (int i = 0; i < paths.Count; i++)
            {
                (string path, int idx) = Resolve(paths[i]);
                if (idx != -1)
                    token = token[path];
            }
            return ((JArray)token).ToList().Count;
        }

        public string FetchValue(string property, int index = 0)
        {
            List<string> paths = new List<string>(property.Split("."));
            JToken token = Json;
            for (int i = 0; i < paths.Count; i++)
            {
                (string path, int idx) = Resolve(paths[i]);
                if (idx == -1)
                    token = token[path];
                else
                    token = ((JArray)token[path]).ToList()[idx];
            }
            return token.ToString();
        }

        public object GetContext()
        {
            return null;
        }

        public bool HasProperty(string property)
        {
            List<string> paths = new List<string>(property.Split("."));
            JToken token = Json;
            for (int i = 0; i < paths.Count; i++)
            {
                (string path, int idx) = Resolve(paths[i]);
                try
                {
                    if (idx == -1)
                        token = token[path];
                    else
                        token = ((JArray)token[path]).ToList()[idx];
                }
                catch(Exception e)
                {
                    return false;
                }
                
            }
            return true;
        }

        public void SetContext(string property, int index = 0)
        {
        }

        public void SetContext(object obj)
        {
        }

        private (string, int) Resolve(string path)
        {
            int start;
            if ((start = path.IndexOf("[")) == -1)
                return (path, -1);
            int end = path.IndexOf("]");
            string cleanProperty;
            if ((start + 1) - (end - 1) == 0)
                return (path.Substring(0, start), Int32.Parse(path.ToCharArray()[start + 1].ToString()));
            int index = Int32.Parse(path.Substring(start + 1, end - 1));
            cleanProperty = path.Substring(0, start - 1);
            return (cleanProperty, index);
        }
    }
}
