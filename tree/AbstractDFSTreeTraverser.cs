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
    public abstract class AbstractDFSTreeTraverser<T>
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
        public virtual void BeginVisit() { }
        public virtual bool Skip() { return false; }
        public abstract void HandleNode();
        public virtual void EndVisit() { }
        public virtual bool ShouldPop() { return true; }
        public abstract List<T> GetChilds();
        public T Peek() { return Childs.Peek(); }
        public void Pop() { Childs.Pop(); }
        public void Push(T child) { Childs.Push(child); }
    }
}
