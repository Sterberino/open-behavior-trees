using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees
{
    public class ParallelNode : CompositeNode
    {
        [Tooltip("The return value returned when Children nodes return mixed results.")]
        public BehaviorTreeNodeResult defaultToResult;

        public override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            bool successResult = false;
            bool runningResult = false;
            bool failureResult = false;
            //Run all children, check for result types being found
            for(int i = 0; i < children.Count; i++)
            {
                BehaviorTreeNodeResult result = children[i].Evaluate(behaviorTree);
                switch (result)
                {
                    case BehaviorTreeNodeResult.running: runningResult = true; break;
                    case BehaviorTreeNodeResult.success: successResult = true; break;
                    case BehaviorTreeNodeResult.failure: failureResult = true; break;
                }
            }

            //If only one result type is found, we return that result type, otherwise we return the default value
            if(successResult && (! runningResult && !failureResult))
            {
                return BehaviorTreeNodeResult.success;
            }
            else if(runningResult && (!successResult && !failureResult))
            {
                return BehaviorTreeNodeResult.running;
            }
            else if(failureResult && (!runningResult && !successResult))
            {
                return BehaviorTreeNodeResult.failure;
            }

            return defaultToResult;
        }


        public override BehaviorTreeNode Clone()
        {
            ParallelNode node = ScriptableObject.CreateInstance<ParallelNode>();
            node.children = new List<BehaviorTreeNode>();
            for(int i = 0; i <  children.Count; i++)
            {
                node.children.Add(children[i].Clone());
            }

            return node;
        }
    }

}

