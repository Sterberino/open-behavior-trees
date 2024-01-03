using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees
{
    //Special Node type used for organizing sub trees in the node editor.
    public class SubTreeNode : BehaviorTreeNode
    {
        [ReadOnly]
        public BehaviorTreeRootNode subtree;

        public override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            return subtree.Evaluate(behaviorTree);
        }

        public override BehaviorTreeNode Clone()
        {
            SubTreeNode node = ScriptableObject.CreateInstance<SubTreeNode>();
            
            if(Application.isPlaying)
            {
                node.subtree = (BehaviorTreeRootNode)subtree.Clone();
            }
            else
            {
                node.subtree = subtree;
            }
            return node;
        }

    }
}

