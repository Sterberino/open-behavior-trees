using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OpenBehaviorTrees;
using UnityEngine.Events;

namespace OpenBehaviorTrees
{
    public enum BehaviorTreeNodeResult { running, success, failure }

    public class BehaviorNodeEvent : UnityEvent<BehaviorTreeNode> { };

    public abstract class BehaviorTreeNode : ScriptableObject
    {
        [HideInInspector]
        public BehaviorTreeNodeResult lastStatus = BehaviorTreeNodeResult.success;
        [HideInInspector]
        public BehaviorNodeEvent onTick;
        
        private bool m_init = false;
        private bool m_started = false;

        /// <summary>
        /// Called when the behavior tree ticks. Wraps the Evaluate method and caches the result. 
        /// Calls OnInit() and OnStart() on the first tick if they are defined.
        /// </summary>
        public BehaviorTreeNodeResult Tick(BehaviorTree behaviorTree)
        {
            if(!m_init)
            {
                Init(behaviorTree);
            }
            if(!m_started)
            {
                NodeStart(behaviorTree);
            }

            lastStatus = Evaluate(behaviorTree);
            onTick?.Invoke(this);

            return lastStatus;
        }

        private void Init(BehaviorTree behaviorTree)
        {
            OnInit(behaviorTree);
            m_init = true;
        }

        private void NodeStart(BehaviorTree behaviorTree)
        {
            OnStart(behaviorTree);
            m_started = true;
        }

        /// <summary>
        /// Called on the first node tick, before OnStart.
        /// </summary>
        protected virtual void OnInit(BehaviorTree behaviorTree){}

        /// <summary>
        /// Called on the first node tick, after OnInit.
        /// </summary>
        protected virtual void OnStart(BehaviorTree behaviorTree) { }

        protected abstract BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree);

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

        public virtual void OnValidate(){}
    }
}