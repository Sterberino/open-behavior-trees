using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

//Based on: https://oguzkonya.com/creating-node-based-editor-unity/
namespace OpenBehaviorTrees
{
    public class BehaviorTreeEditorWindow : EditorWindow
    {
        private List<BTEditorWindowNode> selectedNodes;
        private List<BTEditorWindowNode> nodes;
        private List<Connection> connections;
        
        private bool dirty = false;
        private string assetPath = string.Empty;
        private BehaviorTreeNode root;

        //styles
        private GUIStyle nodeStyle;
        private GUIStyle selectedNodeStyle;
        private GUIStyle subtreeStyle;
        private GUIStyle selectedSubtreeStyle;
        private GUIStyle inPointStyle;
        private GUIStyle outPointStyle;

        private ConnectionPoint selectedInPoint;
        private ConnectionPoint selectedOutPoint;

        private Vector2 offset;
        private Vector2 drag;

        private Rect m_lastSize;
        private Rect m_nodeWindowRect;

        private EditorZoomer m_editorZoomer;
        
        //Elements drawn at the top of the windpw
        private BehaviorTreeWindowInspector inspector;
        private BehaviorTreeWindowNodesList nodesList;
        private BehaviorTreeWindowHeader header;

        //Currently unused
        public BehaviorTreeWindowBlackboardInspector blackboard;
        //For copying and pasting nodes.
        private List<BTEditorWindowNode> clipboard;
        
        #region InitializationAndSetup
        /// <summary>
        /// Opens a Behavior tree node in the editor window.
        /// </summary>
        public static void OpenWindow( BehaviorTreeNode rootNode, bool forceNewWindow = false)
        {
            BehaviorTreeEditorWindow window;
            if (forceNewWindow)
            {
                window = CreateWindow<BehaviorTreeEditorWindow>();
            }
            else
            {
                window = GetWindow<BehaviorTreeEditorWindow>();
            }
            window.titleContent = new GUIContent(rootNode.name);

            window.assetPath = AssetDatabase.GetAssetPath(rootNode);
            window.root = rootNode.Clone();
            window.root.name = rootNode.name;
            window.Clear();
            window.PlaceNodes(window.position, window.root);
        }

        /// <summary>
        /// Automatically places all nodes in the behavior tree into the Window, spaces them out, and draws connections.
        /// </summary>
        private void PlaceNodes(Rect windowRect, BehaviorTreeNode rootNode)
        {
            //Helper class for determining node positions
            TreeNodeModel<BehaviorTreeNode> rootModelNode = CreateTreeModel(rootNode);
            TreeHelpers<BehaviorTreeNode>.CalculateNodePositions(rootModelNode);

            //Initial rootPosition
            Vector2 rootPosition = new Vector2(rootModelNode.X, rootModelNode.Y);
            //Represents the distance between placed nodes
            Vector2 nodeDistance = new Vector2(325f, 250f);

            Queue<TreeNodeModel<BehaviorTreeNode>> nodeModels = new Queue<TreeNodeModel<BehaviorTreeNode>>();
            nodeModels.Enqueue(rootModelNode);
            while (nodeModels.Count > 0)
            {
                TreeNodeModel<BehaviorTreeNode> currentNode = nodeModels.Dequeue();
                if(currentNode.Item == null)
                {
                    continue;
                }
                
                //Place the node relative to the root's position
                Vector2 nodePosition = new Vector2((currentNode.X - rootPosition.x) * nodeDistance.x, (currentNode.Y - rootPosition.y) * nodeDistance.y);
                AddNode(
                    windowRect.center + nodePosition,
                    currentNode.Item,
                    currentNode.Item != rootNode,
                    (currentNode.Item is CompositeNode || currentNode.Item is DecoratorNode)
                );
                for (int i = 0; i < currentNode.Children.Count; i++)
                {
                    nodeModels.Enqueue(currentNode.Children[i]);
                }
            }

            //Draw connections. Map the Behavior Tree Node to their editor window counterpart.
            Dictionary<BehaviorTreeNode, BTEditorWindowNode> dict = new Dictionary<BehaviorTreeNode, BTEditorWindowNode>();
            foreach (BTEditorWindowNode n in nodes)
            {
                dict.Add(n.GetBehaviorTreeNode(), n);
            }

            if (connections == null)
            {
                connections = new List<Connection>();
            }

            /*Use the map of node -> editor window node to place the 
             connections using the treemodel we created earlier.*/
            nodeModels.Enqueue(rootModelNode);
            while (nodeModels.Count > 0)
            {
                TreeNodeModel<BehaviorTreeNode> currentNode = nodeModels.Dequeue();
                for (int i = 0; i < currentNode.Children.Count; i++)
                {
                    BehaviorTreeNode childItem = currentNode.Children[i].Item;
                    if (childItem == null)
                    {
                        continue;
                    }
                    connections.Add(new Connection(dict[childItem].inPoint, dict[currentNode.Item].outPoint, RemoveConnection));
                    nodeModels.Enqueue(currentNode.Children[i]);
                }
            }

            MarkDirty(false);
        }

