using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenBehaviorTrees;

namespace OpenBehaviorTrees {


    public class SequencerNode : CompositeNode
    {
        private int index = 0;

        protected override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            while (index < children.Count)
            {
                BehaviorTreeNodeResult result = children[index].Tick(behaviorTree);
                switch (result)
                {
                    case BehaviorTreeNodeResult.running:
                        return BehaviorTreeNodeResult.running;
                    case BehaviorTreeNodeResult.failure:
                        index = 0;
                        return BehaviorTreeNodeResult.failure;
                    case BehaviorTreeNodeResult.success:
                        index++;
                        break;
                }
            }
            index = 0;
            return BehaviorTreeNodeResult.success;

        }

        public override BehaviorTreeNode Clone()
        {
            SequencerNode node = ScriptableObject.Instantiate(this);
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
