using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenBehaviorTrees;

namespace OpenBehaviorTrees {
    public abstract class DecoratorNode : BehaviorTreeNode
    {
        [field: SerializeField]
        public virtual BehaviorTreeNode child { get; set; }

        public override BehaviorTreeNode Clone()
        {
            DecoratorNode node = ScriptableObject.CreateInstance<DecoratorNode>();
            node.name = node.name.Replace("(Clone)", "").Trim();

            node.child = child == null ? null : child.Clone();
            return node;
        }
    }

}