        /// <summary>
        /// Clears the window of all nodes and connections. Used for refreshing the window.
        /// </summary>
        private void Clear()
        {
            nodes = new List<BTEditorWindowNode>();
            connections = new List<Connection>();
            selectedNodes = new List<BTEditorWindowNode>();
        }

        /// <summary>
        /// Generates a model for the Behavior tree that allows the node editor to automatically place nodes in the window. 
        /// </summary>
        private TreeNodeModel<BehaviorTreeNode> CreateTreeModel(BehaviorTreeNode rootNode)
        {
            //Queue of behavior tree nodes
            Queue<TreeNodeModel<BehaviorTreeNode>> nodes = new Queue<TreeNodeModel<BehaviorTreeNode>>();
            TreeNodeModel<BehaviorTreeNode> rootModelNode = new TreeNodeModel<BehaviorTreeNode>(rootNode, null);
            nodes.Enqueue(rootModelNode);
            while (nodes.Count > 0)
            {
                TreeNodeModel<BehaviorTreeNode> currentNode = nodes.Dequeue();
                BehaviorTreeNode nodeItem = currentNode.Item;
                if (nodeItem is CompositeNode)
                {
                    CompositeNode composite = nodeItem as CompositeNode;
                    for (int i = 0; i < composite.children.Count; i++)
                    {
                        //Get the BehaviorTreeNode Child
                        BehaviorTreeNode child = composite.children[i];
                        //Create treenodemodel from this node, with the parent as the current node
                        TreeNodeModel<BehaviorTreeNode> childModel = new TreeNodeModel<BehaviorTreeNode>(child, currentNode);
                        //add it to the current node model
                        currentNode.Children.Add(childModel);
                        //Add it to the queue to continue iteration.
                        nodes.Enqueue(childModel);
                    }
                }
                else if (nodeItem is DecoratorNode)
                {
                    DecoratorNode decorator = nodeItem as DecoratorNode;
                    //Get the BehaviorTreeNode Child
                    BehaviorTreeNode child = decorator.child;
                    //Create treenodemodel from this node, with the parent as the current node
                    TreeNodeModel<BehaviorTreeNode> childModel = new TreeNodeModel<BehaviorTreeNode>(child, currentNode);
                    //add it to the current node model
                    currentNode.Children.Add(childModel);
                    //Add it to the queue to continue iteration.
                    nodes.Enqueue(childModel);
                }
            }

            return rootModelNode;
        }


        private void OnEnable()
        {
            nodeStyle = new GUIStyle();
            nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node0.png") as Texture2D;
            nodeStyle.border = new RectOffset(12, 12, 12, 12);

            selectedNodeStyle = new GUIStyle();
            selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node0 on.png") as Texture2D;
            selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);

            subtreeStyle = new GUIStyle("flow node hex 0");
            selectedSubtreeStyle = new GUIStyle("flow node hex 0 on");

            inPointStyle = new GUIStyle();
            inPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn mid.png") as Texture2D;
            inPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn mid on.png") as Texture2D;
            inPointStyle.border = new RectOffset(4, 4, 12, 12);

            outPointStyle = new GUIStyle();
            outPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn mid.png") as Texture2D;
            outPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn mid on.png") as Texture2D;
            outPointStyle.border = new RectOffset(4, 4, 12, 12);

            selectedNodes = new List<BTEditorWindowNode>();

