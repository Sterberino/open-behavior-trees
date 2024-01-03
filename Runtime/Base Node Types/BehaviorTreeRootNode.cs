using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees {
    //Special Root Node
    public class BehaviorTreeRootNode : DecoratorNode
    {
        public override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            return child.Evaluate(behaviorTree);
        }

        public override BehaviorTreeNode Clone()
        {
            BehaviorTreeRootNode node = ScriptableObject.CreateInstance<BehaviorTreeRootNode>();
            node.name = node.name.Replace("(Clone)", "").Trim();

            node.child = child == null ? null : child.Clone();
            return node;
        }
    }

}


