using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Source: https://rachel53461.wordpress.com/2014/04/20/algorithm-for-drawing-trees/
namespace OpenBehaviorTrees
{
    public class TreeNodeModel<T>
        where T : class
    {
        public float X { get; set; }
        public int Y { get; set; }
        public float Mod { get; set; }
        public TreeNodeModel<T> Parent { get; set; }
        public List<TreeNodeModel<T>> Children { get; set; }

        public float Width { get; set; }
        public int Height { get; set; }

        public T Item { get; set; }

        public TreeNodeModel(T item, TreeNodeModel<T> parent)
        {
            this.Item = item;
            this.Parent = parent;
            this.Children = new List<TreeNodeModel<T>>();
        }

        public bool IsLeaf()
        {
            return this.Children.Count == 0;
        }

        public bool IsLeftMost()
        {
            if (this.Parent == null)
                return true;

            return this.Parent.Children[0] == this;
        }

        public bool IsRightMost()
        {
            if (this.Parent == null)
                return true;

            return this.Parent.Children[this.Parent.Children.Count - 1] == this;
        }

        public TreeNodeModel<T> GetPreviousSibling()
        {
            if (this.Parent == null || this.IsLeftMost())
                return null;

            return this.Parent.Children[this.Parent.Children.IndexOf(this) - 1];
        }

        public TreeNodeModel<T> GetNextSibling()
        {
            if (this.Parent == null || this.IsRightMost())
                return null;

            return this.Parent.Children[this.Parent.Children.IndexOf(this) + 1];
        }

        public TreeNodeModel<T> GetLeftMostSibling()
        {
            if (this.Parent == null)
                return null;

            if (this.IsLeftMost())
                return this;

            return this.Parent.Children[0];
        }

        public TreeNodeModel<T> GetLeftMostChild()
        {
            if (this.Children.Count == 0)
                return null;

            return this.Children[0];
        }

        public TreeNodeModel<T> GetRightMostChild()
        {
            if (this.Children.Count == 0)
                return null;

            return this.Children[Children.Count - 1];
        }

        public override string ToString()
        {
            return Item.ToString();
        }
    }
}