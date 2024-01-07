using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees
{
    public abstract class UtilityEvaluator : DecoratorNode
    {
        public override BehaviorTreeNode Clone()
        {
            UtilityEvaluator clone = ScriptableObject.CreateInstance<UtilityEvaluator>();
            clone.child = child.Clone();
            return clone;
        }

        public abstract float GetScore(BehaviorTree behaviorTree);
    }

}
