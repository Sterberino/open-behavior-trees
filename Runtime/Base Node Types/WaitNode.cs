using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees { 
    public class WaitNode : ConditionNode
    {
        public enum TimeType { deltaTime, fixedDeltaTime, unscaledDeltaTime, fixedUnscaledDeltaTime, smoothDeltaTime };
        [Tooltip("The type of time that is used by the counter to determine if enough time has passed.")]
        public TimeType timeType = TimeType.fixedDeltaTime;

        [Tooltip("The amount of time that must pass for the node to return BehaviorTreeNodeResult.success.")]
        public float waitTime = 1f;
        private float counter = 0;

        public override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            switch (timeType)
            {
                case TimeType.fixedDeltaTime:
                    counter += Time.fixedDeltaTime; break;
                case TimeType.deltaTime:
                    counter += Time.deltaTime; break;
                case TimeType.fixedUnscaledDeltaTime:
                    counter += Time.fixedUnscaledDeltaTime; break;
                case TimeType.unscaledDeltaTime:
                    counter += Time.unscaledDeltaTime; break;
                case TimeType.smoothDeltaTime:
                    counter += Time.smoothDeltaTime; break;
            }
            
            if(counter >= waitTime)
            {
                counter = 0;
                return BehaviorTreeNodeResult.success;
            }
            return BehaviorTreeNodeResult.failure;
        }

        public override BehaviorTreeNode Clone()
        {
            WaitNode node = ScriptableObject.CreateInstance<WaitNode>();
            node.waitTime = this.waitTime;

            return node;
        }
    }
}