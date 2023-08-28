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
            InitializeState(configuration, input);
            Traverse(GetRootChilds());
            return Model;
        }

        private void InitializeState(string configuration, string input)
        {
            Model = new Model();
            Config = JObject.Parse(configuration);
            ModelContext = Model;
            Parser = FileParserFactory.Create(Config["fileType"].ToString());
            Parser.Parse(input);
        }

        private List<ConfigNode> GetRootChilds()
        {
            // get configuration childs
            List<JToken> childsList = ((JArray)Config["childs"]).ToList();
            childsList.Reverse();
            // initialize Childs
            List<ConfigNode> childs = new List<ConfigNode>();
            foreach (JToken child in childsList)
                childs.Add(new ConfigNode(child, null, Model));
            return childs;
        }

        public override bool Skip()
        {
            bool skip = !Parser.HasProperty(Current.ResolveFullPath()) || (!Current.CanVisit() && Current.IsCollection());
            if (skip)
                Current.ResetIndex();
            return skip;
        }

        public override void HandleNode()
        {
            if (Current.IsCollection())
                HandleNodeCollection();
            HandleNodeProperty();
        }

        private void HandleNodeCollection()
        {
            if (!Current.IsVisited())
            {
                Current.ResetIndex();
                Current.SetCount(FetchCollectionCount());
                CreateModelCollectionInstance();
            }
        }

        private void HandleNodeProperty()
        {
            object obj;
            if (Current.IsFinal())
                obj = FetchPropertyValue();
            else
                obj = CreatePropertyInstance();
            if (Current.IsCollection())
                AddModelCollectionItem(obj);
            else
                SetModelProperty(obj);
            ModelContext = obj;
        }

        public override void EndVisit()
        {
            Current.Visit();
        }

        public override bool ShouldPop()
        {
            return !Current.IsCollection();
        }

        public override List<ConfigNode> GetChilds()
        {
            List<ConfigNode> childs = Current.Childs;
            foreach (ConfigNode child in childs)
            {
                child.ModelContext = ModelContext;
                child.SetParserContext(Parser.GetContext());
            }
            return childs;
        }

        private int FetchCollectionCount()
        {
            return Parser.FetchCollectionCount(Current.ResolveFullPath());
        }

        private string FetchPropertyValue()
        {
            return Parser.FetchValue(Current.ResolveFullPath());
        }

        private void CreateModelCollectionInstance()
        {
            object collection = ReflectionHandler.CreateCollectionInstance(Current.GetCollectionType(), Current.ClassName);
            ReflectionHandler.SetProperty(Current.ModelContext, Current.GetCollection(), collection);
        }

        private object CreatePropertyInstance()
        {
            return ReflectionHandler.CreateInstance(Current.ClassName);
        }

        private void SetModelProperty(object value)
        {
            ReflectionHandler.SetProperty(Current.ModelContext, Current.Property, value);
        }

        private void AddModelCollectionItem(object value)
        {
            ReflectionHandler.AddCollectionItem(Current.ModelContext, Current.GetCollection(), value);
        }
    }
}
