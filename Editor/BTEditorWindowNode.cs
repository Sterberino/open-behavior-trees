using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
//Based on: https://oguzkonya.com/creating-node-based-editor-unity/
namespace OpenBehaviorTrees {
    public class BTEditorWindowNode
    {
        public Rect rect;
        public Rect originalRect;
        public Rect titleRect;

        public string title;
        public bool isDragged;
        public bool isSelected;

        public ConnectionPoint inPoint;
        public ConnectionPoint outPoint;

        public GUIStyle style;
        public GUIStyle defaultNodeStyle;
        public GUIStyle selectedNodeStyle;

        public Action<BTEditorWindowNode> OnRemoveNode;

        public bool selected;
        public bool titleSelected;
        private BehaviorTreeNode behaviorTreeNode;
        private SerializedObject m_serializedObject;
        private bool isSubtree = false;

        public BTEditorWindowNode(
            BehaviorTreeNode behaviorTreeNode,
            Vector2 position,
            Vector2 size,
            GUIStyle nodeStyle,
            GUIStyle selectedStyle,
            GUIStyle inPointStyle,
            GUIStyle outPointStyle,
            Action<ConnectionPoint> OnClickInPoint,
            Action<ConnectionPoint> OnClickOutPoint,
            Action<BTEditorWindowNode> OnClickRemoveNode
        )
        {
            rect = new Rect(position.x, position.y, size.x, size.y);
            originalRect = rect;
            style = nodeStyle;
            inPoint = new ConnectionPoint(this, ConnectionPointType.In, inPointStyle, OnClickInPoint);
            outPoint = new ConnectionPoint(this, ConnectionPointType.Out, outPointStyle, OnClickOutPoint);
            defaultNodeStyle = nodeStyle;
            selectedNodeStyle = selectedStyle;
            OnRemoveNode = OnClickRemoveNode;
            this.behaviorTreeNode = behaviorTreeNode;
            this.m_serializedObject = new SerializedObject(behaviorTreeNode);
            selected = false;
            titleSelected = false;

            isSubtree = this.behaviorTreeNode is SubTreeNode;
        }

        public void Drag(Vector2 delta)
        {
            rect.position += delta;
        }

        public void Draw()
        {
            inPoint?.Draw();
            DrawBehaviorTreeNodeFields(originalRect);
            outPoint?.Draw();
        }

        public SerializedObject GetSerializedObject()
        {
            return m_serializedObject;
        }

        public BehaviorTreeNode GetBehaviorTreeNode()
        {
            return this.behaviorTreeNode;
        }

        private void DrawBehaviorTreeNodeFields(Rect position)
        {
            float boxHeight = EditorGUIUtility.singleLineHeight * 2f;
            float padding = 30f;
            rect = new Rect(rect.position, new Vector2(position.width + padding, position.height + boxHeight + padding));

            position = new Rect(rect.position + new Vector2(padding, padding) / 2f, rect.size - new Vector2(padding, padding));

            GUI.Box(rect, title, style);

            GUIStyle fontStyle = new GUIStyle(EditorStyles.label);
            fontStyle.fontStyle = FontStyle.Bold;
            fontStyle.normal.textColor = selected ? new Color(69f / 255f, 176f / 255f, 224f / 255f) : fontStyle.normal.textColor;
            fontStyle.hover.textColor = fontStyle.normal.textColor;
            fontStyle.alignment = TextAnchor.MiddleCenter;
            

            titleRect = new Rect(position.position, new Vector2(position.width, EditorGUIUtility.singleLineHeight));
            if (titleSelected)
            {
                string newName = GUI.TextArea(titleRect, behaviorTreeNode.name);
                if (!newName.Contains("\n"))
                {
                    behaviorTreeNode.name = newName;
                }

            }
            else
            {
                GUI.Label(titleRect, behaviorTreeNode.name, fontStyle);
            }

            position.y += EditorGUIUtility.singleLineHeight * 1.25f;

            Rect labelPos = position;
            labelPos.height /= 2f;
            labelPos.width /= 2f;
            labelPos.x = position.center.x - labelPos.width / 4f;
            GUI.Label(labelPos, EditorGUIUtility.GetIconForObject(behaviorTreeNode));

            if(isSubtree)
            {
                Rect buttonRect = new Rect(position.x + 40, position.yMax - (2.25f * EditorGUIUtility.singleLineHeight), position.width - 80, EditorGUIUtility.singleLineHeight);
                if(GUI.Button(buttonRect, "Open Subtree"))
                {
                    BehaviorTreeEditorWindow.OpenWindow((this.behaviorTreeNode as SubTreeNode).subtree, true);
                }
            }
        }


        public void SaveNode()
        {
            this.m_serializedObject.Update();
        }

        public void RemovePort(ConnectionPointType connectionPointType)
        {
            if (connectionPointType == ConnectionPointType.In)
            {
                inPoint = null;
            }
            else
            {
                outPoint = null;
            }
        }

        public static string GetNodeTitle(BehaviorTreeNode behaviorTreeNode)
        { 
            string scriptName = behaviorTreeNode.GetType().Name;
            scriptName = System.Text.RegularExpressions.Regex.Replace(scriptName, "([a-z])([A-Z])", "$1 $2");
            return scriptName;
        }

        public void OnClickRemoveNode()
        {
            if (OnRemoveNode != null)
            {
                OnRemoveNode(this);
            }
        }
    }
}
