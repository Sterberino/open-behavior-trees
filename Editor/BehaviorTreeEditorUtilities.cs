using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using System;
using System.Linq;

namespace OpenBehaviorTrees
{
    public static class BehaviorTreeEditorUtilities
    {
        /// <summary>
        /// Gets All of the Behavior tree node types that are placeable in the tree editor. By default it excludes root nodes and Subtrees.
        /// </summary>
        public static List<BehaviorTreeNode> GetAllNodeTypes(List<Type> excludedTypes = null)
        {
            List<Type> excludeTypes = excludedTypes != null ? excludedTypes : new List<Type>() { typeof(BehaviorTreeRootNode), typeof(SubTreeNode)};

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(BehaviorTreeNode)) && !type.IsAbstract && !excludeTypes.Contains(type))
                .Select(type => ScriptableObject.CreateInstance(type) as BehaviorTreeNode).ToList();
        }

        /// <summary>
        /// Gets a list of all Behavior Tree Node assets in the current project. Used to get placeable subtrees in the node editor. 
        /// </summary>
        public static List<BehaviorTreeNode> FindAllBehaviorTreeNodeAssets()
        {
            List<BehaviorTreeNode> behaviorTreeNodeAssets = new List<BehaviorTreeNode>();

            // Find all assets of type MyType
            string[] guids = AssetDatabase.FindAssets("t:BehaviorTreeNode");

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                BehaviorTreeNode myTypeAsset = AssetDatabase.LoadAssetAtPath<BehaviorTreeNode>(assetPath);

                if (myTypeAsset != null)
                {
                    behaviorTreeNodeAssets.Add(myTypeAsset);
                }
            }

            return behaviorTreeNodeAssets;
        }


        /// <summary>
        /// Opens a SaveFilePanel prompting a user to create a new Behavior Tree Root Node asset. Returns null if an invalid path is provided.
        /// </summary>
        public static BehaviorTreeRootNode CreateNewAsset()
        {
            string path = EditorUtility.SaveFilePanel(
                "Create New Asset",
                "Assets",
                "NewAsset",
                "asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                string assetPath = "Assets" + path.Substring(Application.dataPath.Length);
                // Ensure that the selected folder is a valid Unity project folder
                if (!AssetDatabase.IsValidFolder(System.IO.Path.GetDirectoryName(assetPath)))
                {
                    EditorUtility.DisplayDialog(
                        "Invalid Folder",
                        "Please select a valid Unity project folder.",
                        "OK"
                    );
                    return null;
                }

                // Check if the file already exists
                if (System.IO.File.Exists(path))
                {
                    bool overwrite = EditorUtility.DisplayDialog(
                        "Overwrite Existing File?",
                        "A file already exists at the selected location. Do you want to overwrite it?",
                        "Yes",
                        "No"
                    );

                    if (!overwrite)
                    {
                        return null; // User chose not to overwrite
                    }
                }

                // Create a new ScriptableObject asset
                BehaviorTreeRootNode newRoot = ScriptableObject.CreateInstance<BehaviorTreeRootNode>();
                AssetDatabase.CreateAsset(newRoot, "Assets" + path.Substring(Application.dataPath.Length));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return newRoot;
            }

            return null;
        }

        /// <summary>
        /// Given a Behavior tree node asset, it appends all nodes in the tree as sub-assets. Deletes any existing sub-assets that are not part of the tree.
        /// </summary>
        public static void GenerateTreeFromNode(BehaviorTreeNode node)
        {
            Undo.RecordObject(node, "Generate Tree");
            GenerateAssetCopy(node, null, new Queue<int>());
            DeleteNodesNotInTree(node);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(node));
            EditorUtility.SetDirty(node);
        }


        //Remove from the asset any nodes that aren't connected.
        private static void DeleteNodesNotInTree(BehaviorTreeNode node)
        {
            List<BehaviorTreeNode> nodesInTree = node.GetAllChildren();

            string path = AssetDatabase.GetAssetPath(node);
            UnityEngine.Object[] ob = AssetDatabase.LoadAllAssetsAtPath(path);
            List<BehaviorTreeNode> obs = new List<BehaviorTreeNode>();

            for (int i = 0; i < ob.Length; i++)
            {
                if ((BehaviorTreeNode)ob[i] == (BehaviorTreeNode)node)
                {
                    continue;
                }

                obs.Add((BehaviorTreeNode)ob[i]);
            }


            for (int i = 0; i < obs.Count; i++)
            {
                if (!nodesInTree.Contains(obs[i]))
                {
                    Editor.DestroyImmediate(obs[i], true);
                }
            }
        }


        private static BehaviorTreeNode GenerateAssetCopy(BehaviorTreeNode currentNode, BehaviorTreeNode parent, Queue<int> order)
        {
            Queue<int> tempQueue = CopyQueue(order);

            bool root = parent == null;
            string path = AssetDatabase.GetAssetPath(currentNode);

            //Get a reference to all old assets at path
            UnityEngine.Object[] ob = AssetDatabase.LoadAllAssetsAtPath(path);
            List<UnityEngine.Object> obs = new List<UnityEngine.Object>(ob);

            for (int i = 0; i < ob.Length; i++)
            {

                if ((BehaviorTreeNode)ob[i] == (BehaviorTreeNode)currentNode)
                {
                    //Debug.Log(((BehaviorTreeNode)ob[i]).name);
                    continue;
                }

                obs.Add(ob[i]);
            }

            switch (currentNode)
            {
                case CompositeNode compositeNode:
                    var newCompositeNode = ScriptableObject.CreateInstance(compositeNode.GetType()) as CompositeNode;
                    EditorUtility.CopySerialized(compositeNode, newCompositeNode);

                    if ((!root && !obs.Contains(compositeNode)) || (parent != null && path != AssetDatabase.GetAssetPath(parent)))
                    {
                        AssetDatabase.AddObjectToAsset(newCompositeNode, parent);
                        //newCompositeNode.name = "(" + GenerateOrderString(CopyQueue(tempQueue)) + ") " + newCompositeNode.name;
                        for (int i = 0; i < newCompositeNode.children.Count; i++)
                        {
                            Queue<int> tempQueueI = CopyQueue(tempQueue);
                            tempQueueI.Enqueue(i + 1);
                            BehaviorTreeNode child = GenerateAssetCopy(newCompositeNode.children[i], newCompositeNode, tempQueueI);
                            newCompositeNode.children[i] = child;
                        }
                        EditorUtility.SetDirty(parent);
                        return newCompositeNode;
                    }
                    else
                    {
                        for (int i = 0; i < compositeNode.children.Count; i++)
                        {
                            Queue<int> tempQueueI = CopyQueue(tempQueue);
                            tempQueueI.Enqueue(i + 1);
                            BehaviorTreeNode child = GenerateAssetCopy(compositeNode.children[i], compositeNode, tempQueueI);
                            compositeNode.children[i] = child;
                        }

                        EditorUtility.SetDirty(compositeNode);
                        return compositeNode;
                    }
                case DecoratorNode decoratorNode:
                    var newDecoratorNode = ScriptableObject.CreateInstance(decoratorNode.GetType()) as DecoratorNode;
                    EditorUtility.CopySerialized(decoratorNode, newDecoratorNode);

                    if ((!root && !obs.Contains(decoratorNode)) || (parent != null && path != AssetDatabase.GetAssetPath(parent)))
                    {
                        AssetDatabase.AddObjectToAsset(newDecoratorNode, parent);
                        //newDecoratorNode.name = ("(" + GenerateOrderString(CopyQueue(tempQueue)) + ") " + newDecoratorNode.name);
                        EditorUtility.SetDirty(parent);

                        Queue<int> arg = CopyQueue(tempQueue);
                        arg.Enqueue(1);
                        BehaviorTreeNode child = GenerateAssetCopy(newDecoratorNode.child, newDecoratorNode, arg);
                        newDecoratorNode.child = child;

                        return newDecoratorNode;
                    }
                    else
                    {
                        Queue<int> arg = CopyQueue(tempQueue);
                        arg.Enqueue(1);
                        BehaviorTreeNode child = GenerateAssetCopy(decoratorNode.child, decoratorNode, arg);
                        decoratorNode.child = child;

                        EditorUtility.SetDirty(decoratorNode);
                        return decoratorNode;
                    }
                default:
                    var newNode = ScriptableObject.CreateInstance(currentNode.GetType()) as BehaviorTreeNode;
                    EditorUtility.CopySerialized(currentNode, newNode);

                    if ((!root && !obs.Contains(currentNode)) || (parent != null && path != AssetDatabase.GetAssetPath(parent)))
                    {
                        AssetDatabase.AddObjectToAsset(newNode, parent);
                        //newNode.name = ("(" + GenerateOrderString(tempQueue) + ") " + newNode.name);
                        EditorUtility.SetDirty(parent);
                        return newNode;
                    }
                    else
                    {
                        EditorUtility.SetDirty(currentNode);
                        return currentNode;
                    }
            }
        }

        private static Queue<int> CopyQueue(Queue<int> order)
        {
            Queue<int> tempQueue = new Queue<int>();
            foreach (int i in order)
            {
                tempQueue.Enqueue(i);
            }

            return tempQueue;
        }

        //Unused / depracated. Nodes were named with a number sequence such that sub-assets were listed in a directory manner (1-2-2-1), etc
        private static string GenerateOrderString(Queue<int> order)
        {
            string result = "";

            result += order.Dequeue().ToString();
            while (order.Count > 0)
            {
                result += ("-" + order.Dequeue().ToString());
            }

            return result;
        }


    }
}

