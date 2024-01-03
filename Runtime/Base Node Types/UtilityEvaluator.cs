using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees
{
    [CreateAssetMenu(fileName = "Utility Evaluator", menuName = "Custom Scriptable Objects/Behavior Trees/Utility Evaluator")]
    public abstract class UtilityEvaluator : BehaviorTreeNode
    {
        public override BehaviorTreeNode Clone()
        {
            return ScriptableObject.CreateInstance<UtilityEvaluator>();
        }

        public abstract float GetScore(BehaviorTree behaviorTree);
    }

}

