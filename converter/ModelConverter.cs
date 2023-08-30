using EDIConverter.model;
using EDIConverter.parser;
using EDIConverter.tree;
using EDIConverter.util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.converter
{
    public class ModelConverter : TreeDFSTraverser<ConfigNode>
    {
        private JObject Config;
        private Model? Model;
        private object? ModelContext;
        private FileParser? Parser;

        public Model ToModel(string configuration, string input)
        {
            Initialize(configuration, input);
            Traverse(GetRootChilds());
            return Model;
        }

        private void Initialize(string configuration, string input)
        {
            Model = new Model();
            ModelContext = Model;
            Config = JObject.Parse(configuration);
            Parser = FileParserFactory.Create(Config["fileType"].ToString());
            Parser.Parse(input);
        }

        private List<ConfigNode> GetRootChilds()
        {
            List<JToken> childsList = ((JArray)Config["childs"]).ToList();
            childsList.Reverse();
            List<ConfigNode> childs = new List<ConfigNode>();
            foreach (JToken child in childsList)
                childs.Add(new ConfigNode(child, null, Model));
            return childs;
        }

        protected override bool Skip()
        {
            bool skip = false;
            if (!Parser.HasProperty(Current.Value))
                skip = true;
            if (Current.IsCollection() && !Current.CanVisit())
            {
                skip = true;
                Current.ResetIndex();
            }
            return skip;
        }

        protected override void HandleNode()
        {
            if (Current.IsCollection())
                HandleNodeCollection();
            HandleNodeProperty();
        }

        private void HandleNodeCollection()
        {
            if (!Current.IsVisited())
            {
                Current.SetCount(Parser.FetchCollectionCount(Current.Value));
                ReflectionHandler.SetCollectionProperty(Current.ModelContext, Current.GetCollection(), Current.GetCollectionType(), Current.ClassName);
            }
            Parser.SetIndex(Current.Value, Current.GetIndex());
        }

        private void HandleNodeProperty()
        {
            object obj;
            if (Current.IsFinal())
                obj = Parser.FetchValue(Current.Value);
            else
                obj = ReflectionHandler.CreateInstance(Current.ClassName);
            if (Current.IsCollection())
                ReflectionHandler.AddCollectionItem(Current.ModelContext, Current.GetCollection(), obj);
            else
                ReflectionHandler.SetProperty(Current.ModelContext, Current.Property, obj);
            ModelContext = obj;
        }

        protected override void EndVisit()
        {
            Current.Visit();
        }

        protected override bool ShouldPop()
        {
            return !Current.IsCollection();
        }

        protected override List<ConfigNode> GetChilds()
        {
            foreach (ConfigNode child in Current.Childs)
                child.ModelContext = ModelContext;
            return Current.Childs;
        }
    }
}
