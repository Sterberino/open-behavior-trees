using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OpenBehaviorTrees
{
    public class BehaviorTreeWindowHeader
    {
        private BehaviorTreeWindowInspector m_inspector;
        private BehaviorTreeWindowNodesList m_nodesList;
        private BehaviorTreeEditorWindow m_nodeEditor;

        private GUIStyle leftMostButtonStyle;
        private GUIStyle rightmostButtonStyle;
        private GUIStyle buttonStyle;

        private Rect m_rect;

        public BehaviorTreeWindowHeader(BehaviorTreeWindowInspector inspector, BehaviorTreeWindowNodesList nodesList, BehaviorTreeEditorWindow nodeEditor)
        {
            this.m_nodesList = nodesList;
            this.m_inspector = inspector;
            this.m_nodeEditor = nodeEditor;

            GUIStyle style = new GUIStyle("AppToolbarButtonMid");
            style.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn mid.png") as Texture2D;
            style.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn mid on.png") as Texture2D;

            buttonStyle = new GUIStyle(style);
            leftMostButtonStyle = new GUIStyle(style);
            rightmostButtonStyle = new GUIStyle(style);

        }

        public void Render(Rect position, float sidebarWidth)
        {
            m_rect = EditorGUILayout.BeginVertical("box", GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.Width(position.width));


            EditorGUILayout.BeginHorizontal(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));

            GUIStyle style = new GUIStyle(leftMostButtonStyle);

            int borderThickness = 5;
            style.border = m_inspector.open ? new RectOffset(borderThickness, borderThickness, borderThickness, borderThickness) : style.border;


            if (GUILayout.Button("Inspector", leftMostButtonStyle, GUILayout.Width(sidebarWidth / 2), GUILayout.MinWidth(150), GUILayout.ExpandWidth(false)))
            {
                m_inspector.open = !m_inspector.open;
                m_nodesList.open = false;
            }
            if (GUILayout.Button("Nodes", buttonStyle, GUILayout.Width(sidebarWidth / 2), GUILayout.MinWidth(150), GUILayout.ExpandWidth(false)))
            {
                m_nodesList.open = !m_nodesList.open;
                m_inspector.open = false;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", rightmostButtonStyle, GUILayout.Width(sidebarWidth / 2), GUILayout.MinWidth(100)))
            {
                m_nodeEditor.Save();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        public void ProcessEvents(Event e)
        {

        }
    }

}
