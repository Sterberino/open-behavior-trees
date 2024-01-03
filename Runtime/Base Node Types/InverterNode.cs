using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees {
    public class InverterNode : DecoratorNode
    {
        //If child result is running, return running, otherwise, return opposite result of child.
        public override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            BehaviorTreeNodeResult result = child.Evaluate(behaviorTree);

            switch (result)
            {
                case BehaviorTreeNodeResult.running:
                    return BehaviorTreeNodeResult.running;
                case BehaviorTreeNodeResult.success:
                    return BehaviorTreeNodeResult.failure;
                case BehaviorTreeNodeResult.failure:
                    return BehaviorTreeNodeResult.success;
                default:
                    //Something went wrong.
                    return BehaviorTreeNodeResult.failure;
            }
        }

        public override BehaviorTreeNode Clone()
        {
            InverterNode node = ScriptableObject.Instantiate(this);
            node.name = node.name.Replace("(Clone)", "").Trim();

            node.child = child.Clone();
            return node;
        }
    }

}

