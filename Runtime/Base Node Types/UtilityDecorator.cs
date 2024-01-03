using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees
{
    public abstract class UtilityDecorator : DecoratorNode
    {
        private UtilityEvaluator m_child;

        [SerializeField]
        public override BehaviorTreeNode child
        {
            get { return base.child; }
            set
            {
                if (value is UtilityEvaluator)
                {
                    base.child = value;
                    m_child = value as UtilityEvaluator;
                }
                else
                {
                    base.child = m_child;
                }
            }
        }

        public override void OnValidate()
        {
            ValidateChildren();
        }

        protected void ValidateChildren()
        {
            base.OnValidate();
            if (child != null && !(child is UtilityEvaluator))
            {
                child = m_child;
            }
        }
    }

}

