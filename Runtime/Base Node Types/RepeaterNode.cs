using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees {
    public class RepeaterNode : DecoratorNode
    {
        [Tooltip("If set to true, the repeater will repeat indefinitely.")]
        public bool repeatForever;

        [Tooltip("The number of times the repeater node should repeat.")]
        public int repeatCount;
        private int timesRepeated = 0;
        [Tooltip("The result that is returned when the node has repeated [repeatCount] times.")]
        public BehaviorTreeNodeResult resultOnComplete = BehaviorTreeNodeResult.success;

        public override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            
            if(repeatForever)
            {
                child.Evaluate(behaviorTree);
                return BehaviorTreeNodeResult.running;
            }
            
            if(timesRepeated < repeatCount)
            {
                timesRepeated++;
                child.Evaluate(behaviorTree);
                return BehaviorTreeNodeResult.running;
            }
            else
            {
                timesRepeated = 0;
                return resultOnComplete;
            }
        }
    }

}

