using System.Collections.Generic;
using LostForest.Phase2.Runes;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Tiles
{
    public sealed class TileContentSpawner
    {
        private const int MaxPlacementAttemptsPerTree = 36;
        private const float TreeBaseEmbedMeters = 0.04f;
        private const float PrototypeTrunkHeightMultiplier = 3f;
        private static readonly float[] FixedBandHeights01 = { 0.14f, 0.22f, 0.31f, 0.43f, 0.56f, 0.68f, 0.79f, 0.89f };

        private readonly Material trunkMaterial;
        private readonly Material crownMaterial;
        private readonly Material homeTrunkMaterial;
        private readonly Material threatTrunkMaterial;
        private readonly Material barkBandMaterial;

        public TileContentSpawner(
            Material trunkMaterial,
            Material crownMaterial,
            Material homeTrunkMaterial,
            Material threatTrunkMaterial,
            Material barkBandMaterial = null)
        {
            this.trunkMaterial = trunkMaterial;
            this.crownMaterial = crownMaterial;
            this.homeTrunkMaterial = homeTrunkMaterial;
            this.threatTrunkMaterial = threatTrunkMaterial;
            this.barkBandMaterial = barkBandMaterial;
        }

        public int SpawnForestStandIns(
            Transform parent,
            TerrainSlotData slot,
            TileDefinition definition,
            TerrainSurfaceSampler surfaceSampler,
            int worldSeed,
            int orientationIndex,
            float hexOuterRadiusMeters,
            RuneManager runeManager,
            FieldSlotData fieldSlot,
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
            List<RuneTreeAnchor> runeTreeAnchors = runeManager == null ? null : new List<RuneTreeAnchor>(profile.CurrentTileTreeCount);
            int spawnedCount = 0;

            Transform tileContentRoot = new GameObject($"Tile {definition.TileIdLabel} {slot.Label} Content").transform;
            tileContentRoot.SetParent(parent, false);

            for (int i = 0; i < profile.CurrentTileTreeCount; i++)
            {
                if (!TryGetTreePlanarPosition(slot, profile, random, hexOuterRadiusMeters, acceptedPositions, orientationIndex, out Vector3 planarPosition))
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
                RuneTreeAnchor runeTreeAnchor = SpawnTree(tileContentRoot, definition, random, position, profile, spawnedCount);
                runeTreeAnchors?.Add(runeTreeAnchor);
                spawnedCount++;
                groundedCount++;
            }

            if (runeManager != null && runeTreeAnchors != null)
            {
                runeManager.SpawnTreeMarkersForSlot(tileContentRoot, fieldSlot, runeTreeAnchors);
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
            int orientationIndex,
            out Vector3 position)
        {
            float usableRadius = Mathf.Max(1f, hexOuterRadiusMeters - profile.EdgeInsetMeters);
            Quaternion contentRotation = Quaternion.Euler(0f, Mathf.Clamp(orientationIndex, 0, 5) * 60f, 0f);

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

                Vector3 localPosition = contentRotation * new Vector3(point.x, 0f, point.y);
                Vector3 candidate = slot.CenterPoint.Position + localPosition;

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

        private RuneTreeAnchor SpawnTree(Transform parent, TileDefinition definition, System.Random random, Vector3 basePosition, ForestFillProfile profile, int index)
        {
            float height = Lerp(profile.TrunkHeightRangeMeters, random) * PrototypeTrunkHeightMultiplier;
            float radius = Lerp(profile.TrunkRadiusRangeMeters, random);
            float yaw = (float)random.NextDouble() * 360f;

            Transform treeRoot = new GameObject($"Tree Stand-In {index:00}").transform;
            treeRoot.SetParent(parent, false);
            treeRoot.position = basePosition;
            treeRoot.rotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = $"Tree Stand-In Trunk {index:00}";
            trunk.transform.SetParent(treeRoot, false);
            trunk.transform.localPosition = Vector3.up * (height * 0.5f);
            trunk.transform.localRotation = Quaternion.identity;
            trunk.transform.localScale = new Vector3(radius, height * 0.5f, radius);
            trunk.GetComponent<Renderer>().sharedMaterial = GetTrunkMaterial(definition);
            RemoveCollider(trunk);

            SpawnBarkBands(treeRoot, radius, height, index);

            if (definition.ContentCategory == TileContentCategory.ThreatOrigin)
            {
                return new RuneTreeAnchor(treeRoot, radius, height, index);
            }

            return new RuneTreeAnchor(treeRoot, radius, height, index);
        }

        private void SpawnBarkBands(Transform treeRoot, float trunkRadius, float trunkHeight, int treeIndex)
        {
            for (int i = 0; i < FixedBandHeights01.Length; i++)
            {
                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                band.name = $"Tree Stand-In Bark Band {treeIndex:00}-{i:00}";
                band.transform.SetParent(treeRoot, false);
                band.transform.localPosition = Vector3.up * (trunkHeight * FixedBandHeights01[i]);
                band.transform.localRotation = Quaternion.identity;
                band.transform.localScale = new Vector3(trunkRadius * 1.012f, 0.04f, trunkRadius * 1.012f);
                band.GetComponent<Renderer>().sharedMaterial = barkBandMaterial == null ? threatTrunkMaterial : barkBandMaterial;
                RemoveCollider(band);
            }
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

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();

            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }
    }
}
