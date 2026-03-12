//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;

namespace SdfGlueCore.Model.DataNodes
{
    public abstract class TreeNode : IResetable
    {
        public int                  Id                  = 0;
        public ExString             Name                = new ExString("");
        public TreeNode?            Parent              = null;
        public bool                 IsSelected          = false;
        public bool                 IsExpanded          = false;

        private List<TreeNode>      children_           = new List<TreeNode>();

        public TreeNode(int id, String name)
        {
            Id      = id;
            //Id      = GetHashCode();
            Name.Val    = name;
        }

        public IEnumerable<TreeNode> Children
        {
            get
            {
                return children_;
            }
        }

        public void EmptyChildrenList()
        {
            children_ = new List<TreeNode>();
        }

        public int GetChildrenCount()
        {
            return children_.Count;
        }

        public TreeNode GetChildrenAt(int index)
        {
            return children_[index];
        }

        public int GetChildrenIndex(TreeNode node)
        {
            if (!children_.Contains(node))
                return -1;

            return children_.IndexOf(node);
        }

        public TreeNode AddChild(TreeNode obj)
        {
            //if (obj == null)
            //    return null;

            children_.Add(obj);
            obj.Parent = this;

            return obj;
        }

        public TreeNode AddChildAtIndex(TreeNode obj, int index)
        {
            //if (obj == null)
            //    return null;

            children_.Insert(index, obj);
            obj.Parent = this;

            return obj;
        }

        public void RemoveChild(TreeNode obj)
        {
            if (!children_.Contains(obj))
                return;

            children_.Remove(obj);
        }

        public void MoveChildUp(TreeNode obj)
        {
            if (!children_.Contains(obj))
                return;

            int index = children_.IndexOf(obj);
            if (index == 0)
                return;

            children_.RemoveAt(index);
            children_.Insert(index-1, obj);
        }

        public void MoveChildDown(TreeNode obj)
        {
            if (!children_.Contains(obj))
                return;

            int index = children_.IndexOf(obj);
            if (index == children_.Count-1)
                return;

            children_.RemoveAt(index);
            children_.Insert(index+1, obj);
        }

        public delegate void RecursiveAction(TreeNode node);
        //public static void CallRecursive(TreeNode? node, ref bool stopRecursion, RecursiveAction? action)
        public static void CallRecursive(TreeNode? node, RecursiveAction? action)
        {
            if (node == null)
                return;

            //if (stopRecursion)
            //    return;

            if (action != null)
                action(node);

            foreach(TreeNode child in node.Children)
            {
                //CallRecursive(child, ref stopRecursion, action);
                CallRecursive(child, action);
            }
        }

        // Przechodzenie drzewa w taki sposób, że najbierw analizujemy potomków (tryb 'Post-order")
        // https://pl.wikipedia.org/wiki/Przechodzenie_drzewa
        // https://en.wikipedia.org/wiki/Tree_traversal
        //public static void CallRecursiveChildrenFirst(TreeNode? node, ref bool stopRecursion, RecursiveAction action)
        public static void CallRecursiveChildrenFirst(TreeNode? node, RecursiveAction action)
        {
            if (node == null)
                return;

            //if (stopRecursion)
            //    return;

            foreach(TreeNode child in node.Children)
            {
                //CallRecursiveChildrenFirst(child, ref stopRecursion, action);
                CallRecursiveChildrenFirst(child, action);
            }

            if (action != null)
                action(node);
        }

        public abstract void ResetPrevVal();
    }
}
