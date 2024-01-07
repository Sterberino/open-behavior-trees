using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees
{
    public class SelectorNode : CompositeNode
    {
        private int index = 0;

        public override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            while (index < children.Count)
            {
                BehaviorTreeNodeResult result = children[index].Evaluate(behaviorTree);
                if (result == BehaviorTreeNodeResult.running)
                {
                    return BehaviorTreeNodeResult.running;
                }
                else if (result == BehaviorTreeNodeResult.success)
                {
                    index = 0;
                    return BehaviorTreeNodeResult.success;
                }
                else
                {
                    index++;
                }
            }
            index = 0;
            return BehaviorTreeNodeResult.failure;

        }

        public override BehaviorTreeNode Clone()
        {
            SelectorNode node = ScriptableObject.CreateInstance<SelectorNode>();
            node.index = 0;

            node.children = new List<BehaviorTreeNode>();
            node.name = node.name.Replace("(Clone)", "").Trim();

            for (int i = 0; i < children.Count; i++)
            {
                node.children.Add(children[i].Clone());
            }

            return node;
        }
    }


}