            m_editorZoomer = new EditorZoomer();
            m_lastSize = position;

            inspector = new BehaviorTreeWindowInspector();
            nodesList = new BehaviorTreeWindowNodesList(this);
            header = new BehaviorTreeWindowHeader(inspector, nodesList, this);
        }
        #endregion

        private void OnGUI()
        {
            header.Render(position, position.width / 4f);
            if (inspector.open)
            {
                EditorGUILayout.BeginHorizontal();
                inspector.Render(selectedNodes, position.width / 4f);
                DrawNodeWindow();
                EditorGUILayout.EndHorizontal();
            }
            else if (nodesList.open)
            {
                EditorGUILayout.BeginHorizontal();
                nodesList.Render(position.width / 4f);
                DrawNodeWindow();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                DrawNodeWindow();
                inspector.open = false;
            }

            if (WindowSizeChanged())
            {
                Repaint();
            }
        }

        #region TreeUtilities
        /// <summary>
        /// Gets a Dictionary of BehaviorTreeNodes and their level in the tree. 
        /// </summary>
        public Dictionary<BehaviorTreeNode, int> BreadthFirstSearch(BehaviorTreeNode rootNode)
        {
            // Mark all the vertices 
            // as not visited 
            HashSet<BehaviorTreeNode> visited = new HashSet<BehaviorTreeNode>();
            Dictionary<BehaviorTreeNode, int> levels = new Dictionary<BehaviorTreeNode, int>();

            // Create a queue for BFS 
            Queue<BehaviorTreeNode> queue = new Queue<BehaviorTreeNode>();

            // Mark the current node as 
            // visited and enqueue it 
            visited.Add(rootNode);
            levels.Add(rootNode, 0);
            queue.Enqueue(rootNode);

            while (queue.Count > 0)
            {
                // Dequeue a vertex from 
                // queue and print it
                BehaviorTreeNode currentNode = queue.Dequeue();

                // Console.Write( s + " " );

                List<BehaviorTreeNode> children = new List<BehaviorTreeNode>();
                if (currentNode is DecoratorNode)
                {
                    DecoratorNode decorator = currentNode as DecoratorNode;
                    if (decorator.child != null)
                    {
                        children.Add(decorator.child);
                    }
                }
                else if (currentNode is CompositeNode)
                {
                    CompositeNode composite = currentNode as CompositeNode;
                    if (composite.children != null)
                    {
                        for (int i = 0; i < composite.children.Count; i++)
                        {
                            if (composite.children[i] == null)
                            {
                                continue;
                            }
                            children.Add(composite.children[i]);
                        }
                    }
                }

                foreach (BehaviorTreeNode val in children)
                {
                    if (!visited.Contains(val))
                    {
                        visited.Add(val);
                        levels.Add(val, levels[currentNode] + 1);
                        queue.Enqueue(val);
                    }
                }
            }

            return levels;
        }

        /// <summary>
        /// Returns a Dictionary with the Level of the tree as a key and the number of nodes in that level as a value. Starts from given node.
        /// </summary>
        private Dictionary<int, int> ItemsInLevel(BehaviorTreeNode rootNode)
        {
            Dictionary<BehaviorTreeNode, int> nodes = BreadthFirstSearch(rootNode);
            return ItemsInLevel(nodes);
        }

        /// <summary>
        /// Returns a Dictionary with the Level of the tree as a key and the number of nodes in that level as a value. Starts from given node.
        /// </summary>
        private Dictionary<int, int> ItemsInLevel(Dictionary<BehaviorTreeNode, int> nodes)
        {
            Dictionary<int, int> itemsInLevel = new Dictionary<int, int>();

            foreach (BehaviorTreeNode currentNode in nodes.Keys)
            {
                int level = nodes[currentNode];
                if (itemsInLevel.TryGetValue(level, out int numItems))
                {
                    itemsInLevel[level] = numItems + 1;
                }
                else
                {
                    itemsInLevel.Add(level, 1);
                }
            }

            return itemsInLevel;
        }
        #endregion

