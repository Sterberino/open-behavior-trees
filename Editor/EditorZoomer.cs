using UnityEngine;
using System.Collections;
using System;

//Source: https://gist.github.com/MattRix/564fa9c36c511ce9ec2b8f5c84022a97#file-editorzoomer-cs

//based on the code in this post: http://martinecker.com/martincodes/unity-editor-window-zooming/
//but I changed how the API works and made it much more flexible
//usage: create an EditorZoomer instance wherever you want to use it (it tracks the pan + zoom state)
//in your OnGUI, draw your scrollable content between zoomer.Begin() and zoomer.End();
//you also must offset your content by zoomer.GetContentOffset();

//The only change I have made to this is wrapping it in the namespace for the purposes of organizing the package.
namespace OpenBehaviorTrees
{
    public class EditorZoomer
    {
        private const float kEditorWindowTabHeight = 21.0f;

        public float zoom = 1f;

        public Rect zoomArea = new Rect();
        public Vector2 zoomOrigin = Vector2.zero;

        Vector2 lastMouse = Vector2.zero;
        Matrix4x4 prevMatrix;

        public Rect Begin(params GUILayoutOption[] options)
        {
            HandleEvents();

            //fill the available area
            var possibleZoomArea = GUILayoutUtility.GetRect(0, 10000, 0, 10000, options);

            if (Event.current.type == EventType.Repaint) //the size is correct during repaint, during layout it's 1,1
            {
                zoomArea = possibleZoomArea;
            }

            GUI.EndGroup(); // End the group Unity begins automatically for an EditorWindow to clip out the window tab. This allows us to draw outside of the size of the EditorWindow.

            Rect clippedArea = zoomArea.ScaleSizeBy(1f / zoom, zoomArea.TopLeft());
            clippedArea.y += kEditorWindowTabHeight;
            GUI.BeginGroup(clippedArea);

            prevMatrix = GUI.matrix;
            Matrix4x4 translation = Matrix4x4.TRS(clippedArea.TopLeft(), Quaternion.identity, Vector3.one);
            Matrix4x4 scale = Matrix4x4.Scale(new Vector3(zoom, zoom, 1.0f));
            GUI.matrix = translation * scale * translation.inverse * GUI.matrix;

            return clippedArea;
        }

        public void End()
        {
            GUI.matrix = prevMatrix; //restore the original matrix
            GUI.EndGroup();
            GUI.BeginGroup(new Rect(0.0f, kEditorWindowTabHeight, Screen.width, Screen.height));
        }

        public void HandleEvents()
        {
            if (Event.current.isMouse)
            {
                if (Event.current.type == EventType.MouseDrag && ((Event.current.button == 0 && Event.current.modifiers == EventModifiers.Alt) || Event.current.button == 2))
                {
                    var mouseDelta = Event.current.mousePosition - lastMouse;

                    zoomOrigin += mouseDelta;

                    Event.current.Use();
                }

                lastMouse = Event.current.mousePosition;
            }

            if (Event.current.type == EventType.ScrollWheel)
            {
                float oldZoom = zoom;

                float zoomChange = 1.10f;

                zoom *= Mathf.Pow(zoomChange, -Event.current.delta.y / 3f);
                zoom = Mathf.Clamp(zoom, 0.1f, 10f);

                bool shouldZoomTowardsMouse = true; //if this is false, it will always zoom towards the center of the content (0,0)

                if (shouldZoomTowardsMouse)
                {
                    //we want the same content that was under the mouse pre-zoom to be there post-zoom as well
                    //in other words, the content's position *relative to the mouse* should not change

                    Vector2 areaMousePos = Event.current.mousePosition - zoomArea.center;

                    Vector2 contentOldMousePos = (areaMousePos / oldZoom) - (zoomOrigin / oldZoom);
                    Vector2 contentMousePos = (areaMousePos / zoom) - (zoomOrigin / zoom);

                    Vector2 mouseDelta = contentMousePos - contentOldMousePos;

                    zoomOrigin += mouseDelta * zoom;
                }

                Event.current.Use();
            }
        }

        public Vector2 GetContentOffset()
        {
            Vector2 offset = -zoomOrigin / zoom; //offset the midpoint

            offset -= (zoomArea.size / 2f) / zoom; //offset the center

            return offset;
        }
    }

    // Helper Rect extension methods
    public static class RectExtensions
    {
        public static Vector2 TopLeft(this Rect rect)
        {
            return new Vector2(rect.xMin, rect.yMin);
        }

        public static Rect ScaleSizeBy(this Rect rect, float scale)
        {
            return rect.ScaleSizeBy(scale, rect.center);
        }

        public static Rect ScaleSizeBy(this Rect rect, float scale, Vector2 pivotPoint)
        {
            Rect result = rect;
            result.x -= pivotPoint.x;
            result.y -= pivotPoint.y;
            result.xMin *= scale;
            result.xMax *= scale;
            result.yMin *= scale;
            result.yMax *= scale;
            result.x += pivotPoint.x;
            result.y += pivotPoint.y;
            return result;
        }
        public static Rect ScaleSizeBy(this Rect rect, Vector2 scale)
        {
            return rect.ScaleSizeBy(scale, rect.center);
        }
        public static Rect ScaleSizeBy(this Rect rect, Vector2 scale, Vector2 pivotPoint)
        {
            Rect result = rect;
            result.x -= pivotPoint.x;
            result.y -= pivotPoint.y;
            result.xMin *= scale.x;
            result.xMax *= scale.x;
            result.yMin *= scale.y;
            result.yMax *= scale.y;
            result.x += pivotPoint.x;
            result.y += pivotPoint.y;
            return result;
        }
    }
}
