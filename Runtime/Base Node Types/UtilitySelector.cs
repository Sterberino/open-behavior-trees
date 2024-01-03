using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace OpenBehaviorTrees
{
    public class UtilitySelector : UtilityComposite
    {
        [Tooltip("The selected Utility Evaluator will be chosen from the top X results, where X is 'chooseFromTopResults'.")]
        public int chooseFromTopResults;

        public override BehaviorTreeNodeResult Evaluate(BehaviorTree behaviorTree)
        {
            //Map each utility evaluator to its score
            Dictionary<UtilityEvaluator, float> utilityScoresMap = new Dictionary<UtilityEvaluator, float>();
            foreach (UtilityEvaluator evaluator in children)
            {
                utilityScoresMap.Add(evaluator, evaluator.GetScore(behaviorTree));
            }

            //Get the key value pairs from the map, sort them in descending order by the scores.
            List<KeyValuePair<UtilityEvaluator, float>> keyValuePairs = utilityScoresMap.ToList();
            keyValuePairs = keyValuePairs.OrderByDescending(kv => kv.Value).ToList();

            //Select the top result or randomly from the top X results.
            if(chooseFromTopResults == 0)
            {
                return keyValuePairs[0].Key.Evaluate(behaviorTree);
            }
            else
            {
                //Make sure to not choose an index that is out of range.
                int randomChoice = Random.Range(0, Mathf.Min(keyValuePairs.Count, chooseFromTopResults + 1));
                return keyValuePairs[randomChoice].Key.Evaluate(behaviorTree);
            }
        }

        public override BehaviorTreeNode Clone()
        {
            UtilitySelector node = ScriptableObject.CreateInstance<UtilitySelector>();

            node.children = new List<BehaviorTreeNode>();
            node.name = node.name.Replace("(Clone)", "").Trim();

            for (int i = 0; i < children.Count; i++)
            {
                node.children.Add(children[i].Clone());
            }

            return node;
        }
    }

}
