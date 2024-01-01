// Only available in Unity 2020.1 or higher (and in LTS versions 2018.4 and 2019.4)
#if UNITY_2020_1_OR_NEWER || UNITY_2018_4 || UNITY_2019_4

//#define NAVMESHCOMPONENTS_SHOW_NAVMESHDATA_REF
using DunGen.Adapters;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.AI;

namespace Dungen.Adapters
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(UnityNavMesh2DAdapter))]
	public sealed class UnityNavMesh2DAdapterInspector : Editor
	{
		private SerializedProperty agentTypeID;
		private SerializedProperty defaultArea;
		private SerializedProperty layerMask;
		private SerializedProperty overrideTileSize;
		private SerializedProperty overrideVoxelSize;
		private SerializedProperty tileSize;
		private SerializedProperty voxelSize;
		private SerializedProperty unwalkableArea;

#if NAVMESHCOMPONENTS_SHOW_NAVMESHDATA_REF
		SerializedProperty m_NavMeshData;
#endif
		private class Styles
		{
			public readonly GUIContent LayerMask = new GUIContent("Include Layers");
			public readonly GUIContent SpriteCollectGeometry = new GUIContent("Sprite Geometry", "Which type of geometry to collect for sprites");
		}

		static Styles styles;


		private void OnEnable()
		{
			agentTypeID = serializedObject.FindProperty("agentTypeID");
			defaultArea = serializedObject.FindProperty("defaultArea");
			layerMask = serializedObject.FindProperty("layerMask");
			overrideTileSize = serializedObject.FindProperty("overrideTileSize");
			overrideVoxelSize = serializedObject.FindProperty("overrideVoxelSize");
			tileSize = serializedObject.FindProperty("tileSize");
			voxelSize = serializedObject.FindProperty("voxelSize");
			unwalkableArea = serializedObject.FindProperty("unwalkableArea");

#if NAVMESHCOMPONENTS_SHOW_NAVMESHDATA_REF
			m_NavMeshData = serializedObject.FindProperty("navMeshData");
#endif
			NavMeshVisualizationSettings.showNavigation++;
		}

		private void OnDisable()
		{
			NavMeshVisualizationSettings.showNavigation--;
		}

		public override void OnInspectorGUI()
		{
			if (styles == null)
				styles = new Styles();

			serializedObject.Update();

			var buildSettings = NavMesh.GetSettingsByID(agentTypeID.intValue);

			if (buildSettings.agentTypeID != -1)
			{
				// Draw image
				const float diagramHeight = 80.0f;
				Rect agentDiagramRect = EditorGUILayout.GetControlRect(false, diagramHeight);
				NavMeshEditorHelpers.DrawAgentDiagram(agentDiagramRect, buildSettings.agentRadius, buildSettings.agentHeight, buildSettings.agentClimb, buildSettings.agentSlope);
			}
			NavMeshComponentsGUIUtility.AgentTypePopup("Agent Type", agentTypeID);

			EditorGUILayout.Space();

			EditorGUILayout.PropertyField(layerMask, styles.LayerMask);

			EditorGUILayout.Space();

			overrideVoxelSize.isExpanded = EditorGUILayout.Foldout(overrideVoxelSize.isExpanded, "Advanced");
			if (overrideVoxelSize.isExpanded)
			{
				EditorGUI.indentLevel++;

				NavMeshComponentsGUIUtility.AreaPopup("Default Area", defaultArea);
				NavMeshComponentsGUIUtility.AreaPopup("Unwalkable Area", unwalkableArea);

				// Override voxel size.
				EditorGUILayout.PropertyField(overrideVoxelSize);

				using (new EditorGUI.DisabledScope(!overrideVoxelSize.boolValue || overrideVoxelSize.hasMultipleDifferentValues))
				{
					EditorGUI.indentLevel++;

					EditorGUILayout.PropertyField(voxelSize);

					if (!overrideVoxelSize.hasMultipleDifferentValues)
					{
						if (!agentTypeID.hasMultipleDifferentValues)
						{
							float voxelsPerRadius = voxelSize.floatValue > 0.0f ? (buildSettings.agentRadius / voxelSize.floatValue) : 0.0f;
							EditorGUILayout.LabelField(" ", voxelsPerRadius.ToString("0.00") + " voxels per agent radius", EditorStyles.miniLabel);
						}
						if (overrideVoxelSize.boolValue)
							EditorGUILayout.HelpBox("Voxel size controls how accurately the navigation mesh is generated from the level geometry. A good voxel size is 2-4 voxels per agent radius. Making voxel size smaller will increase build time.", MessageType.None);
					}
					EditorGUI.indentLevel--;
				}

				// Override tile size
				EditorGUILayout.PropertyField(overrideTileSize);

				using (new EditorGUI.DisabledScope(!overrideTileSize.boolValue || overrideTileSize.hasMultipleDifferentValues))
				{
					EditorGUI.indentLevel++;

					EditorGUILayout.PropertyField(tileSize);

					if (!tileSize.hasMultipleDifferentValues && !voxelSize.hasMultipleDifferentValues)
					{
						float tileWorldSize = tileSize.intValue * voxelSize.floatValue;
						EditorGUILayout.LabelField(" ", tileWorldSize.ToString("0.00") + " world units", EditorStyles.miniLabel);
					}

					if (!overrideTileSize.hasMultipleDifferentValues)
					{
						if (overrideTileSize.boolValue)
							EditorGUILayout.HelpBox("Tile size controls the how local the changes to the world are (rebuild or carve). Small tile size allows more local changes, while potentially generating more data overall.", MessageType.None);
					}
					EditorGUI.indentLevel--;
				}

				EditorGUILayout.Space();
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();

			var hadError = false;
			var multipleTargets = targets.Length > 1;
			foreach (UnityNavMesh2DAdapter adapter in targets)
			{
				var settings = adapter.GetBuildSettings();
				var bounds = new Bounds(Vector3.zero, Vector3.zero);
				var errors = settings.ValidationReport(bounds);

				if (errors.Length > 0)
				{
					if (multipleTargets)
						EditorGUILayout.LabelField(adapter.name);
					foreach (var err in errors)
					{
						EditorGUILayout.HelpBox(err, MessageType.Warning);
					}
					GUILayout.BeginHorizontal();
					GUILayout.Space(EditorGUIUtility.labelWidth);
					if (GUILayout.Button("Open Agent Settings...", EditorStyles.miniButton))
						NavMeshEditorHelpers.OpenAgentSettings(adapter.AgentTypeID);
					GUILayout.EndHorizontal();
					hadError = true;
				}
			}

			if (hadError)
				EditorGUILayout.Space();

#if NAVMESHCOMPONENTS_SHOW_NAVMESHDATA_REF
			var nmdRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

			EditorGUI.BeginProperty(nmdRect, GUIContent.none, m_NavMeshData);
			var rectLabel = EditorGUI.PrefixLabel(nmdRect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(m_NavMeshData.displayName));
			EditorGUI.EndProperty();

			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUI.BeginProperty(nmdRect, GUIContent.none, m_NavMeshData);
				EditorGUI.ObjectField(rectLabel, m_NavMeshData, GUIContent.none);
				EditorGUI.EndProperty();
			}
#endif
		}
	}
}
#endif