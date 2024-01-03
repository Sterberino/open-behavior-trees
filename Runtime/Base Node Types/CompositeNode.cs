using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenBehaviorTrees;

namespace OpenBehaviorTrees {
    public abstract class CompositeNode : BehaviorTreeNode
    {
        [field: SerializeField]
        public virtual List<BehaviorTreeNode> children { get; set; }
    }
}


