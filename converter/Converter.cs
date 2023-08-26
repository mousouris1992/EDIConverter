using EDIConverter.model;
using EDIConverter.parser;
using EDIConverter.util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EDIConverter.converter
{
    /// <summary>
    /// Converts to a Model object, given a configuration and an input
    /// </summary>
    public class Converter
    {
        private Stack<ConfigNode> Childs = new Stack<ConfigNode>();

        private ConfigNode? CurrentNode;

        private object? ModelCurrentObject;

        private FileParser? Parser;

        /// <summary>
        /// Converts to a Model object given a configuration and an input
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="input"></param>
        /// <returns>the converted Model</returns>
        public Model ToModel(string configuration, string input)
        {
            Model model = new Model();
            InitializeState(configuration, input, model);
            TraverseNodes();
            return model;
        }

        private void InitializeState(string configuration, string input, Model model)
        {
            JObject jconfig = JObject.Parse(configuration);
            // get childs list
            List<JToken> childsList = ((JArray)jconfig["childs"]).ToList();
            childsList.Reverse();
            // initialize stacks
            Childs.Clear();
            foreach (JToken child in childsList)
                Childs.Push(new ConfigNode(child, null, model));
            // initialize objects 
            CurrentNode = null;
            ModelCurrentObject = model;
            // initialize parser
            Parser = FileParserFactory.Create(jconfig["fileType"].ToString());
            Parser.Parse(input);
        }

        private void TraverseNodes()
        {
            while (Childs.Count > 0)
            {
                InitVisit();
                if (Skip())
                    continue;
                if (CurrentNode.IsCollection())
                    HandleCollectionNode();
                HandleNodeProperty();
                EndVisit();
            }
        }

        private void InitVisit()
        {
            CurrentNode = Childs.Peek();
            SetParentContext();
        }

        private bool Skip()
        {
            bool skip = !Parser.HasProperty(CurrentNode.Value) || (!CurrentNode.CanVisit() && CurrentNode.IsCollection());
            if (skip)
            {
                CurrentNode.ResetIndex();
                Childs.Pop();
            }
            return skip;
        }

        private void HandleCollectionNode()
        {
            if (!CurrentNode.IsVisited())
            {
                CurrentNode.ResetIndex();
                CurrentNode.SetCount(FetchCollectionCount());
                CreateModelCollectionInstance();
            }
        }

        private void HandleNodeProperty()
        {
            SetCurrentContext();
            object obj;
            if (CurrentNode.IsFinal())
                obj = FetchPropertyValue();
            else
                obj = CreatePropertyInstance();
            if (CurrentNode.IsCollection())
                AddModelCollectionItem(obj);
            else
                SetModelProperty(obj);
            ModelCurrentObject = obj;
        }
        
        private void EndVisit()
        {
            if (!CurrentNode.IsCollection())
                Childs.Pop();
            PushChildNodes();
            CurrentNode.Visit();
        }

        private void PushChildNodes()
        {
            foreach (ConfigNode node in CurrentNode.Childs){
                node.ModelContext = ModelCurrentObject;
                node.SetParserContext(Parser.GetContext());
                Childs.Push(node);
            }
        }

        private void SetParentContext()
        {
            Parser.SetContext(CurrentNode.GetParserContext());
        }

        private void SetCurrentContext()
        {
            if (CurrentNode.GetParserContext() == null || CurrentNode.IsCollection())
                Parser.SetContext(CurrentNode.Value, CurrentNode.GetIndex());
        }

        // fetches the collection count corresponding to current node's value, by looking at the input file
        private int FetchCollectionCount()
        {
            return Parser.FetchCollectionCount(CurrentNode.Value);
        }

        // fetches the input object corresponding to current node's value, by looking at the input file
        private string FetchPropertyValue()
        {
            return Parser.FetchValue(CurrentNode.Value);
        }

        // creates an instance of the collection class corresponding to current node
        private void CreateModelCollectionInstance()
        {
            object collection = ReflectionHandler.CreateCollectionInstance(CurrentNode.GetCollectionType(), CurrentNode.ClassName);
            ReflectionHandler.SetProperty(CurrentNode.ModelContext, CurrentNode.GetCollection(), collection);
        }

        // creates an instance of class corresponding to current node
        private object CreatePropertyInstance()
        {
            return ReflectionHandler.CreateInstance(CurrentNode.ClassName);
        }

        // sets model's property with given value
        private void SetModelProperty(object value)
        {
            ReflectionHandler.SetProperty(CurrentNode.ModelContext, CurrentNode.Property, value);
        }

        // adds a model's collection item with given value
        private void AddModelCollectionItem(object value)
        {
            ReflectionHandler.AddCollectionItem(CurrentNode.ModelContext, CurrentNode.GetCollection(), value);
        }
    }
}
