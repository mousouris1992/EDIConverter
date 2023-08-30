using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.tree
{
    /// <summary>
    /// Abstract Tree DFS traverser that provides bare bones implementation
    /// and basic operations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class TreeDFSTraverser<T>
    {
        private Stack<T> Childs = new Stack<T>();     
        protected T Current { get; private set; }  
        protected T Previous { get; private set; }

        public void Traverse(List<T> childs)
        {
            Childs = new Stack<T>(childs);
            while(Childs.Count > 0)
            {
                Previous = Current;
                Current = Peek();
                BeginVisit();
                if (Skip())
                {
                    Pop();
                    continue;
                }
                HandleNode();
                EndVisit();
                if (ShouldPop())
                    Pop();
                foreach (T Child in GetChilds())
                    Push(Child);
            }
        }
        protected virtual void BeginVisit() { }
        protected virtual bool Skip() { return false; }
        protected abstract void HandleNode();
        protected virtual void EndVisit() { }
        protected virtual bool ShouldPop() { return true; }
        protected abstract List<T> GetChilds();
        protected T Peek() { return Childs.Peek(); }
        protected void Pop() { Childs.Pop(); }
        protected void Push(T child) { Childs.Push(child); }
    }
}
