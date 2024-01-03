using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenBehaviorTrees;

namespace OpenBehaviorTrees {

    [CreateAssetMenu(fileName = "Behavior Tree Sequencer Node", menuName = "Custom Scriptable Objects/Behavior Trees/Behavior Tree Sequencer Node")]
    public class SequencerNode : CompositeNode
    {
        private int index = 0;

        public override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            if (index < children.Count)
            {
                BehaviorTreeNodeResult result = children[index].Evaluate(behaviorTree);
                if (result == BehaviorTreeNodeResult.running)
                {
                    //Debug.Log("Running child: " + children[index].name);
                    return BehaviorTreeNodeResult.running;
                }
                else if (result == BehaviorTreeNodeResult.failure)
                {
                    index = 0;
                    return BehaviorTreeNodeResult.failure;
                }
                else
                {
                    index++;
                    if (index < children.Count)
                    {
                        return BehaviorTreeNodeResult.running;
                    }
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
