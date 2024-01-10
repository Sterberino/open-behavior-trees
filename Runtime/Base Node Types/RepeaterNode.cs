using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees {
    public class RepeaterNode : DecoratorNode
    {
        [Tooltip("If set to true, the repeater will repeat indefinitely.")]
        public bool repeatForever;

        [Tooltip("The number of times the repeater node should repeat.")]
        [DrawIf("repeatForever", true)]
        public int repeatCount;
        private int timesRepeated = 0;
        [Tooltip("The result that is returned when the node has repeated [repeatCount] times.")]
        public BehaviorTreeNodeResult resultOnComplete = BehaviorTreeNodeResult.success;

        protected override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            
            if(repeatForever)
            {
                child.Tick(behaviorTree);
                return BehaviorTreeNodeResult.running;
            }
            
            if(timesRepeated < repeatCount)
            {
                timesRepeated++;
                child.Tick(behaviorTree);
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

