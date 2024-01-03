using System;
using UnityEditor;
using UnityEngine;

//Source: https://oguzkonya.com/creating-node-based-editor-unity/
namespace OpenBehaviorTrees
{
    public class Connection
    {
        public ConnectionPoint inPoint;
        public ConnectionPoint outPoint;
        public Action<Connection> OnClickRemoveConnection;

        public Connection(ConnectionPoint inPoint, ConnectionPoint outPoint, Action<Connection> OnClickRemoveConnection)
        {
            this.inPoint = inPoint;
            this.outPoint = outPoint;
            this.OnClickRemoveConnection = OnClickRemoveConnection;
        }

        public void Draw()
        {
            Handles.DrawBezier(
                inPoint.rect.center,
                outPoint.rect.center,
                inPoint.rect.center + Vector2.down * 50f,
                outPoint.rect.center - Vector2.down * 50f,
                Color.white,
                null,
                2f
            );

            Vector2 buttonPos = (inPoint.rect.center + outPoint.rect.center) * 0.5f;
            if (Handles.Button(buttonPos, Quaternion.identity, 4, 8, Handles.RectangleHandleCap))
            {
                if (OnClickRemoveConnection != null)
                {
                    ProcessContextMenu(buttonPos);
                }
            }
        }

        private void ProcessContextMenu(Vector2 mousePosition)
        {
            GenericMenu genericMenu = new GenericMenu();
            genericMenu.AddItem(new GUIContent("Delete Connection"), false, () => OnClickRemoveConnection(this));
            genericMenu.ShowAsContext();
        }
    }

}
