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
        private Stack<JToken> Childs = new Stack<JToken>();

        private Stack<CollectionInfo> Collections = new Stack<CollectionInfo>();

        private HashSet<JToken> VisitedChilds = new HashSet<JToken>();

        private JToken? CurrentNode;

        private object? ParentObject;

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
            Childs = new Stack<JToken>(childsList);
            Collections.Clear();
            VisitedChilds.Clear();
            // initialize objects 
            CurrentNode = null;
            ParentObject = model;
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
                if (IsCollectionNode())
                {
                    bool skip = HandleCollectionInfo();
                    if (skip)
                        continue;
                }
                HandleNode();
                if (!IsCollectionNode())
                    Childs.Pop();
                PushChildNodes();
                VisitedChilds.Add(CurrentNode);
            }
        }

        // pushes current node's childs into the childs stack
        private void PushChildNodes()
        {
            List<JToken> currentNodeChilds = CurrentNode["childs"] != null ? CurrentNode["childs"].ToList() : new List<JToken>();
            currentNodeChilds.Reverse();
            foreach (JToken child in currentNodeChilds)
                Childs.Push(child);
        }

        // handles current node as a collection node.
        private bool HandleCollectionInfo()
        {
            bool skip = false;
            if (IsNodeVisited())
            {
                CollectionInfo collectionInfo = Collections.Peek();
                collectionInfo.index++;
                if (collectionInfo.index >= collectionInfo.count)
                {
                    Childs.Pop();
                    Collections.Pop();
                    VisitedChilds.Remove(CurrentNode);
                    skip = true;
                }
                else
                    ParentObject = collectionInfo.parentObject;
            }
            else
            {
                object collection = CreateCollectionInstance();
                SetModelCollection(collection);
                Collections.Push(new CollectionInfo()
                {
                    parentObject = ParentObject,
                    collection = collection,
                    count = FetchCollectionCount()
                });
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
            if (IsSimpleNode())
                obj = FetchPropertyValue();
            else
                obj = CreatePropertyInstance();
            if (IsCollectionNode())
                AddModelCollectionItem(obj);
            else
                SetModelProperty(obj);
            if (!IsSimpleNode())
                ParentObject = obj;
        }

        // decides if current node has been visited
        private bool IsNodeVisited()
        {
            return VisitedChilds.FirstOrDefault(child => child == CurrentNode) != null;
        }

        // decides if current node is a simple node
        private bool IsSimpleNode()
        {
            return CurrentNode["childs"] == null;
        }

        private bool ShouldSkipNode()
        {
            return !FileParser.HasProperty(CurrentNode["value"].ToString());
        }

        // decides if current node is a collection node
        private bool IsCollectionNode()
        {
            return CurrentNode["collectionType"] != null;
        }

        // fetches the collection count corresponding to current node's value, by looking at the input file
        private int FetchCollectionCount()
        {
            return FileParser.FetchCollectionCount(CurrentNode["value"].ToString());
        }

        // fetches the input object corresponding to current node's value, by looking at the input file
        private string FetchPropertyValue()
        {
            return FileParser.FetchValue(CurrentNode["value"].ToString());
        }

        // creates an instance of the collection class corresponding to current node
        private object CreateCollectionInstance()
        {
            return ReflectionHandler.CreateCollectionInstance(CurrentNode["collectionType"].ToString(), CurrentNode["class"].ToString());
        }

        // creates an instance of class corresponding to current node
        private object CreatePropertyInstance()
        {
            return ReflectionHandler.CreateInstance(CurrentNode["class"].ToString());
        }

        private void SetModelCollection(object value)
        {
            ReflectionHandler.SetProperty(ParentObject, CurrentNode["collection"].ToString(), value);
        }

        // sets model's property with given value
        private void SetModelProperty(object value)
        {
            ReflectionHandler.SetProperty(ParentObject, CurrentNode["property"].ToString(), value);
        }

        // adds a model's collection item with given value
        private void AddModelCollectionItem(object value)
        {
            ReflectionHandler.AddCollectionItem(ParentObject, CurrentNode["collection"].ToString(), value);
        }
    }
}
