using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.World
{
    /// <summary>
    /// Builds renderable low-poly terrain surfaces from Frame-owned shared height points.
    /// This class owns mesh topology only; point identity, heights, and stitching stay with the Frame data graph.
    /// </summary>
    public static class HexTerrainMeshBuilder
    {
        public const string BuilderName = nameof(HexTerrainMeshBuilder);

        public static HexTerrainMeshData BuildFrame(
            TerrainFrameData frameData,
            Transform parent,
            Material material,
            HexTerrainMeshSettings settings = null)
        {
            settings = settings ?? new HexTerrainMeshSettings();

            if (frameData == null)
            {
                HexTerrainMeshData emptyMeshData = new HexTerrainMeshData(parent, "Empty Terrain Frame");
                emptyMeshData.SetColliderGenerationRequested(settings.AddMeshCollider);
                emptyMeshData.AddSkippedMesh("HexTerrainMeshBuilder skipped terrain frame mesh generation because the TerrainFrameData was null.");
                MaybeLog(emptyMeshData, settings, true);
                return emptyMeshData;
            }

            return BuildSlots(frameData.Slots, parent, material, settings, settings.GroupObjectName);
        }

        public static HexTerrainMeshData BuildSlots(
            IEnumerable<TerrainSlotData> slots,
            Transform parent,
            Material material,
            HexTerrainMeshSettings settings = null,
            string groupLabel = null)
        {
            settings = settings ?? new HexTerrainMeshSettings();
            string resolvedGroupLabel = string.IsNullOrWhiteSpace(groupLabel) ? settings.GroupObjectName : groupLabel;

            GameObject groupObject = new GameObject(resolvedGroupLabel);
            SetParentPreservingWorld(groupObject.transform, parent);

            HexTerrainMeshData meshData = new HexTerrainMeshData(groupObject.transform, resolvedGroupLabel);
            meshData.SetColliderGenerationRequested(settings.AddMeshCollider);

            if (slots == null)
            {
                meshData.AddSkippedMesh("HexTerrainMeshBuilder skipped terrain mesh generation because no TerrainSlotData collection was supplied.");
                MaybeLog(meshData, settings, true);
                return meshData;
            }

            foreach (TerrainSlotData slot in slots)
            {
                if (slot == null)
                {
                    string skipMessage = "HexTerrainMeshBuilder skipped one terrain mesh because the TerrainSlotData entry was null.";
                    Debug.LogWarning(skipMessage);
                    meshData.AddSkippedMesh(skipMessage);
                    continue;
                }

                Transform slotParent = groupObject.transform;

                if (settings.CreateSlotRootsForGroups)
                {
                    GameObject slotObject = new GameObject($"Slot {slot.Label}");
                    SetParentPreservingWorld(slotObject.transform, groupObject.transform);
                    slotParent = slotObject.transform;
                }

                meshData.Append(BuildSlotSurfaceInternal(slot, slotParent, material, settings, false));
            }

            MaybeLog(meshData, settings, true);
            return meshData;
        }

        public static HexTerrainMeshData BuildSlotSurface(
            TerrainSlotData slot,
            Transform parent,
            Material material,
            HexTerrainMeshSettings settings = null)
        {
            settings = settings ?? new HexTerrainMeshSettings();
            return BuildSlotSurfaceInternal(slot, parent, material, settings, true);
        }

        public static Mesh CreateSlotMesh(TerrainSlotData slot, HexTerrainMeshSettings settings = null)
        {
            settings = settings ?? new HexTerrainMeshSettings();

            if (!CanBuildSlotMesh(slot, out string validationMessage))
            {
                Debug.LogWarning(validationMessage);
                return null;
            }

            return CreateValidatedSlotMesh(slot, settings);
        }

        private static Mesh CreateValidatedSlotMesh(TerrainSlotData slot, HexTerrainMeshSettings settings)
        {
            IReadOnlyList<SharedHeightPoint> vertexPoints = slot.VertexPoints;
            IReadOnlyList<SharedHeightPoint> edgePoints = slot.EdgeMidpointPoints;
            IReadOnlyList<SharedHeightPoint> innerPoints = slot.InnerPoints;

            Vector3[] meshVertices = new Vector3[19];
            meshVertices[0] = Lift(slot.CenterPoint.Position, settings.SurfaceLift);

            for (int i = 0; i < 6; i++)
            {
                meshVertices[1 + i] = Lift(vertexPoints[i].Position, settings.SurfaceLift);
                meshVertices[7 + i] = Lift(edgePoints[i].Position, settings.SurfaceLift);
                meshVertices[13 + i] = Lift(innerPoints[i].Position, settings.SurfaceLift);
            }

            Mesh mesh = new Mesh();
            mesh.name = settings.GetMeshName(slot.Label);
            mesh.SetVertices(meshVertices);
            mesh.SetTriangles(BuildSlotTriangles(), 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static HexTerrainMeshData BuildSlotSurfaceInternal(
            TerrainSlotData slot,
            Transform parent,
            Material material,
            HexTerrainMeshSettings settings,
            bool allowGenerationReport)
        {
            string scopeLabel = slot == null ? "Empty Slot Terrain Mesh" : $"Slot {slot.Label} Terrain Mesh";
            HexTerrainMeshData meshData = new HexTerrainMeshData(parent, scopeLabel);
            meshData.SetColliderGenerationRequested(settings.AddMeshCollider);

            if (!CanBuildSlotMesh(slot, out string validationMessage))
            {
                Debug.LogWarning(validationMessage);
                meshData.AddSkippedMesh(validationMessage);
                MaybeLog(meshData, settings, allowGenerationReport);
                return meshData;
            }

            Mesh mesh = CreateValidatedSlotMesh(slot, settings);

            GameObject surfaceObject = new GameObject(settings.GetSurfaceObjectName(slot.Label));
            SetParentPreservingWorld(surfaceObject.transform, parent);
            surfaceObject.isStatic = settings.MarkStatic;

            MeshFilter meshFilter = surfaceObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = surfaceObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            Component meshCollider = null;

            if (settings.AddMeshCollider)
            {
                HexTerrainColliderBuildResult colliderResult = HexTerrainCollisionBuilder.BuildMeshCollider(surfaceObject, mesh);
                meshCollider = colliderResult.ColliderComponent;

                if (!colliderResult.IsSuccessful)
                {
                    Debug.LogWarning(colliderResult.SkipReason);
                    meshData.AddSkippedCollider(colliderResult.SkipReason);
                }
            }

            meshData.AddSurface(new HexTerrainMeshSurface(slot, surfaceObject, mesh, meshCollider));
            MaybeLog(meshData, settings, allowGenerationReport);
            return meshData;
        }

        private static int[] BuildSlotTriangles()
        {
            List<int> triangles = new List<int>(72);

            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                int vertex = 1 + i;
                int nextVertex = 1 + next;
                int edge = 7 + i;
                int inner = 13 + i;
                int nextInner = 13 + next;

                triangles.Add(0);
                triangles.Add(nextInner);
                triangles.Add(inner);

                triangles.Add(inner);
                triangles.Add(edge);
                triangles.Add(vertex);

                triangles.Add(inner);
                triangles.Add(nextInner);
                triangles.Add(edge);

                triangles.Add(nextInner);
                triangles.Add(nextVertex);
                triangles.Add(edge);
            }

            return triangles.ToArray();
        }

        private static bool CanBuildSlotMesh(TerrainSlotData slot, out string validationMessage)
        {
            if (slot == null)
            {
                validationMessage = "HexTerrainMeshBuilder skipped terrain mesh generation because the TerrainSlotData was null.";
                return false;
            }

            if (slot.CenterPoint == null)
            {
                validationMessage = $"HexTerrainMeshBuilder skipped Slot {slot.Label} because it has no Frame-owned center point.";
                return false;
            }

            if (!HasSixPoints(slot.VertexPoints))
            {
                validationMessage = $"HexTerrainMeshBuilder skipped Slot {slot.Label} because it does not have six Frame-owned vertex points.";
                return false;
            }

            if (!HasSixPoints(slot.EdgeMidpointPoints))
            {
                validationMessage = $"HexTerrainMeshBuilder skipped Slot {slot.Label} because it does not have six Frame-owned edge midpoint points.";
                return false;
            }

            if (!HasSixPoints(slot.InnerPoints))
            {
                validationMessage = $"HexTerrainMeshBuilder skipped Slot {slot.Label} because it does not have six Frame-owned inner points.";
                return false;
            }

            validationMessage = string.Empty;
            return true;
        }

        private static bool HasSixPoints(IReadOnlyList<SharedHeightPoint> points)
        {
            if (points == null || points.Count != 6)
            {
                return false;
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector3 Lift(Vector3 position, float lift)
        {
            return position + Vector3.up * lift;
        }

        private static void SetParentPreservingWorld(Transform child, Transform parent)
        {
            if (child == null || parent == null)
            {
                return;
            }

            child.SetParent(parent, true);
        }

        private static void MaybeLog(HexTerrainMeshData meshData, HexTerrainMeshSettings settings, bool allowGenerationReport)
        {
            if (allowGenerationReport && settings.LogGenerationReport)
            {
                Debug.Log(meshData.BuildReport());
            }
        }
    }
}