        #region NodeWindowDrawing
        /// <summary>
        /// Draw the section of the editor window that actually has the nodes.
        /// </summary>
        private void DrawNodeWindow()
        {
            m_nodeWindowRect = EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
            m_editorZoomer.Begin();
            float zoom = m_editorZoomer.zoom;

            Event e = Event.current;
            header.ProcessEvents(e);
            if (nodesList.open)
            {
                nodesList.ProcessEvents(e);
            }
            if (inspector.open)
            {
                inspector.ProcessEvents(e);
            }

            //Draw the vertical and horizontal background lines
            DrawGrid(20, 0.2f, Color.gray, zoom);
            DrawGrid(100, 0.4f, Color.gray, zoom);

            /*Draw all the nodes and connections between the nodes. We draw the connections 
              first so that the connecting lines are below the nodes*/
            DrawConnections();
            DrawNodes();
            //Draw the connecting line if we are adding a connection between nodes 
            DrawConnectionLine(e);

            //Process the events on the nodes in the window. If the event is not used, we process events for the rest of the window (such as dragging)
            if (!ProcessNodeEvents(e))
            {
                ProcessEvents(e);
            }

            m_editorZoomer.End();
            EditorGUILayout.EndVertical();

            //Repaint if necessary
            if (GUI.changed) Repaint();
        }

