using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenBehaviorTrees;

namespace OpenBehaviorTrees
{
    public enum BehaviorTreeNodeResult { running, success, failure }

    public abstract class BehaviorTreeNode : ScriptableObject
    {
        public virtual BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            return BehaviorTreeNodeResult.success;
        }
        public abstract BehaviorTreeNode Clone();

        /// <summary>
        /// Gets all children nodes, not including the current node.
        /// </summary>
        /// <returns></returns>
        public List<BehaviorTreeNode> GetAllChildren()
        {
            List<BehaviorTreeNode> children = new List<BehaviorTreeNode>();

            switch (this)
            {
                case CompositeNode compositeNode:
                    foreach (BehaviorTreeNode node in compositeNode.children)
                    {
                        children.Add(node);
                        children.AddRange(node.GetAllChildren());
                    }
                    break;
                case DecoratorNode decorator:
                    children.Add(decorator.child);
                    children.AddRange(decorator.child.GetAllChildren());
                    break;
                default:
                    break;
            }


            return children;
        }

        public virtual void OnValidate()
        {
        }
    }
}