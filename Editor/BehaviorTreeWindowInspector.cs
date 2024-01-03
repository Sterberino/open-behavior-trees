using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OpenBehaviorTrees
{
    public class BehaviorTreeWindowInspector
    {
        public bool open { get; set; }
        private Rect m_rect;
        private Vector2 scrollPosition;


        public SerializedObject m_serializedObject;

        public void SetRect(Rect rect)
        {
            this.m_rect = rect;
        }

        public Rect GetRect()
        {
            return m_rect;
        }

        public void Render(List<BTEditorWindowNode> nodes, float windowWidth)
        {
            BTEditorWindowNode node = nodes.Count == 1 ? nodes[0] : null;
            m_rect = EditorGUILayout.BeginVertical("box", GUILayout.Width(windowWidth), GUILayout.MinWidth(300), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (nodes.Count == 0)
            {
                EditorGUILayout.LabelField("No items Selected", GUILayout.Width(windowWidth), GUILayout.MinWidth(200));
            }
            else if (nodes.Count > 1)
            {
                EditorGUILayout.LabelField($"{nodes.Count} items Selected", GUILayout.Width(windowWidth), GUILayout.MinWidth(200));
            }
            else
            {
                SerializedObject ob = node.GetSerializedObject();
                SerializedProperty property = ob.GetIterator();

                EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.MinWidth(287), GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField("Name", GUILayout.Width(50));
                node.GetBehaviorTreeNode().name = EditorGUILayout.TextField(node.GetBehaviorTreeNode().name, new GUIStyle("BoldTextField"), GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

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

                ob.Update();
                if (EditorGUI.EndChangeCheck())
                {
                    node.GetBehaviorTreeNode().OnValidate();
                }

                GUILayout.BeginHorizontal();
                BehaviorTreeNode n = nodes[0].GetBehaviorTreeNode();

                Texture2D tex = EditorGUIUtility.GetIconForObject(n);
                tex = (Texture2D)EditorGUILayout.ObjectField("Icon", tex, typeof(Texture), true);
                EditorGUIUtility.SetIconForObject(n, tex);

                GUILayout.EndHorizontal();

            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();


        }

        public void ProcessEvents(Event e)
        {

        }
    }

}
