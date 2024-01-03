using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.Text.RegularExpressions;

namespace OpenBehaviorTrees
{
	public class BehaviorTreeWindowNodesList
	{
		private BehaviorTreeEditorWindow m_nodeEditor;
		public bool open { get; set; }
		private Rect m_rect;

		private GUIStyle buttonStyle;
		private GUIStyle searchbarStyle;
		private List<BehaviorTreeNode> allNodeTypes;

		private List<BehaviorTreeNode> decorators;
		private List<BehaviorTreeNode> composites;
		private List<BehaviorTreeNode> conditions;
		private List<BehaviorTreeNode> tasks;
		private List<BehaviorTreeNode> subtrees;

		private List<BehaviorTreeNode> leafs; //TODO: Separate into Condition checking nodes and action nodes.

		private bool m_decoratorsOpen = false;
		private bool m_compositesOpen = false;
		private bool m_leafsOpen = false;
		private bool m_tasksOpen = false;
		private bool m_conditionsOpen = false;
		private bool m_subtreesOpen = false;

		private Vector2 scrollPosition;
		private string searchTerm;

		public BehaviorTreeWindowNodesList(BehaviorTreeEditorWindow nodeEditor)
		{
			buttonStyle = new GUIStyle("AppToolbarButtonMid");
			buttonStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn mid.png") as Texture2D;
			buttonStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn mid on.png") as Texture2D;
			allNodeTypes = BehaviorTreeEditorUtilities.GetAllNodeTypes();

			searchbarStyle = new GUIStyle("ToolbarSearchTextField");

			leafs = new List<BehaviorTreeNode>();
			decorators = new List<BehaviorTreeNode>();
			composites = new List<BehaviorTreeNode>();
			tasks = new List<BehaviorTreeNode>();
			conditions = new List<BehaviorTreeNode>();
			subtrees = BehaviorTreeEditorUtilities.FindAllBehaviorTreeNodeAssets();

			searchTerm = "";

			foreach (BehaviorTreeNode node in allNodeTypes)
			{
				if (node.GetType().IsAbstract)
				{
					continue;
				}

				switch (node)
				{
					case DecoratorNode:
						Type decType = node.GetType();
						if (decType.IsSubclassOf(typeof(DecoratorNode)))
						{
							decorators.Add(node);
						}
						break;
					case CompositeNode:
						Type compType = node.GetType();
						if (compType.IsSubclassOf(typeof(CompositeNode)))
						{
							composites.Add(node);
						}
						break;
					case TaskNode:
						Type taskType = node.GetType();
						if (taskType.IsSubclassOf(typeof(TaskNode)))
						{
							tasks.Add(node);
						}
						break;
					case ConditionNode:
						Type conType = node.GetType();
						if (conType.IsSubclassOf(typeof(ConditionNode)))
						{
							conditions.Add(node);
						}
						break;
					default:
						if (!(node is ConditionNode) && !(node is TaskNode))
						{
							leafs.Add(node);
						}
						break;
				}
			}

			scrollPosition = Vector2.zero;
			this.m_nodeEditor = nodeEditor;
		}

		public void Render(float windowWidth)
		{
			m_rect = EditorGUILayout.BeginVertical("box", GUILayout.Width(windowWidth), GUILayout.MinWidth(300), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
			searchTerm = EditorGUILayout.TextField(searchTerm, searchbarStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight));

			List<BehaviorTreeNode> filteredDecorators = searchTerm == "" ? decorators : decorators.Where(x => Match(x)).ToList();
			List<BehaviorTreeNode> filteredLeafs = searchTerm == "" ? leafs : leafs.Where(x => Match(x)).ToList();
			List<BehaviorTreeNode> filteredComposites = searchTerm == "" ? composites : composites.Where(x => Match(x)).ToList();
			List<BehaviorTreeNode> filteredConditions = searchTerm == "" ? conditions : conditions.Where(x => Match(x)).ToList();
			List<BehaviorTreeNode> filteredTasks = searchTerm == "" ? tasks : tasks.Where(x => Match(x)).ToList();
			List<BehaviorTreeNode> filteredSubtrees = searchTerm == "" ? subtrees : subtrees.Where(x => Match(x)).ToList();

			if (filteredComposites.Count > 0)
			{
				m_compositesOpen = EditorGUILayout.Foldout(m_compositesOpen, "Composites");
				if (m_compositesOpen)
				{
					filteredComposites.ForEach(x => CreateItem(x));
				}
			}

			if (filteredDecorators.Count > 0)
			{
				m_decoratorsOpen = EditorGUILayout.Foldout(m_decoratorsOpen, "Decorators");
				if (m_decoratorsOpen)
				{
					filteredDecorators.ForEach(x => CreateItem(x));
				}
			}

			if (filteredTasks.Count > 0)
			{
				m_tasksOpen = EditorGUILayout.Foldout(m_tasksOpen, "Tasks");
				if (m_tasksOpen)
				{
					filteredTasks.ForEach(x => CreateItem(x));
				}
			}

			if (filteredConditions.Count > 0)
			{
				m_conditionsOpen = EditorGUILayout.Foldout(m_conditionsOpen, "Conditions");
				if (m_conditionsOpen)
				{
					filteredConditions.ForEach(x => CreateItem(x));
				}
			}

			if (filteredLeafs.Count > 0)
			{
				m_leafsOpen = EditorGUILayout.Foldout(m_leafsOpen, "Leafs");
				if (m_leafsOpen)
				{
					filteredLeafs.ForEach(x => CreateItem(x));
				}
			}

			if(filteredSubtrees.Count > 0)
            {
				GUIContent subtreeContent = new GUIContent("Subtrees");
				subtreeContent.tooltip = "A list of all the tree assets in the project.";
				m_subtreesOpen = EditorGUILayout.Foldout(m_subtreesOpen, subtreeContent);
				if (m_subtreesOpen)
				{
					filteredSubtrees.ForEach(x => CreateItem(x, true));
				}
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}

		bool Match(BehaviorTreeNode node)
		{
			string pattern = $@"\b{searchTerm}\w*\b";
			string input = BTEditorWindowNode.GetNodeTitle(node);
			Match m = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
			if (m.Success)
			{
				return true;
			}
			return false;

		}

		public void CreateItem(BehaviorTreeNode node, bool subTree = false)
		{
			bool buttonPressed = GUILayout.Button(BTEditorWindowNode.GetNodeTitle(node), buttonStyle);
			if (buttonPressed)
			{
				bool hasOutPort = false;
				if (!subTree && (node is CompositeNode || node is DecoratorNode))
				{
					hasOutPort = true;
				}

				Rect rect = m_nodeEditor.GetNodeWindowRect();

				if(subTree)
                {
					SubTreeNode addition = ScriptableObject.CreateInstance<SubTreeNode>();
					addition.subtree = (BehaviorTreeRootNode)node;
					m_nodeEditor.AddNode(new Vector2(rect.x + 100, rect.y + 100), addition, true, hasOutPort);
				}
				else
                {
					m_nodeEditor.AddNode(new Vector2(rect.x + 100, rect.y + 100), node, true, hasOutPort);
				}

			}
		}


		

		public void ProcessEvents(Event e)
		{

		}

	}

}