        /// <summary>
        /// Draws the lines in the background of the editor window.
        /// </summary>
        private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor, float zoom)
        {
            int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
            int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

            Handles.BeginGUI();
            Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

            offset += drag * 0.5f;
            Vector3 newOffset = new Vector3(offset.x % gridSpacing, offset.y % gridSpacing, 0);
            int zoomFactor = Mathf.CeilToInt(1 / zoom) + 1;

            for (int i = 0; i < widthDivs * zoomFactor; i++)
            {
                Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset, new Vector3(gridSpacing * i, position.height * zoomFactor, 0f) + newOffset);
            }

            for (int j = 0; j < heightDivs * zoomFactor; j++)
            {
                Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset, new Vector3(position.width * zoomFactor, gridSpacing * j, 0f) + newOffset);
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawNodes()
        {
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Draw();
                }
            }
        }

        private void DrawConnections()
        {
            if (connections != null)
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    connections[i].Draw();
                }
            }
        }

        private void DrawConnectionLine(Event e)
        {
            if (selectedInPoint != null && selectedOutPoint == null)
            {
                Handles.DrawBezier(
                    selectedInPoint.rect.center,
                    e.mousePosition,
                    selectedInPoint.rect.center + Vector2.left * 50f,
                    e.mousePosition - Vector2.left * 50f,
                    Color.white,
                    null,
                    2f
                );

                GUI.changed = true;
            }

            if (selectedOutPoint != null && selectedInPoint == null)
            {
                Handles.DrawBezier(
                    selectedOutPoint.rect.center,
                    e.mousePosition,
                    selectedOutPoint.rect.center - Vector2.left * 50f,
                    e.mousePosition + Vector2.left * 50f,
                    Color.white,
                    null,
                    2f
                );

                GUI.changed = true;
            }
        }
        #endregion

        #region EventProcessing
        private void ProcessEvents(Event e)
        {
            if (e.type == EventType.Used)
            {
                return;
            }
            drag = Vector2.zero;

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        ClearConnectionSelection();
                        if (inspector.GetRect().Contains(e.mousePosition))
                        {
                            GUI.changed = true;
                            break;
                        }
                    }
                    if (e.button == 1)
                    {
                        ProcessContextMenu(e.mousePosition);
                    }
                    break;
                case EventType.MouseDrag:
                    if (e.button == 0)
                    {
                        OnDrag(e.delta);
                    }
                    break;
                case EventType.KeyDown:
                    if (e.keyCode == KeyCode.Delete)
                    {
                        for (int i = 0; i < selectedNodes.Count; i++)
                        {
                            if (selectedNodes[i].GetBehaviorTreeNode() == root)
                            {
                                continue;
                            }
                            RemoveNode(selectedNodes[i]);
                        }
                        selectedNodes.Clear();
                        GUI.changed = true;
                    }
                    break;
            }
        }

        private bool ProcessNodeEvents(Event e)
        {
            if (nodes != null)
            {
                if (NodeRectContainsMouseEvent(e, out BTEditorWindowNode node))
                {
                    switch (e.type)
                    {
                        case EventType.MouseDown:
                            if (e.button == 0)
                            {
                                if (!selectedNodes.Contains(node))
                                {
                                    if (!e.shift)
                                    {
                                        ClearSelectedNodes();
                                    }
                                    node.isDragged = true;
                                    node.isSelected = true;
                                    node.style = node.selectedNodeStyle;
                                    node.selected = true;
                                    if (!selectedNodes.Contains(node))
                                    {
                                        selectedNodes.Add(node);
                                    }
                                }

                                if (node.titleRect.Contains(e.mousePosition))
                                {
                                    node.titleSelected = true;
                                }
                                else
                                {
                                    node.titleSelected = false;
                                }

                                GUI.changed = true;
                                e.Use();
                                return true;
                            }
                            break;

                        case EventType.MouseUp:
                            foreach (BTEditorWindowNode n in selectedNodes)
                            {
                                n.isDragged = false;
                            }
                            break;

                        case EventType.MouseDrag:
                            foreach (BTEditorWindowNode n in selectedNodes)
                            {
                                n.Drag(e.delta);
                            }
                            GUI.changed = true;
                            e.Use();
                            return true;

                    }
                }
                else if (e.type == EventType.MouseDown && e.button == 0 && !e.shift)
                {
                    ClearSelectedNodes();
                    GUI.changed = true;
                }
            }

            if (e.isKey && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Escape))
            {
                foreach (BTEditorWindowNode node in selectedNodes)
                {
                    node.titleSelected = false;
                }
                GUI.changed = true;
                return true;
            }

            return false;
        }

        private bool NodeRectContainsMouseEvent(Event e, out BTEditorWindowNode node)
        {
            foreach (BTEditorWindowNode n in nodes)
            {
                if (n.rect.Contains(e.mousePosition))
                {
                    node = n;
                    return true;
                }
            }

            node = null;
            return false;
        }

        private void ProcessContextMenu(Vector2 mousePosition)
        {

            if (inspector.open || nodesList.open)
            {
                mousePosition.x -= 150f;
            }

            //Should we add a separator to the genericMenu?
            bool separator = false;
            GenericMenu genericMenu = new GenericMenu();

            //If we are hovered over a Node, add an option to delete it. If we have selected nodes, the option to delete will delete all selected nodes. 
            if (NodeRectContainsMouseEvent(Event.current, out BTEditorWindowNode node))
            {
                //If we have multiple selected nodes, we will remove all of them, otherwise, remove the node containing the mouse pos
                if (selectedNodes != null && selectedNodes.Contains(node))
                {
                    string itemText = $"Remove Node{(selectedNodes.Count > 1 ? "s" : "")}";
                    GenericMenu.MenuFunction func = (selectedNodes.Count > 1 ? () => {
                        List<BTEditorWindowNode> currentlySelectedNodes = new List<BTEditorWindowNode>(selectedNodes);
                        foreach (BTEditorWindowNode n in currentlySelectedNodes)
                        {
                            RemoveNode(n);
                        }
                    }
                    : () => RemoveNode(node));
                    genericMenu.AddItem(new GUIContent(itemText), false, func);
                }
                else
                {
                    genericMenu.AddItem(new GUIContent("Remove Node"), false, () => RemoveNode(node));
                }
                separator = true;
            }

            //If we have selected nodes, add the option to copy the nodes to the clipboard.
            if (selectedNodes != null && selectedNodes.Count != 0)
            {
                genericMenu.AddItem(new GUIContent($"Copy Node{(selectedNodes.Count > 1 ? "s" : "")}"), false, () => {
                    if (clipboard == null)
                    {
                        clipboard = new List<BTEditorWindowNode>();
                    }
                    clipboard.Clear();
                    foreach (BTEditorWindowNode node in selectedNodes)
                    {
                        clipboard.Add(node);
                    }
                    separator = true;
                });
            }

            //If our clipboard is not empty, add the option to paste the nodes from the clipboard
            if (clipboard != null && clipboard.Count > 0)
            {
                genericMenu.AddItem(new GUIContent("Paste Nodes"), false, () => PasteNodes(mousePosition));
                separator = true;
            }

            //If any of the above options were true, add a separator to the menu.
            if (separator)
            {
                genericMenu.AddSeparator("");
            }

            genericMenu.AddItem(new GUIContent("Create New Node"), false, () => {
                NodeSearchWindow nodeWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
                nodeWindow.onSetIndexCallback = (BehaviorTreeNode node) =>
                {
                    AddNode(mousePosition, node, true);

                    Event.current.Use();
                    Event.current.type = EventType.Used;
                    drag = Vector2.zero;
                };

                SearchWindow.Open(new SearchWindowContext(mousePosition), nodeWindow);
            });

            genericMenu.ShowAsContext();
        }

        private void OnDrag(Vector2 delta)
        {
            drag = delta;

            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    nodes[i].Drag(delta);
                }
            }

            GUI.changed = true;
        }
        #endregion

        #region NodeManagement
        public BTEditorWindowNode AddNode(Vector2 position, BehaviorTreeNode node, bool hasInPort)
        {
            bool hasOutPort = false;
            if (node is CompositeNode || node is DecoratorNode)
            {
                hasOutPort = true;
            }

            return AddNode(position, node, true, hasOutPort);
        }

        public BTEditorWindowNode AddNode(Vector2 position, BehaviorTreeNode behaviorTreeNode, bool hasInPort, bool hasOutPort)
        {
            if (behaviorTreeNode.name == string.Empty)
            {
                behaviorTreeNode.name = BTEditorWindowNode.GetNodeTitle(behaviorTreeNode);
            }

            if (nodes == null)
            {
                nodes = new List<BTEditorWindowNode>();
            }

            GUIStyle defaultStyle = behaviorTreeNode is not SubTreeNode ? nodeStyle : subtreeStyle;
            GUIStyle selectedStyle = behaviorTreeNode is not SubTreeNode ? selectedNodeStyle : selectedSubtreeStyle;

            BTEditorWindowNode node = new BTEditorWindowNode(
                behaviorTreeNode, position, new Vector2(200, 50), 
                defaultStyle, selectedStyle, inPointStyle, outPointStyle, 
                OnClickInPoint, OnClickOutPoint, RemoveNode
                );
            if (!hasInPort)
            {
                node.RemovePort(ConnectionPointType.In);
            }

            if (!hasOutPort)
            {
                node.RemovePort(ConnectionPointType.Out);
            }

            if (behaviorTreeNode != root)
            {
                MarkDirty(true);
            }
            nodes.Add(node);
            return node;
        }

        private void RemoveNode(BTEditorWindowNode node)
        {
            if (node.GetBehaviorTreeNode() == root)
            {
                Debug.LogWarning("Cannot Delete Root Node.");
                return;
            }

            if (connections != null)
            {
                List<Connection> connectionsToRemove = new List<Connection>();

                for (int i = 0; i < connections.Count; i++)
                {
                    if (connections[i].inPoint == node.inPoint || connections[i].outPoint == node.outPoint)
                    {
                        connectionsToRemove.Add(connections[i]);
                    }
                }

                for (int i = 0; i < connectionsToRemove.Count; i++)
                {
                    RemoveConnection(connectionsToRemove[i]);
                }
                connectionsToRemove = null;
                MarkDirty(true);
            }

            nodes.Remove(node);

        }

        private void ClearSelectedNodes()
        {
            foreach (BTEditorWindowNode node in selectedNodes)
            {
                node.isSelected = false;
                node.style = node.defaultNodeStyle;
                node.selected = false;
            }

            selectedNodes.Clear();
        }

        private void PasteNodes(Vector2 mousePosition)
        {
            //Get the center position of the nodes.
            Vector2 center = Vector2.zero;
            foreach (BTEditorWindowNode node in clipboard)
            {
                center += node.rect.center;
            }
            center /= clipboard.Count;

            //Maps the original node to the new node
            Dictionary<BTEditorWindowNode, BTEditorWindowNode> clipboardMap = new Dictionary<BTEditorWindowNode, BTEditorWindowNode>();
            //Map all the Node's connection points to the node.
            Dictionary<ConnectionPoint, BTEditorWindowNode> inPoints = new Dictionary<ConnectionPoint, BTEditorWindowNode>();
            Dictionary<ConnectionPoint, BTEditorWindowNode> outPoints = new Dictionary<ConnectionPoint, BTEditorWindowNode>();


            foreach (BTEditorWindowNode node in clipboard)
            {
                //Add the node's connection points to the maps
                if (node.inPoint != null)
                {
                    inPoints.Add(node.inPoint, node);
                }

                if (node.outPoint != null)
                {
                    outPoints.Add(node.outPoint, node);
                }


                //Position to place the node
                Vector2 positionToPlaceNode = mousePosition + (node.rect.center - center);
                BehaviorTreeNode newNode = node.GetBehaviorTreeNode().Clone();
                //Clear the children nodes of the new nodes
                switch (newNode)
                {
                    case DecoratorNode decorator:
                        decorator.child = null;
                        break;
                    case CompositeNode composite:
                        composite.children = new List<BehaviorTreeNode>();
                        break;
                    default:
                        break;
                }
                //Map the original Node to the newly added Node
                clipboardMap.Add(node, AddNode(positionToPlaceNode, newNode, node.GetBehaviorTreeNode() != root));
            }

            //We iterate through the existing connections, and if there are connections between the nodes in
            //the clipboard, we paste those connections between the corresponding pasted nodes.
            List<Connection> currentConnections = new List<Connection>(connections);
            foreach (Connection connection in currentConnections)
            {
                if (inPoints.ContainsKey(connection.inPoint) && outPoints.ContainsKey(connection.outPoint))
                {
                    //Use the connection points to get the original nodes in the clipboard
                    inPoints.TryGetValue(connection.inPoint, out BTEditorWindowNode originalInNode);
                    outPoints.TryGetValue(connection.outPoint, out BTEditorWindowNode originalOutNode);
                    //Use the original Nodes to get the Nodes we pasted into the editor
                    clipboardMap.TryGetValue(originalInNode, out BTEditorWindowNode newInNode);
                    clipboardMap.TryGetValue(originalOutNode, out BTEditorWindowNode newOutNode);
                    //Draw connections between those two nodes.
                    selectedInPoint = newInNode.inPoint;
                    selectedOutPoint = newOutNode.outPoint;
                    CreateConnection();
                }
            }

            //Clear the current selected nodes and add the newly pasted ones
            if (selectedNodes == null)
            {
                selectedNodes = new List<BTEditorWindowNode>();
            }
            selectedNodes.Clear();
            foreach (BTEditorWindowNode keyNode in clipboardMap.Keys)
            {
                keyNode.isDragged = false;
                keyNode.selected = false;
                keyNode.style = keyNode.defaultNodeStyle;

                clipboardMap.TryGetValue(keyNode, out BTEditorWindowNode node);
                node.isDragged = true;
                node.selected = true;
                node.style = node.selectedNodeStyle;
                selectedNodes.Add(node);
            }
            GUI.changed = true;
        }
        #endregion

        #region ConnectionManagement
        private void OnClickInPoint(ConnectionPoint inPoint)
        {
            selectedInPoint = inPoint;

            if (selectedOutPoint != null)
            {
                if (selectedOutPoint.node != selectedInPoint.node)
                {
                    CreateConnection();
                    ClearConnectionSelection();
                }
                else
                {
                    ClearConnectionSelection();
                }
            }
        }

        private void OnClickOutPoint(ConnectionPoint outPoint)
        {
            selectedOutPoint = outPoint;

            if (selectedInPoint != null)
            {
                if (selectedOutPoint.node != selectedInPoint.node)
                {
                    CreateConnection();
                    ClearConnectionSelection();
                }
                else
                {
                    ClearConnectionSelection();
                }
            }
        }

        private void CreateConnection()
        {
            if (connections == null)
            {
                connections = new List<Connection>();
            }

            //Guard against creating a connection between a UtilityDecorator/ Composite and non UtilityEvaluator nodes
            BehaviorTreeNode outNode = selectedOutPoint.node.GetBehaviorTreeNode();
            if ((outNode is UtilityDecorator || outNode is UtilityComposite) && !(selectedInPoint.node.GetBehaviorTreeNode() is UtilityEvaluator))
            {
                Debug.LogWarning($"Cannot create a connection between node of type {outNode.GetType()} and Non-UtilityEvaluator node of type {selectedInPoint.node.GetBehaviorTreeNode().GetType()}");
                return;
            }
            //Prevent a node from having multiple parents.
            foreach (Connection connection in connections)
            {
                if (connection.inPoint == selectedInPoint)
                {
                    string inNodeName = selectedInPoint.node.GetBehaviorTreeNode().name;
                    Debug.LogWarning($"Cannot create a connection between node {outNode.name} and node {inNodeName}: {inNodeName} already has a parent.");
                    return;
                }
            }


            connections.Add(new Connection(selectedInPoint, selectedOutPoint, RemoveConnection));

            BehaviorTreeNode btreeNode = selectedOutPoint.node.GetBehaviorTreeNode();
            BehaviorTreeNode outPointNode = selectedInPoint.node.GetBehaviorTreeNode();
            switch (btreeNode)
            {
                case CompositeNode composite:
                    if (composite.children == null)
                    {
                        composite.children = new List<BehaviorTreeNode>();
                    }

                    if (!composite.children.Contains(outPointNode))
                    {
                        composite.children.Add(outPointNode);
                    }
                    selectedOutPoint.node.SaveNode();
                    MarkDirty(true);
                    return;
                case DecoratorNode decorator:
                    if (decorator.child != outPointNode)
                    {
                        decorator.child = outPointNode;
                    }
                    selectedOutPoint.node.SaveNode();
                    MarkDirty(true);
                    return;
                default:
                    Debug.LogError($"Parent node {btreeNode.GetType().ToString()} is not a Behavior tree node type with children.");
                    return;
            }

        }
        
        private void RemoveConnection(Connection connection)
        {
            BehaviorTreeNode btreeNode = connection.outPoint.node.GetBehaviorTreeNode();
            BehaviorTreeNode outPointNode = connection.inPoint.node.GetBehaviorTreeNode();
            switch (btreeNode)
            {
                case CompositeNode composite:
                    if (composite.children == null)
                    {
                        composite.children = new List<BehaviorTreeNode>();
                    }
                    composite.children.Remove(outPointNode);
                    connection.outPoint.node.SaveNode();
                    MarkDirty(true);
                    break;
                case DecoratorNode decorator:
                    decorator.child = null;
                    connection.outPoint.node.SaveNode();
                    MarkDirty(true);
                    break; ;
                default:
                    Debug.LogError($"Parent node {btreeNode.GetType().ToString()} is not a Behavior tree node type with children.");
                    break;
            }




            connections.Remove(connection);
        }

        private void ClearConnectionSelection()
        {
            selectedInPoint = null;
            selectedOutPoint = null;
        }
        #endregion

        #region General
        /// <summary>
        /// Saves all of the changes made to the behavior tree in the Node Editor.
        /// </summary>
        public void Save()
        {
            //Get the directory of the existing asset
            string dir = assetPath.Substring(0, assetPath.LastIndexOf('/') + 1);

            /*Copy the node to the asset, remove all the existing sub assets, and then generate the tree by adding 
            tree nodes as sub assets. */
            EditorUtility.CopySerialized(root, AssetDatabase.LoadAssetAtPath<BehaviorTreeNode>(dir + root.name + ".asset"));
            RemoveAllSubAssets(root);
            BehaviorTreeNode newRoot = AssetDatabase.LoadAssetAtPath<BehaviorTreeNode>(dir + root.name + ".asset");
            BehaviorTreeEditorUtilities.GenerateTreeFromNode(newRoot);
            //Reopen this window with the newly created/ saved node.
            BehaviorTreeEditorWindow.OpenWindow(newRoot);
            MarkDirty(false);

            // Save the changes to assetdatabase
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        private static void RemoveAllSubAssets(Object selectedObject)
        {
            if (selectedObject != null)
            {
                // Extract all sub-assets
                Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(selectedObject));

                // Remove each sub-asset
                foreach (Object subAsset in subAssets)
                {
                    DestroyImmediate(subAsset, true);
                }
            }
        }

        private void MarkDirty(bool dirty)
        {
            this.dirty = dirty;
            string title = root == null ? "Node Based Editor" : root.name;
            title += (dirty ? "*" : "");
            titleContent = new GUIContent(title);
        }

        private bool WindowSizeChanged()
        {
            if (position != m_lastSize)
            {
                m_lastSize = position;
                return true;
            }
            m_lastSize = position;
            return false;
        }

        public Rect GetNodeWindowRect()
        {
            return m_nodeWindowRect;
        }
        #endregion
    }
}
