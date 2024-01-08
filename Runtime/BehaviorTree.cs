using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenBehaviorTrees {
    public class BehaviorTree : MonoBehaviour
    {
        public BehaviorTreeNode rootNode;
        [Tooltip("If set to false, you will need to manually call Setup(). Useful for asynchronous scene loading.")]
        public bool startOnEnable;
        public bool paused;

        public Blackboard blackboard;

        private Coroutine currentBehavior;
        [HideInInspector]
        public bool runningBehavior;
        [HideInInspector]
        public BehaviorTreeNode activeSubTree;
        
        private bool setup = false;

        public void Setup()
        {
            if (rootNode == null)
            {
                Debug.Log("Behavior tree root node was null on Gameobject " + gameObject.name);
                this.enabled = false;
            }
            else
            {
                GenerateRuntimeNodes();
                if (blackboard == null)
                {
                    blackboard = new Blackboard();
                }
                setup = true;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(!setup || paused)
            {
                return;
            }

            if (!runningBehavior)
            {
                if (currentBehavior != null)
                {
                    StopCoroutine(currentBehavior);
                }

                currentBehavior = StartCoroutine(RunBehavior());

            }
        }

        private IEnumerator RunBehavior()
        {
            runningBehavior = true;
            BehaviorTreeNodeResult result;


            if (activeSubTree == null)
            {
                result = rootNode.Evaluate(this);
            }
            else
            {
                result = activeSubTree.Evaluate(this);
            }

            while (result == BehaviorTreeNodeResult.running)
            {
                if (paused)
                {
                    yield return null;
                    continue;
                }

                yield return null;
                if (activeSubTree == null)
                {
                    result = rootNode.Evaluate(this);
                }
                else
                {
                    result = activeSubTree.Evaluate(this);
                }
            }

            runningBehavior = false;
            yield return null;
        }

        //Create a runtime instance specific to the NPC for each node.
        private void GenerateRuntimeNodes()
        {
            rootNode = rootNode.Clone();
        }

        public void OnEnable()
        {
            if(startOnEnable)
            {
                Setup();
            }
        }
    }
}

