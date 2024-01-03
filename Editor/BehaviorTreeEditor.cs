using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OpenBehaviorTrees{
[CustomEditor(typeof(BehaviorTree))]
    public class BehaviorTreeEditor : Editor
    {
        //Render everything normally, and then give the option to either open the existing root node or create one if root is null.
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if((target as BehaviorTree).rootNode == null)
            {
                if(GUILayout.Button("Create New Root Node"))
                {
                    BehaviorTreeRootNode rootNode = BehaviorTreeEditorUtilities.CreateNewAsset();
                    (target as BehaviorTree).rootNode = rootNode != null ? rootNode : (target as BehaviorTree).rootNode;
                }
            }
            else
            {
                if (GUILayout.Button("Open In Node Editor"))
                {
                    BehaviorTreeEditorWindow.OpenWindow((target as BehaviorTree).rootNode);
                }
            }
        }
    }
}
