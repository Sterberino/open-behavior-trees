using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace OpenBehaviorTrees
{
    [CreateAssetMenu(fileName = "Utility Selector", menuName = "Custom Scriptable Objects/Behavior Trees/Utility Selector")]
    public abstract class UtilityComposite : CompositeNode
    {
        /*The [field: SerializeField] attribute header IS required to get the Editor to show getter-setter variables, but adding
         "field:" creates a warning. This can be ignored, and so to reduce console clutter I have ignored it here.*/
#pragma warning disable 0657
        [field: SerializeField]
#pragma warning restore 0657
        public override List<BehaviorTreeNode> children
        {
            get
            {
                if (base.children == null)
                {
                    base.children = new List<BehaviorTreeNode>();
                }
                return base.children;
            }
            set
            {
                base.children = value.Where(item => item is UtilityEvaluator).ToList();
            }
        }



        public override void OnValidate()
        {
            ValidateChildren();
        }

        protected void ValidateChildren()
        {
            children = children.Where(item => item is UtilityEvaluator).ToList();
        }
    }


}
