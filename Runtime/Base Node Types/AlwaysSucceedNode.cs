using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees {
    public class AlwaysSucceedNode : DecoratorNode
    {
        protected override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            child.Tick(behaviorTree);
            return BehaviorTreeNodeResult.success;
        }


        public override BehaviorTreeNode Clone()
        {
            AlwaysSucceedNode node = ScriptableObject.Instantiate(this);
            node.name = node.name.Replace("(Clone)", "").Trim();
            node.child = child.Clone();
            return node;
        }
    }
}


