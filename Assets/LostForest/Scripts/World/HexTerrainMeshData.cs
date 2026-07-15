using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LostForest.Phase2.World
{
    public sealed class HexTerrainMeshData
    {
        private readonly List<HexTerrainMeshSurface> surfaces = new List<HexTerrainMeshSurface>();
        private readonly List<string> skippedMeshMessages = new List<string>();
        private readonly List<string> skippedColliderMessages = new List<string>();
        private bool colliderGenerationRequested;

        public HexTerrainMeshData(Transform root, string scopeLabel)
        {
            Root = root;
            ScopeLabel = string.IsNullOrWhiteSpace(scopeLabel) ? "Hex Terrain Mesh" : scopeLabel;
        }

        public Transform Root { get; }
        public string ScopeLabel { get; }
        public IReadOnlyList<HexTerrainMeshSurface> Surfaces => surfaces;
        public IReadOnlyList<string> SkippedMeshMessages => skippedMeshMessages;
        public IReadOnlyList<string> SkippedColliderMessages => skippedColliderMessages;
        public int SurfaceCount => surfaces.Count;
        public int MeshCount => SurfaceCount;
        public bool ColliderGenerationRequested => colliderGenerationRequested;
        public int SkippedMeshCount => skippedMeshMessages.Count;
        public int SkippedColliderCount => skippedColliderMessages.Count;

        public int TotalVertexCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < surfaces.Count; i++)
                {
                    count += surfaces[i].VertexCount;
                }

                return count;
            }
        }

        public int TotalTriangleCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < surfaces.Count; i++)
                {
                    count += surfaces[i].TriangleCount;
                }

                return count;
            }
        }

        public int ColliderCount
        {
            get
            {
                int count = 0;

                for (int i = 0; i < surfaces.Count; i++)
                {
                    if (surfaces[i].ColliderComponent != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void AddSurface(HexTerrainMeshSurface surface)
        {
            if (surface == null)
            {
                return;
            }

            surfaces.Add(surface);
        }

        public void SetColliderGenerationRequested(bool requested)
        {
            colliderGenerationRequested = requested;
        }

        public void AddSkippedMesh(string message)
        {
            skippedMeshMessages.Add(GetSafeMessage(message, "Terrain mesh generation was skipped for an unspecified reason."));
        }

        public void AddSkippedCollider(string message)
        {
            skippedColliderMessages.Add(GetSafeMessage(message, "Terrain MeshCollider generation was skipped for an unspecified reason."));
        }

        public void Append(HexTerrainMeshData other)
        {
            if (other == null)
            {
                return;
            }

            for (int i = 0; i < other.surfaces.Count; i++)
            {
                surfaces.Add(other.surfaces[i]);
            }

            for (int i = 0; i < other.skippedMeshMessages.Count; i++)
            {
                skippedMeshMessages.Add(other.skippedMeshMessages[i]);
            }

            for (int i = 0; i < other.skippedColliderMessages.Count; i++)
            {
                skippedColliderMessages.Add(other.skippedColliderMessages[i]);
            }

            colliderGenerationRequested |= other.colliderGenerationRequested;
        }

        public bool ValidateColliderReadiness(out string validationMessage)
        {
            if (ColliderGenerationRequested)
            {
                if (MeshCount == 0)
                {
                    validationMessage = "Failed: collider generation is enabled, but no terrain meshes were generated.";
                    return false;
                }

                if (ColliderCount == MeshCount && SkippedMeshCount == 0 && SkippedColliderCount == 0)
                {
                    validationMessage = $"Passed: collider generation is enabled and {ColliderCount}/{MeshCount} terrain meshes have MeshColliders.";
                    return true;
                }

                validationMessage = $"Failed: collider generation is enabled, but {ColliderCount}/{MeshCount} terrain meshes have MeshColliders, {SkippedMeshCount} mesh builds were skipped, and {SkippedColliderCount} collider builds were skipped.";
                return false;
            }

            if (ColliderCount == 0)
            {
                validationMessage = $"Disabled: collider generation is off and {MeshCount} terrain meshes have no generated MeshColliders.";
                return true;
            }

            validationMessage = $"Failed: collider generation is disabled, but {ColliderCount} terrain MeshColliders were present.";
            return false;
        }

        public string BuildReport()
        {
            bool colliderValidationPassed = ValidateColliderReadiness(out string colliderValidationMessage);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Lost Forest Phase 2 terrain mesh generated by reusable builder.");
            builder.AppendLine($"Builder\t{HexTerrainMeshBuilder.BuilderName}");
            builder.AppendLine($"Scope\t{ScopeLabel}");
            builder.AppendLine($"Surfaces\t{SurfaceCount}");
            builder.AppendLine($"Meshes\t{MeshCount}");
            builder.AppendLine($"Vertices\t{TotalVertexCount}");
            builder.AppendLine($"Triangles\t{TotalTriangleCount}");
            builder.AppendLine($"Mesh collider generation\t{(ColliderGenerationRequested ? "Enabled" : "Disabled")}");
            builder.AppendLine($"Mesh colliders\t{ColliderCount}");
            builder.AppendLine($"Skipped meshes\t{SkippedMeshCount}");
            builder.AppendLine($"Skipped mesh colliders\t{SkippedColliderCount}");
            builder.AppendLine($"Collider validation\t{(colliderValidationPassed ? "OK" : "Needs attention")} - {colliderValidationMessage}");

            AppendMessages(builder, "Skipped mesh detail", skippedMeshMessages);
            AppendMessages(builder, "Skipped collider detail", skippedColliderMessages);

            return builder.ToString();
        }

        private static void AppendMessages(StringBuilder builder, string label, IReadOnlyList<string> messages)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                builder.AppendLine($"{label} {i + 1}\t{messages[i]}");
            }
        }

        private static string GetSafeMessage(string message, string fallback)
        {
            return string.IsNullOrWhiteSpace(message) ? fallback : message;
        }
    }

    public sealed class HexTerrainMeshSurface
    {
        public HexTerrainMeshSurface(
            TerrainSlotData slot,
            GameObject surfaceObject,
            Mesh mesh,
            Component colliderComponent)
        {
            Slot = slot;
            SurfaceObject = surfaceObject;
            Mesh = mesh;
            ColliderComponent = colliderComponent;
        }

        public TerrainSlotData Slot { get; }
        public string SlotLabel => Slot == null ? string.Empty : Slot.Label;
        public GameObject SurfaceObject { get; }
        public Mesh Mesh { get; }
        public Component ColliderComponent { get; }
        public int VertexCount => Mesh == null ? 0 : Mesh.vertexCount;
        public int TriangleCount => Mesh == null ? 0 : Mesh.triangles.Length / 3;
    }
}
