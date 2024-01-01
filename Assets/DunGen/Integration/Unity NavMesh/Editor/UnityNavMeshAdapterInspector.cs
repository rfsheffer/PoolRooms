// Only available in Unity 5.6 or higher
#if !(UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5)
using DunGen.Editor;
using System.Collections.Generic;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace DunGen.Adapters
{
	[CustomEditor(typeof(UnityNavMeshAdapter))]
	public class UnityNavMeshAdapterInspector : UnityEditor.Editor
	{
		#region Constants

		private static readonly GUIContent bakeModeLabel = new GUIContent("Runtime Bake Mode", "Determine what to do as the runtime baking process");
		private static readonly GUIContent layerMaskLabel = new GUIContent("Layer Mask", "Objects on these layers will be considered when generating the navmesh. This setting will NOT override the layer mask of any existing nav mesh surface, it will only apply to any new surfaces that need to be made");
		private static readonly GUIContent addNavMeshLinksBetweenRoomsLabel = new GUIContent("Link Rooms", "If checked, NavMeshLinks will be formed to connect rooms in the dungeon");
		private static readonly GUIContent navMeshAgentTypesLabel = new GUIContent("Agent Types Link Info", "Per-agent information about how to create NavMeshLinks between rooms");
		private static readonly GUIContent navMeshLinkDistanceFromDoorwayLabel = new GUIContent("Distance from Doorway", "The distance on either side of each doorway that the NavMeshLink positions will be placed");
		private static readonly GUIContent disableLinkWhenDoorIsClosedLabel = new GUIContent("Disable When Door is Closed", "If true, the link will only be active when the corresponding door is open");
		private static readonly GUIContent autoGenerateFullRebakeSurfacesLabel = new GUIContent("Auto-Generate Surfaces", "If checked, a new surface will be generated for each agent type using some default settings. Uncheck this if you want to specify your own settings");
		private static readonly GUIContent fullRebakeTargetsLabel = new GUIContent("Target Surfaces", "The surfaces to use when doing a full dungeon bake. Only used when 'Auto-Generate Surfaces' is unchecked");
		private static readonly GUIContent useAutomaticLinkDistanceLabel = new GUIContent("Auto-Calculate Link Points", "Try to calculate the appropriate start and end points for the nav mesh link. If unchecked, the start and end points will be generated a specified distance apart");
		private static readonly GUIContent automaticLinkDistanceOffsetLabel = new GUIContent("Link Offset Distance", "A small offset applied to the automatic link point calculation to guarantee that the endpoints overlap the navigation mesh appropriately");

		private static readonly Dictionary<UnityNavMeshAdapter.RuntimeNavMeshBakeMode, string> bakeModeHelpLabels = new Dictionary<UnityNavMeshAdapter.RuntimeNavMeshBakeMode, string>()
		{
			{ UnityNavMeshAdapter.RuntimeNavMeshBakeMode.PreBakedOnly, "Uses only existing baked surfaces found in the dungeon tiles, no runtime baking is performed" },
			{ UnityNavMeshAdapter.RuntimeNavMeshBakeMode.AddIfNoSurfaceExists, "Uses existing baked surfaces in the tiles if any are found, otherwise new surfaces will be added and baked at runtime" },
			{ UnityNavMeshAdapter.RuntimeNavMeshBakeMode.AlwaysRebake, "Adds new surfaces where they don't already exist. Rebakes all at runtime" },
			{ UnityNavMeshAdapter.RuntimeNavMeshBakeMode.FullDungeonBake, "Bakes a single surface for the entire dungeon at runtime. No room links will be made. Openable doors will have to have NavMesh Obstacle components" },
		};

		#endregion

		private SerializedProperty priorityProp;
		private SerializedProperty bakeModeProp;
		private SerializedProperty layerMaskProp;
		private SerializedProperty addNavMeshLinksBetweenRoomsProp;
		private SerializedProperty navMeshAgentTypesProp;
		private SerializedProperty navMeshLinkDistanceFromDoorwayProp;
		private SerializedProperty autoGenerateFullRebakeSurfacesProp;
		private SerializedProperty useAutomaticLinkDistanceProp;
		private SerializedProperty automaticLinkDistanceOffsetProp;

		private ReorderableList fullRebakeTargetsList;


		private void OnEnable()
		{
			priorityProp = serializedObject.FindProperty(nameof(UnityNavMeshAdapter.Priority));
			bakeModeProp = serializedObject.FindProperty(nameof(UnityNavMeshAdapter.BakeMode));
			layerMaskProp = serializedObject.FindProperty(nameof(UnityNavMeshAdapter.LayerMask));
			addNavMeshLinksBetweenRoomsProp = serializedObject.FindProperty(nameof(UnityNavMeshAdapter.AddNavMeshLinksBetweenRooms));
			navMeshAgentTypesProp = serializedObject.FindProperty(nameof(UnityNavMeshAdapter.NavMeshAgentTypes));
			navMeshLinkDistanceFromDoorwayProp = serializedObject.FindProperty(nameof(UnityNavMeshAdapter.NavMeshLinkDistanceFromDoorway));
			autoGenerateFullRebakeSurfacesProp = serializedObject.FindProperty(nameof(UnityNavMeshAdapter.AutoGenerateFullRebakeSurfaces));
			useAutomaticLinkDistanceProp = serializedObject.FindProperty(nameof(UnityNavMeshAdapter.UseAutomaticLinkDistance));
			automaticLinkDistanceOffsetProp = serializedObject.FindProperty(nameof(UnityNavMeshAdapter.AutomaticLinkDistanceOffset));

			fullRebakeTargetsList = new ReorderableList(serializedObject, serializedObject.FindProperty("FullRebakeTargets"), true, true, true, true);
			fullRebakeTargetsList.drawElementCallback = DrawFullRebakeTargetsEntry;
			fullRebakeTargetsList.drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, fullRebakeTargetsLabel);
		}

		public override void OnInspectorGUI()
		{
			var data = target as UnityNavMeshAdapter;
			if (data == null)
				return;

			serializedObject.Update();


			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(priorityProp, InspectorConstants.AdapterPriorityLabel);
			EditorGUILayout.PropertyField(bakeModeProp, bakeModeLabel);

			// Show layer mask here unless this is a full rebake or pre-baked only
			if (data.BakeMode != UnityNavMeshAdapter.RuntimeNavMeshBakeMode.FullDungeonBake && data.BakeMode != UnityNavMeshAdapter.RuntimeNavMeshBakeMode.PreBakedOnly)
				EditorGUILayout.PropertyField(layerMaskProp, layerMaskLabel);

			string bakeModeHelpLabel;
			if (bakeModeHelpLabels.TryGetValue((UnityNavMeshAdapter.RuntimeNavMeshBakeMode)bakeModeProp.enumValueIndex, out bakeModeHelpLabel))
				EditorGUILayout.HelpBox(bakeModeHelpLabel, MessageType.Info, true);

			EditorGUILayout.Space();

			if (data.BakeMode == UnityNavMeshAdapter.RuntimeNavMeshBakeMode.FullDungeonBake)
			{
				EditorGUILayout.PropertyField(autoGenerateFullRebakeSurfacesProp, autoGenerateFullRebakeSurfacesLabel);

				EditorGUI.BeginDisabledGroup(!data.AutoGenerateFullRebakeSurfaces);
				EditorGUILayout.PropertyField(layerMaskProp, layerMaskLabel);
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup(data.AutoGenerateFullRebakeSurfaces);
				fullRebakeTargetsList.DoLayoutList();
				EditorGUI.EndDisabledGroup();
			}

			EditorGUI.BeginDisabledGroup(bakeModeProp.enumValueIndex == (int)UnityNavMeshAdapter.RuntimeNavMeshBakeMode.FullDungeonBake);
			DrawLinksGUI();
			EditorGUI.EndDisabledGroup();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawFullRebakeTargetsEntry(Rect rect, int index, bool isActive, bool isFocused)
		{
			var element = fullRebakeTargetsList.serializedProperty.GetArrayElementAtIndex(index);
			EditorGUI.PropertyField(rect, element);
		}

		private void DrawLinksGUI()
		{
			addNavMeshLinksBetweenRoomsProp.isExpanded = EditorGUILayout.Foldout(addNavMeshLinksBetweenRoomsProp.isExpanded, "Room Links");

			if (addNavMeshLinksBetweenRoomsProp.isExpanded)
			{
				EditorGUILayout.BeginVertical("box");
				EditorGUI.indentLevel++;

				EditorGUILayout.PropertyField(addNavMeshLinksBetweenRoomsProp, addNavMeshLinksBetweenRoomsLabel);

				using (new EditorGUI.DisabledScope(!addNavMeshLinksBetweenRoomsProp.boolValue))
				{
					EditorGUILayout.PropertyField(useAutomaticLinkDistanceProp, useAutomaticLinkDistanceLabel);

					if (useAutomaticLinkDistanceProp.boolValue)
						EditorGUILayout.PropertyField(automaticLinkDistanceOffsetProp, automaticLinkDistanceOffsetLabel);
					else
						EditorGUILayout.PropertyField(navMeshLinkDistanceFromDoorwayProp, navMeshLinkDistanceFromDoorwayLabel);

					EditorGUILayout.Space();
					EditorGUILayout.Space();

					EditorGUILayout.BeginVertical("box");
					EditorGUILayout.BeginHorizontal();

					EditorGUILayout.LabelField(navMeshAgentTypesLabel);

					if (GUILayout.Button("Add New"))
						navMeshAgentTypesProp.InsertArrayElementAtIndex(navMeshAgentTypesProp.arraySize);

					EditorGUILayout.EndHorizontal();

					int indexToRemove = -1;
					for (int i = 0; i < navMeshAgentTypesProp.arraySize; i++)
					{
						EditorGUILayout.BeginVertical("box");

						if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(18)))
							indexToRemove = i;

						var elementProp = navMeshAgentTypesProp.GetArrayElementAtIndex(i);
						var agentTypeID = elementProp.FindPropertyRelative("AgentTypeID");
						var areaTypeID = elementProp.FindPropertyRelative("AreaTypeID");
						var disableWhenDoorIsClosed = elementProp.FindPropertyRelative("DisableLinkWhenDoorIsClosed");

						NavMeshComponentsGUIUtility.AgentTypePopup("Agent Type", agentTypeID);
						NavMeshComponentsGUIUtility.AreaPopup("Area", areaTypeID);
						EditorGUILayout.PropertyField(disableWhenDoorIsClosed, disableLinkWhenDoorIsClosedLabel);

						EditorGUILayout.EndVertical();
					}

					EditorGUILayout.EndVertical();

					if (indexToRemove >= 0 && indexToRemove < navMeshAgentTypesProp.arraySize)
						navMeshAgentTypesProp.DeleteArrayElementAtIndex(indexToRemove);
				}

				EditorGUI.indentLevel--;
				EditorGUILayout.EndVertical();
			}
		}
	}
}
#endif