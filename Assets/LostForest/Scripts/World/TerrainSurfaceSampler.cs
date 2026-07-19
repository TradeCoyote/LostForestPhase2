using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.World
{
    public sealed class TerrainSurfaceSampler
    {
        private const float BarycentricTolerance = -0.0005f;
        private const float DefaultRaycastPaddingMeters = 80f;

        private static readonly int[] SlotTriangles =
        {
            0, 14, 13,
            13, 7, 1,
            13, 14, 7,
            14, 2, 7,
            0, 15, 14,
            14, 8, 2,
            14, 15, 8,
            15, 3, 8,
            0, 16, 15,
            15, 9, 3,
            15, 16, 9,
            16, 4, 9,
            0, 17, 16,
            16, 10, 4,
            16, 17, 10,
            17, 5, 10,
            0, 18, 17,
            17, 11, 5,
            17, 18, 11,
            18, 6, 11,
            0, 13, 18,
            18, 12, 6,
            18, 13, 12,
            13, 1, 12
        };

        private readonly TerrainFrameData frameData;
        private readonly List<Collider> terrainColliders = new List<Collider>();
        private readonly float raycastPaddingMeters;

        public TerrainSurfaceSampler(TerrainFrameData frameData, HexTerrainMeshData terrainMeshData, float raycastPaddingMeters = DefaultRaycastPaddingMeters)
        {
            this.frameData = frameData;
            this.raycastPaddingMeters = Mathf.Max(1f, raycastPaddingMeters);
            AddTerrainColliders(terrainMeshData);
        }

        public int RaycastSampleCount { get; private set; }
        public int FallbackSampleCount { get; private set; }
        public int FailedSampleCount { get; private set; }

        public void ResetStats()
        {
            RaycastSampleCount = 0;
            FallbackSampleCount = 0;
            FailedSampleCount = 0;
        }

        public bool TrySample(Vector3 worldXzPosition, out TerrainSurfaceSample sample)
        {
            if (TrySampleTerrainCollider(worldXzPosition, out sample))
            {
                RaycastSampleCount++;
                return true;
            }

            if (TrySampleFrameFallback(worldXzPosition, out sample))
            {
                FallbackSampleCount++;
                return true;
            }

            FailedSampleCount++;
            sample = default;
            return false;
        }

        public string BuildStatsSummary(string label)
        {
            string safeLabel = string.IsNullOrWhiteSpace(label) ? "Terrain surface sampling" : label;
            return $"{safeLabel}: Raycast={RaycastSampleCount}, Fallback={FallbackSampleCount}, Failed={FailedSampleCount}";
        }

        private void AddTerrainColliders(HexTerrainMeshData terrainMeshData)
        {
            if (terrainMeshData == null)
            {
                return;
            }

            for (int i = 0; i < terrainMeshData.Surfaces.Count; i++)
            {
                Collider collider = terrainMeshData.Surfaces[i].ColliderComponent as Collider;

                if (collider != null)
                {
                    terrainColliders.Add(collider);
                }
            }
        }

        private bool TrySampleTerrainCollider(Vector3 worldXzPosition, out TerrainSurfaceSample sample)
        {
            sample = default;

            if (terrainColliders.Count == 0)
            {
                return false;
            }

            float topY = GetHighestFrameY(worldXzPosition.y) + raycastPaddingMeters;
            float bottomY = GetLowestFrameY(worldXzPosition.y) - raycastPaddingMeters;
            Ray ray = new Ray(new Vector3(worldXzPosition.x, topY, worldXzPosition.z), Vector3.down);
            float maxDistance = Mathf.Max(1f, topY - bottomY);
            bool hasHit = false;
            RaycastHit bestHit = default;

            for (int i = 0; i < terrainColliders.Count; i++)
            {
                Collider collider = terrainColliders[i];

                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                if (!collider.Raycast(ray, out RaycastHit hit, maxDistance))
                {
                    continue;
                }

                if (!hasHit || hit.distance < bestHit.distance)
                {
                    bestHit = hit;
                    hasHit = true;
                }
            }

            if (!hasHit)
            {
                return false;
            }

            sample = new TerrainSurfaceSample(bestHit.point, bestHit.normal, TerrainSurfaceSampleSource.TerrainCollider, FindSlotForWorldPosition(bestHit.point));
            return true;
        }

        private bool TrySampleFrameFallback(Vector3 worldXzPosition, out TerrainSurfaceSample sample)
        {
            sample = default;

            if (frameData == null || frameData.SlotCount == 0)
            {
                return false;
            }

            Vector2 samplePoint = new Vector2(worldXzPosition.x, worldXzPosition.z);

            for (int slotIndex = 0; slotIndex < frameData.Slots.Count; slotIndex++)
            {
                TerrainSlotData slot = frameData.Slots[slotIndex];

                if (slot == null || !TryBuildSlotSurfacePoints(slot, out Vector3[] points))
                {
                    continue;
                }

                for (int triangleIndex = 0; triangleIndex < SlotTriangles.Length; triangleIndex += 3)
                {
                    Vector3 a = points[SlotTriangles[triangleIndex]];
                    Vector3 b = points[SlotTriangles[triangleIndex + 1]];
                    Vector3 c = points[SlotTriangles[triangleIndex + 2]];

                    if (!TryGetBarycentric(samplePoint, ToPlanar(a), ToPlanar(b), ToPlanar(c), out Vector3 barycentric))
                    {
                        continue;
                    }

                    float y = a.y * barycentric.x + b.y * barycentric.y + c.y * barycentric.z;
                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

                    if (normal.y < 0f)
                    {
                        normal = -normal;
                    }

                    sample = new TerrainSurfaceSample(new Vector3(worldXzPosition.x, y, worldXzPosition.z), normal, TerrainSurfaceSampleSource.FrameHeightFallback, slot);
                    return true;
                }
            }

            return false;
        }

        private TerrainSlotData FindSlotForWorldPosition(Vector3 worldPosition)
        {
            if (frameData == null || frameData.SlotCount == 0)
            {
                return null;
            }

            Vector2 samplePoint = new Vector2(worldPosition.x, worldPosition.z);

            for (int i = 0; i < frameData.Slots.Count; i++)
            {
                TerrainSlotData slot = frameData.Slots[i];

                if (slot == null || !TryBuildSlotSurfacePoints(slot, out Vector3[] points))
                {
                    continue;
                }

                for (int triangleIndex = 0; triangleIndex < SlotTriangles.Length; triangleIndex += 3)
                {
                    if (TryGetBarycentric(
                            samplePoint,
                            ToPlanar(points[SlotTriangles[triangleIndex]]),
                            ToPlanar(points[SlotTriangles[triangleIndex + 1]]),
                            ToPlanar(points[SlotTriangles[triangleIndex + 2]]),
                            out _))
                    {
                        return slot;
                    }
                }
            }

            return null;
        }

        private float GetHighestFrameY(float fallback)
        {
            if (frameData == null || frameData.SharedPointCount == 0)
            {
                return fallback;
            }

            float highest = float.MinValue;

            for (int i = 0; i < frameData.SharedPointList.Count; i++)
            {
                highest = Mathf.Max(highest, frameData.SharedPointList[i].Position.y);
            }

            return highest;
        }

        private float GetLowestFrameY(float fallback)
        {
            if (frameData == null || frameData.SharedPointCount == 0)
            {
                return fallback;
            }

            float lowest = float.MaxValue;

            for (int i = 0; i < frameData.SharedPointList.Count; i++)
            {
                lowest = Mathf.Min(lowest, frameData.SharedPointList[i].Position.y);
            }

            return lowest;
        }

        private static bool TryBuildSlotSurfacePoints(TerrainSlotData slot, out Vector3[] points)
        {
            points = null;

            if (slot.CenterPoint == null || !HasSixPoints(slot.VertexPoints) || !HasSixPoints(slot.EdgeMidpointPoints) || !HasSixPoints(slot.InnerPoints))
            {
                return false;
            }

            points = new Vector3[19];
            points[0] = slot.CenterPoint.Position;

            for (int i = 0; i < 6; i++)
            {
                points[1 + i] = slot.VertexPoints[i].Position;
                points[7 + i] = slot.EdgeMidpointPoints[i].Position;
                points[13 + i] = slot.InnerPoints[i].Position;
            }

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

        private static Vector2 ToPlanar(Vector3 position)
        {
            return new Vector2(position.x, position.z);
        }

        private static bool TryGetBarycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out Vector3 barycentric)
        {
            Vector2 v0 = b - a;
            Vector2 v1 = c - a;
            Vector2 v2 = p - a;
            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);
            float denominator = d00 * d11 - d01 * d01;

            if (Mathf.Abs(denominator) <= Mathf.Epsilon)
            {
                barycentric = default;
                return false;
            }

            float v = (d11 * d20 - d01 * d21) / denominator;
            float w = (d00 * d21 - d01 * d20) / denominator;
            float u = 1f - v - w;
            barycentric = new Vector3(u, v, w);
            return u >= BarycentricTolerance && v >= BarycentricTolerance && w >= BarycentricTolerance;
        }
    }
}
