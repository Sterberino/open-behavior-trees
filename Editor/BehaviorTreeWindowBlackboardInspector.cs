using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OpenBehaviorTrees {
    public class BehaviorTreeWindowBlackboardInspector
    {
        public bool open { get; set; }
        private SerializedProperty property;

        public BehaviorTreeWindowBlackboardInspector(BehaviorTree behaviorTree)
        {
            SerializedObject ob = new SerializedObject(behaviorTree);
            property = ob.FindProperty("blackboard");
        }

        public void Render(Rect position)
        {
            EditorGUI.BeginChangeCheck();
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.name == "m_Script")
                    {

                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.PropertyField(property, new GUIContent(property.displayName), true);
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(property, new GUIContent(property.displayName), true);
                    }

                } while (property.NextVisible(false));
            }

            property.serializedObject.Update();
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }

        }


    }
}