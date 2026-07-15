using System;
using UnityEngine;

namespace LostForest.Phase2.World
{
    /// <summary>
    /// Owns Unity physics component creation for generated terrain surfaces.
    /// Mesh topology stays in HexTerrainMeshBuilder; future chunking or simplified collision meshes can branch here.
    /// </summary>
    public static class HexTerrainCollisionBuilder
    {
        public const string BuilderName = nameof(HexTerrainCollisionBuilder);
        public const string PerSurfaceModeLabel = "Per-surface MeshCollider";

        public static HexTerrainColliderBuildResult BuildMeshCollider(GameObject surfaceObject, Mesh mesh)
        {
            if (surfaceObject == null)
            {
                return HexTerrainColliderBuildResult.Skipped("HexTerrainCollisionBuilder skipped MeshCollider creation because the surface GameObject was null.");
            }

            if (mesh == null)
            {
                return HexTerrainColliderBuildResult.Skipped($"HexTerrainCollisionBuilder skipped MeshCollider creation for {surfaceObject.name} because the mesh was null.");
            }

            try
            {
                MeshCollider meshCollider = surfaceObject.GetComponent<MeshCollider>();
                bool createdNewComponent = meshCollider == null;

                if (createdNewComponent)
                {
                    meshCollider = surfaceObject.AddComponent<MeshCollider>();
                }

                meshCollider.convex = false;
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = mesh;

                return HexTerrainColliderBuildResult.Succeeded(meshCollider, createdNewComponent);
            }
            catch (Exception exception)
            {
                return HexTerrainColliderBuildResult.Skipped(
                    $"HexTerrainCollisionBuilder skipped MeshCollider creation for {surfaceObject.name}: {exception.GetType().Name}: {exception.Message}");
            }
        }
    }

    public sealed class HexTerrainColliderBuildResult
    {
        private HexTerrainColliderBuildResult(Component colliderComponent, bool createdNewComponent, string skipReason)
        {
            ColliderComponent = colliderComponent;
            CreatedNewComponent = createdNewComponent;
            SkipReason = string.IsNullOrWhiteSpace(skipReason) ? string.Empty : skipReason;
        }

        public Component ColliderComponent { get; }
        public bool CreatedNewComponent { get; }
        public string SkipReason { get; }
        public bool IsSuccessful => ColliderComponent != null;

        public static HexTerrainColliderBuildResult Succeeded(Component colliderComponent, bool createdNewComponent)
        {
            return new HexTerrainColliderBuildResult(colliderComponent, createdNewComponent, string.Empty);
        }

        public static HexTerrainColliderBuildResult Skipped(string skipReason)
        {
            return new HexTerrainColliderBuildResult(null, false, skipReason);
        }
    }
}
