// Only available in Unity 5.6 or higher
#if !(UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5)
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace DunGen.Adapters
{
	[AddComponentMenu("DunGen/NavMesh/Unity NavMesh Adapter")]
	public class UnityNavMeshAdapter : NavMeshAdapter
	{
		#region Nested Types

		public enum RuntimeNavMeshBakeMode
		{
			/// <summary>
			/// Uses only existing baked surfaces found in the dungeon tiles, no runtime baking is performed
			/// </summary>
			PreBakedOnly,
			/// <summary>
			/// Uses existing baked surfaces in the tiles if any are found, otherwise new surfaces will be added and baked at runtime
			/// </summary>
			AddIfNoSurfaceExists,
			/// <summary>
			/// Adds new surfaces where they don't already exist. Rebakes all at runtime
			/// </summary>
			AlwaysRebake,
			/// <summary>
			/// Bakes a single surface for the entire dungeon at runtime. No links will be made
			/// </summary>
			FullDungeonBake,
		}

		[Serializable]
		public sealed class NavMeshAgentLinkInfo
		{
			public int AgentTypeID = 0;
			public int AreaTypeID = 0;
			public bool DisableLinkWhenDoorIsClosed = true;
		}

		#endregion

		public RuntimeNavMeshBakeMode BakeMode = RuntimeNavMeshBakeMode.AddIfNoSurfaceExists;
		public LayerMask LayerMask = ~0;
		public bool AddNavMeshLinksBetweenRooms = true;
		public List<NavMeshAgentLinkInfo> NavMeshAgentTypes = new List<NavMeshAgentLinkInfo>() { new NavMeshAgentLinkInfo() };
		public float NavMeshLinkDistanceFromDoorway = 2.5f;
		public bool AutoGenerateFullRebakeSurfaces = true;
		public List<NavMeshSurface> FullRebakeTargets = new List<NavMeshSurface>();
		public bool UseAutomaticLinkDistance = false;
		public float AutomaticLinkDistanceOffset = 0.1f;

		private List<NavMeshSurface> addedSurfaces = new List<NavMeshSurface>();
		private List<NavMeshSurface> fullBakeSurfaces = new List<NavMeshSurface>();


		public override void Generate(Dungeon dungeon)
		{
			if (BakeMode == RuntimeNavMeshBakeMode.FullDungeonBake)
			{
				BakeFullDungeon(dungeon);
				return;
			}

			// Bake Surfaces
			if (BakeMode != RuntimeNavMeshBakeMode.PreBakedOnly)
			{
				foreach (var tile in dungeon.AllTiles)
				{
					// Find existing surfaces
					var existingSurfaces = tile.gameObject.GetComponentsInChildren<NavMeshSurface>();

					// Add surfaces for any agent type that is missing one
					var addedSurfaces = AddMissingSurfaces(tile, existingSurfaces);

					// Gather surfaces to bake
					IEnumerable<NavMeshSurface> surfacesToBake = addedSurfaces;

					// Append all existing surfaces if mode is set to "Always Rebake"
					if (BakeMode == RuntimeNavMeshBakeMode.AlwaysRebake)
						surfacesToBake = surfacesToBake.Concat(existingSurfaces);
					// Append only unbaked surfaces if mode is set to "Add if no Surface Exists"
					else if (BakeMode == RuntimeNavMeshBakeMode.AddIfNoSurfaceExists)
					{
						var existingUnbakedSurfaces = existingSurfaces.Where(x => x.navMeshData == null);
						surfacesToBake = surfacesToBake.Concat(existingUnbakedSurfaces);
					}


					// Bake
					foreach (var surface in surfacesToBake.Distinct())
						surface.BuildNavMesh();
				}
			}

			// Add links between rooms
			if (AddNavMeshLinksBetweenRooms)
			{
				foreach (var connection in dungeon.Connections)
					foreach (var linkInfo in NavMeshAgentTypes)
						AddNavMeshLink(connection, linkInfo);
			}

			if (OnProgress != null)
				OnProgress(new NavMeshGenerationProgress() { Description = "Done", Percentage = 1.0f });
		}

		private void BakeFullDungeon(Dungeon dungeon)
		{
			if (AutoGenerateFullRebakeSurfaces)
			{
				foreach (var surface in fullBakeSurfaces)
					if (surface != null)
						surface.RemoveData();

				fullBakeSurfaces.Clear();

				int settingsCount = NavMesh.GetSettingsCount();

				for (int i = 0; i < settingsCount; i++)
				{
					var settings = NavMesh.GetSettingsByIndex(i);

					// Find a surface if it already exists
					var surface = dungeon.gameObject.GetComponents<NavMeshSurface>()
						.Where(s => s.agentTypeID == settings.agentTypeID)
						.FirstOrDefault();

					if (surface == null)
					{
						surface = dungeon.gameObject.AddComponent<NavMeshSurface>();

						surface.agentTypeID = settings.agentTypeID;
						surface.collectObjects = CollectObjects.Children;
						surface.layerMask = LayerMask;
					}

					fullBakeSurfaces.Add(surface);

					surface.BuildNavMesh();
				}

				// Disable all other surfaces to avoid overlapping navmeshes
				foreach (var surface in dungeon.gameObject.GetComponentsInChildren<NavMeshSurface>())
					if (!fullBakeSurfaces.Contains(surface))
						surface.enabled = false;
			}
			else
			{
				foreach (var surface in FullRebakeTargets)
					surface.BuildNavMesh();
			}

			if (OnProgress != null)
				OnProgress(new NavMeshGenerationProgress() { Description = "Done", Percentage = 1.0f });
		}

		private NavMeshSurface[] AddMissingSurfaces(Tile tile, NavMeshSurface[] existingSurfaces)
		{
			addedSurfaces.Clear();
			int settingsCount = NavMesh.GetSettingsCount();

			for (int i = 0; i < settingsCount; i++)
			{
				var settings = NavMesh.GetSettingsByIndex(i);

				// We already have a surface for this agent type
				if (existingSurfaces.Where(x => x.agentTypeID == settings.agentTypeID).Any())
					continue;

				var surface = tile.gameObject.AddComponent<NavMeshSurface>();
				surface.agentTypeID = settings.agentTypeID;
				surface.collectObjects = CollectObjects.Children;
				surface.layerMask = LayerMask;

				addedSurfaces.Add(surface);
			}

			return addedSurfaces.ToArray();
		}

		private void AddNavMeshLink(DoorwayConnection connection, NavMeshAgentLinkInfo agentLinkInfo)
		{
			var doorway = connection.A.gameObject;
			var agentSettings = NavMesh.GetSettingsByID(agentLinkInfo.AgentTypeID);

			// We need to account for the agent's radius when setting the link's width
			float linkWidth = Mathf.Max(connection.A.Socket.Size.x - (agentSettings.agentRadius * 2), 0.01f);

			// Add NavMeshLink to one of the doorways
			var link = doorway.AddComponent<NavMeshLink>();
			link.agentTypeID = agentLinkInfo.AgentTypeID;
			link.bidirectional = true;
			link.area = agentLinkInfo.AreaTypeID;
			link.width = linkWidth;

			if (UseAutomaticLinkDistance)
			{
				link.startPoint = doorway.transform.InverseTransformPoint(GetClosestPointOnNavMesh(doorway.transform.position, doorway.transform.forward)) + new Vector3(0f, 0f, AutomaticLinkDistanceOffset);
				link.endPoint = doorway.transform.InverseTransformPoint(GetClosestPointOnNavMesh(doorway.transform.position, -doorway.transform.forward)) - new Vector3(0f, 0f, AutomaticLinkDistanceOffset);
			}
			else
			{
				link.startPoint = new Vector3(0, 0, -NavMeshLinkDistanceFromDoorway);
				link.endPoint = new Vector3(0, 0, NavMeshLinkDistanceFromDoorway);
			}


			if (agentLinkInfo.DisableLinkWhenDoorIsClosed)
			{
				// If there is a door in this doorway, hookup event listeners to enable/disable the link when the door is opened/closed respectively
				GameObject doorObj = (connection.A.UsedDoorPrefabInstance != null) ? connection.A.UsedDoorPrefabInstance : (connection.B.UsedDoorPrefabInstance != null) ? connection.B.UsedDoorPrefabInstance : null;

				if (doorObj != null)
				{
					var door = doorObj.GetComponent<Door>();
					link.enabled = door.IsOpen;

					if (door != null)
						door.OnDoorStateChanged += (d, o) => link.enabled = o;
				}
			}
		}

		/// <summary>
		/// Finds the closest point on the navigation mesh. Unlike NavMesh.FindClosestEdge and
		/// NavMesh.Raycast, this method works reliably when given a point that is not on the navmesh
		/// </summary>
		/// <param name="point">The position we want to find the nearest point to</param>
		/// <param name="distanceDirection">An optional direction. If specified, this will ignore points in the opposite direction</param>
		/// <returns></returns>
		private Vector3 GetClosestPointOnNavMesh(Vector3 point, Vector3? distanceDirection = null)
		{
			float CalculateDistance(Vector3 p0, Vector3 p1)
			{
				if (distanceDirection == null)
					return (p0 - p1).magnitude;
				else
				{
					float distance = Vector3.Dot(p1 - p0, distanceDirection.Value);

					if (distance <= 0f)
						return float.PositiveInfinity;
					else
						return (p0 - p1).magnitude;
				}
			}

			var triangulation = NavMesh.CalculateTriangulation();

			Vector3 closestPoint = point;
			float closestDistance = float.PositiveInfinity;

			for (int i = 2; i < triangulation.indices.Length; i += 3)
			{
				Vector3 v0 = triangulation.vertices[triangulation.indices[i - 2]];
				Vector3 v1 = triangulation.vertices[triangulation.indices[i - 1]];
				Vector3 v2 = triangulation.vertices[triangulation.indices[i]];

				Vector3 p0 = GetClosestPointOnEdge(point, v0, v1);
				Vector3 p1 = GetClosestPointOnEdge(point, v1, v2);
				Vector3 p2 = GetClosestPointOnEdge(point, v2, v0);

				float p0Dist = CalculateDistance(point, p0);
				float p1Dist = CalculateDistance(point, p1);
				float p2Dist = CalculateDistance(point, p2);

				if (p0Dist < closestDistance)
				{
					closestDistance = p0Dist;
					closestPoint = p0;
				}

				if (p1Dist < closestDistance)
				{
					closestDistance = p1Dist;
					closestPoint = p1;
				}

				if (p2Dist < closestDistance)
				{
					closestDistance = p2Dist;
					closestPoint = p2;
				}
			}

			return closestPoint;
		}

		private Vector3 GetClosestPointOnEdge(Vector3 referencePoint, Vector3 edgePointA, Vector3 edgePointB)
		{
			Vector3 direction = edgePointB - edgePointA;
			float lineLength = direction.magnitude;
			direction.Normalize();

			float projectDistance = Mathf.Clamp(Vector3.Dot(referencePoint - edgePointA, direction), 0f, lineLength);
			return edgePointA + direction * projectDistance;
		}
	}
}
#endif