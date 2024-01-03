using System;
using UnityEngine;

//Source: https://oguzkonya.com/creating-node-based-editor-unity/
namespace OpenBehaviorTrees
{
    public enum ConnectionPointType { In, Out }

    public class ConnectionPoint
    {
        public Rect rect;

        public ConnectionPointType type;

        public BTEditorWindowNode node;

        public GUIStyle style;

        public Action<ConnectionPoint> OnClickConnectionPoint;

        public ConnectionPoint(BTEditorWindowNode node, ConnectionPointType type, GUIStyle style, Action<ConnectionPoint> OnClickConnectionPoint)
        {
            this.node = node;
            this.type = type;
            this.style = style;
            this.OnClickConnectionPoint = OnClickConnectionPoint;
            rect = new Rect(0, 0, 30f, 12f);
        }

        public void Draw()
        {
            //rect.y = node.rect.y + (node.rect.height * 0.5f) - rect.height * 0.5f;
            rect.x = node.rect.x + (node.rect.width * 0.5f) - rect.width * 0.5f;

            switch (type)
            {
                case ConnectionPointType.In:
                    rect.y = node.rect.y - rect.height + ((node.GetBehaviorTreeNode() is SubTreeNode) ? 10f : 6f);
                    //rect.x = node.rect.x - rect.width + 8f; 
                    break;

                case ConnectionPointType.Out:
                    rect.y = node.rect.y + node.rect.height - ((node.GetBehaviorTreeNode() is SubTreeNode) ? 12f : 8.5f);
                    //rect.x = node.rect.x + node.rect.width - 8f;
                    break;
            }

            if (GUI.Button(rect, "", style))
            {
                if (OnClickConnectionPoint != null)
                {
                    OnClickConnectionPoint(this);
                }
            }
        }
    }

}
