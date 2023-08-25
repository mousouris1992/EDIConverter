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
    public class Converter
    {
        private Stack<ConfigNode> Childs = new Stack<ConfigNode>();

        private HashSet<ConfigNode> VisitedChilds = new HashSet<ConfigNode>();

        private ConfigNode? CurrentNode;

        private object? CurrentObj;

        private FileParser? FileParser;

        // converts to Model given a configuration and an input file
        public Model ToModel(string configContent, string inputContent)
        {
            Model model = new Model();
            InitializeState(configContent, inputContent, model);
            TraverseNodes();
            return model;
        }

        // initializes converter's state
        private void InitializeState(string configContent, string inputContent, Model model)
        {
            JObject config = JObject.Parse(configContent);
            // get childs list
            List<JToken> childsList = ((JArray)config["childs"]).ToList();
            childsList.Reverse();
            // initialize stacks
            Childs.Clear();
            foreach(JToken child in childsList)
                Childs.Push(new ConfigNode(child, null, model));
            VisitedChilds.Clear();
            // initialize objects 
            CurrentNode = null;
            CurrentObj = model;
            // initialize parser
            FileParser = FileParserFactory.Create(config["fileType"].ToString());
            FileParser.Parse(inputContent);
        }

        // traverses configuration nodes
        private void TraverseNodes()
        {
            while (Childs.Count > 0)
            {
                CurrentNode = Childs.Peek();
                if (ShouldSkipNode())
                {
                    Childs.Pop();
                    continue;
                }
                if (CurrentNode.IsCollection())
                {
                    bool skip = HandleCollectionInfo();
                    if (skip)
                        continue;
                }
                HandleNode();
                if (!CurrentNode.IsCollection())
                    Childs.Pop();
                PushChildNodes();
                VisitedChilds.Add(CurrentNode);
            }
        }

        private void PushChildNodes()
        {
            foreach (ConfigNode node in CurrentNode.childs){
                node.parentObj = CurrentObj;
                Childs.Push(node);
            }
        }

        // handles current node as a collection node.
        private bool HandleCollectionInfo()
        {
            bool skip = false;
            if (IsNodeVisited())
            {
                CurrentNode.index++;
                if (CurrentNode.index >= CurrentNode.count)
                {
                    Childs.Pop();
                    VisitedChilds.Remove(CurrentNode);
                    skip = true;
                }
            }
            else
            {
                CurrentNode.index = 0;
                CurrentNode.count = FetchCollectionCount();
                object collection = CreateCollectionInstance();
                SetModelCollection(collection);
            }
            return skip;
        }

        // handles current node's property. Current node is either a simple or a complex node.
        // In case of simple node, the property is fetched from the input file, and set to the model.
        // In case of a complex node, the property is first initialized and then set to the model.
        //TODO refactor
        private void HandleNode()
        {
            object obj;
            if (CurrentNode.IsPrimitiveType())
                obj = FetchPropertyValue();
            else
                obj = CreatePropertyInstance();
            if (CurrentNode.IsCollection())
                AddModelCollectionItem(obj);
            else
                SetModelProperty(obj);
            CurrentObj = obj;
        }

        // decides if current node has been visited
        private bool IsNodeVisited()
        {
            return VisitedChilds.FirstOrDefault(child => child == CurrentNode) != null;
        }

        private bool ShouldSkipNode()
        {
            return !FileParser.HasProperty(CurrentNode.value);
        }

        // fetches the collection count corresponding to current node's value, by looking at the input file
        private int FetchCollectionCount()
        {
            return FileParser.FetchCollectionCount(CurrentNode.value);
        }

        // fetches the input object corresponding to current node's value, by looking at the input file
        private string FetchPropertyValue()
        {
            return FileParser.FetchValue(CurrentNode.value);
        }

        // creates an instance of the collection class corresponding to current node
        private object CreateCollectionInstance()
        {
            return ReflectionHandler.CreateCollectionInstance(CurrentNode.collectionType, CurrentNode.className);
        }

        // creates an instance of class corresponding to current node
        private object CreatePropertyInstance()
        {
            return ReflectionHandler.CreateInstance(CurrentNode.className);
        }

        private void SetModelCollection(object value)
        {
            ReflectionHandler.SetProperty(CurrentNode.parentObj, CurrentNode.collection, value);
        }

        // sets model's property with given value
        private void SetModelProperty(object value)
        {
            ReflectionHandler.SetProperty(CurrentNode.parentObj, CurrentNode.property, value);
        }

        // adds a model's collection item with given value
        private void AddModelCollectionItem(object value)
        {
            ReflectionHandler.AddCollectionItem(CurrentNode.parentObj, CurrentNode.collection, value);
        }
    }
}
