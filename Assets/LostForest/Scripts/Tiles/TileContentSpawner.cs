using System.Collections.Generic;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Tiles
{
    public sealed class TileContentSpawner
    {
        private const int MaxPlacementAttemptsPerTree = 36;
        private const float TreeBaseEmbedMeters = 0.04f;

        private readonly Material trunkMaterial;
        private readonly Material crownMaterial;
        private readonly Material homeTrunkMaterial;
        private readonly Material threatTrunkMaterial;

        public TileContentSpawner(
            Material trunkMaterial,
            Material crownMaterial,
            Material homeTrunkMaterial,
            Material threatTrunkMaterial)
        {
            this.trunkMaterial = trunkMaterial;
            this.crownMaterial = crownMaterial;
            this.homeTrunkMaterial = homeTrunkMaterial;
            this.threatTrunkMaterial = threatTrunkMaterial;
        }

        public int SpawnForestStandIns(
            Transform parent,
            TerrainSlotData slot,
            TileDefinition definition,
            TerrainSurfaceSampler surfaceSampler,
            int worldSeed,
            int orientationIndex,
            float hexOuterRadiusMeters,
            out int groundedCount,
            out int skippedCount)
        {
            groundedCount = 0;
            skippedCount = 0;

            if (parent == null || slot == null || definition == null || definition.ForestFill == null)
            {
                return 0;
            }

            ForestFillProfile profile = definition.ForestFill;
            int contentSeed = BuildContentSeed(worldSeed, slot.Label, definition.TileId, profile.SeedSalt, orientationIndex);
            System.Random random = new System.Random(contentSeed);
            List<Vector3> acceptedPositions = new List<Vector3>(profile.CurrentTileTreeCount);
            int spawnedCount = 0;

            Transform tileContentRoot = new GameObject($"Tile {definition.TileIdLabel} {slot.Label} Content").transform;
            tileContentRoot.SetParent(parent, false);

            for (int i = 0; i < profile.CurrentTileTreeCount; i++)
            {
                if (!TryGetTreePlanarPosition(slot, profile, random, hexOuterRadiusMeters, acceptedPositions, out Vector3 planarPosition))
                {
                    skippedCount++;
                    continue;
                }

                if (surfaceSampler == null || !surfaceSampler.TrySample(planarPosition, out TerrainSurfaceSample surfaceSample))
                {
                    skippedCount++;
                    continue;
                }

                Vector3 position = surfaceSample.Position - Vector3.up * TreeBaseEmbedMeters;
                acceptedPositions.Add(position);
                SpawnTree(tileContentRoot, definition, random, position, profile, spawnedCount);
                spawnedCount++;
                groundedCount++;
            }

            return spawnedCount;
        }

        private static int BuildContentSeed(int worldSeed, string slotLabel, int tileId, int seedSalt, int orientationIndex)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + worldSeed;
                hash = hash * 31 + tileId;
                hash = hash * 31 + seedSalt;
                hash = hash * 31 + Mathf.Clamp(orientationIndex, 0, 5);
                hash = hash * 31 + GetStableStringHash(slotLabel);
                return hash;
            }
        }

        private static int GetStableStringHash(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;

                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        private static bool TryGetTreePlanarPosition(
            TerrainSlotData slot,
            ForestFillProfile profile,
            System.Random random,
            float hexOuterRadiusMeters,
            IReadOnlyList<Vector3> acceptedPositions,
            out Vector3 position)
        {
            float usableRadius = Mathf.Max(1f, hexOuterRadiusMeters - profile.EdgeInsetMeters);

            for (int attempt = 0; attempt < MaxPlacementAttemptsPerTree; attempt++)
            {
                Vector2 point = RandomPointInCircle(random, usableRadius);

                if (!IsInsideFlatTopHex(point, usableRadius))
                {
                    continue;
                }

                if (point.magnitude < profile.CenterClearingRadiusMeters)
                {
                    continue;
                }

                Vector3 candidate = slot.CenterPoint.Position + new Vector3(point.x, 0f, point.y);

                if (IsTooClose(candidate, acceptedPositions, profile.MinTreeSpacingMeters))
                {
                    continue;
                }

                position = candidate;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        private static Vector2 RandomPointInCircle(System.Random random, float radius)
        {
            float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
            float distance = Mathf.Sqrt((float)random.NextDouble()) * radius;
            return new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
        }

        private static bool IsInsideFlatTopHex(Vector2 point, float outerRadius)
        {
            float qx = Mathf.Abs(point.x);
            float qy = Mathf.Abs(point.y);
            float innerRadius = outerRadius * 0.8660254f;

            if (qx > outerRadius || qy > innerRadius)
            {
                return false;
            }

            return innerRadius * outerRadius - innerRadius * qx - outerRadius * 0.5f * qy >= 0f;
        }

        private static bool IsTooClose(Vector3 candidate, IReadOnlyList<Vector3> acceptedPositions, float minSpacing)
        {
            float minSpacingSqr = minSpacing * minSpacing;

            for (int i = 0; i < acceptedPositions.Count; i++)
            {
                Vector3 delta = candidate - acceptedPositions[i];
                delta.y = 0f;

                if (delta.sqrMagnitude < minSpacingSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private void SpawnTree(Transform parent, TileDefinition definition, System.Random random, Vector3 basePosition, ForestFillProfile profile, int index)
        {
            float height = Lerp(profile.TrunkHeightRangeMeters, random);
            float radius = Lerp(profile.TrunkRadiusRangeMeters, random);
            float yaw = (float)random.NextDouble() * 360f;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = $"Tree Stand-In {index:00}";
            trunk.transform.SetParent(parent, false);
            trunk.transform.position = basePosition + Vector3.up * (height * 0.5f);
            trunk.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            trunk.transform.localScale = new Vector3(radius, height * 0.5f, radius);
            trunk.GetComponent<Renderer>().sharedMaterial = GetTrunkMaterial(definition);

            if (definition.ContentCategory == TileContentCategory.ThreatOrigin)
            {
                return;
            }

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = $"Tree Snow Cap {index:00}";
            crown.transform.SetParent(parent, false);
            crown.transform.position = basePosition + Vector3.up * (height + radius * 0.8f);
            crown.transform.localScale = new Vector3(radius * 3.8f, radius * 0.65f, radius * 3.8f);
            crown.GetComponent<Renderer>().sharedMaterial = crownMaterial;
        }

        private Material GetTrunkMaterial(TileDefinition definition)
        {
            switch (definition.ContentCategory)
            {
                case TileContentCategory.Home:
                    return homeTrunkMaterial == null ? trunkMaterial : homeTrunkMaterial;
                case TileContentCategory.ThreatOrigin:
                    return threatTrunkMaterial == null ? trunkMaterial : threatTrunkMaterial;
                default:
                    return trunkMaterial;
            }
        }

        private static float Lerp(Vector2 range, System.Random random)
        {
            return Mathf.Lerp(range.x, range.y, (float)random.NextDouble());
        }
    }
}
