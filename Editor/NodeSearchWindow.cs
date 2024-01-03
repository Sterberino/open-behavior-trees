using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using System;
using System.Linq;

namespace OpenBehaviorTrees
{
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        public Action<BehaviorTreeNode> onSetIndexCallback;

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            onSetIndexCallback?.Invoke((BehaviorTreeNode)searchTreeEntry.userData);
            return true;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchList = new List<SearchTreeEntry>();

            List<SearchTreeEntry> compositesList = new List<SearchTreeEntry>();
            List<SearchTreeEntry> decoratorsList = new List<SearchTreeEntry>();
            List<SearchTreeEntry> leavesList = new List<SearchTreeEntry>();
            //List<SearchTreeEntry> decoratorsList = new List<SearchTreeEntry>()

            searchList.Add(new SearchTreeGroupEntry(new GUIContent("Create Node"), 0));

            compositesList.Add(new SearchTreeGroupEntry(new GUIContent("Composites"), 1));
            decoratorsList.Add(new SearchTreeGroupEntry(new GUIContent("Decorators"), 1));
            leavesList.Add(new SearchTreeGroupEntry(new GUIContent("Leaves"), 1));

            List<BehaviorTreeNode> nodes = BehaviorTreeEditorUtilities.GetAllNodeTypes();
            foreach (BehaviorTreeNode node in nodes)
            {
                if (node.GetType().IsAbstract)
                {
                    continue;
                }

                SearchTreeEntry entry;
                switch (node)
                {
                    case DecoratorNode decorator:
                        if (node.GetType().IsSubclassOf(typeof(DecoratorNode)))
                        {
                            entry = new SearchTreeEntry(new GUIContent(BTEditorWindowNode.GetNodeTitle(node)));
                            entry.level = 2;
                            entry.userData = decorator;
                            decoratorsList.Add(entry);

                        }
                        break;
                    case CompositeNode composite:
                        if (node.GetType().IsSubclassOf(typeof(CompositeNode)))
                        {
                            entry = new SearchTreeEntry(new GUIContent(BTEditorWindowNode.GetNodeTitle(node)));
                            entry.level = 2;
                            entry.userData = composite;
                            compositesList.Add(entry);
                        }
                        break;
                    default:

                        entry = new SearchTreeEntry(new GUIContent(BTEditorWindowNode.GetNodeTitle(node)));
                        entry.level = 2;
                        entry.userData = node;
                        leavesList.Add(entry);

                        break;
                }
            }

            for (int i = 0; i < compositesList.Count; i++)
            {
                searchList.Add(compositesList[i]);
            }
            for (int i = 0; i < decoratorsList.Count; i++)
            {
                searchList.Add(decoratorsList[i]);
            }
            for (int i = 0; i < leavesList.Count; i++)
            {
                searchList.Add(leavesList[i]);
            }

            return searchList;
        }
    }

}
