using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace OpenBehaviorTrees
{
    [CustomEditor(typeof(BehaviorTreeNode), true)]
    public class BehaviorTreeNodeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!AssetDatabase.IsSubAsset(target))
            {
                if (GUILayout.Button("Open in Node Editor"))
                {
                    BehaviorTreeEditorWindow.OpenWindow(target as BehaviorTreeNode);
                }
            }
        }


        
    }

}
