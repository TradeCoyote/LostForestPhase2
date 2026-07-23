using System.Collections.Generic;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Feedback
{
    [ExecuteAlways]
    public sealed class PrototypeBirchForestDebugSpawner : MonoBehaviour
    {
        private const int MaxPlacementAttemptsPerTree = 42;
        private const string TreeRootName = "Prototype Birch Fog Readability Trees";
        private static readonly float[] FixedBandHeights01 = { 0.14f, 0.22f, 0.31f, 0.43f, 0.56f, 0.68f, 0.79f, 0.89f };

        [Header("Terrain Source")]
        [SerializeField] private SevenHexTerrainFrameDebugView terrainFrame;
        [SerializeField] private bool rebuildOnStart = true;
        [SerializeField] private bool logRebuild = true;

        [Header("Placement")]
        [SerializeField] private int seed = 20260720;
        [SerializeField] private int treeCount = 78;
        [SerializeField] private float placementRadiusMeters = 142f;
        [SerializeField] private float centerClearingRadiusMeters = 13f;
        [SerializeField] private float minTreeSpacingMeters = 4.85f;
        [SerializeField] private float raycastStartHeightMeters = 140f;
        [SerializeField] private float raycastDistanceMeters = 280f;

        [Header("Birch Scale")]
        [SerializeField] private float trunkHeightMultiplier = 3f;
        [SerializeField] private Vector2 baseTrunkHeightRangeMeters = new Vector2(9f, 16f);
        [SerializeField] private Vector2 trunkRadiusRangeMeters = new Vector2(0.36f, 0.78f);
        [SerializeField] private float maxLeanDegrees = 4f;

        [Header("Bark Bands")]
        [SerializeField] private int minBandCount = 5;
        [SerializeField] private int maxBandCount = 10;
        [SerializeField] private Vector2 bandHeightRangeMeters = new Vector2(0.05f, 0.12f);
        [SerializeField] private Vector2 bandRadiusMultiplierRange = new Vector2(1.008f, 1.014f);
        [SerializeField, Range(0f, 1f)] private float lowestBandHeight01 = 0.10f;
        [SerializeField, Range(0f, 1f)] private float highestBandHeight01 = 0.92f;

        [Header("Materials")]
        [SerializeField] private Color trunkColor = new Color(0.91f, 0.94f, 0.91f, 1f);
        [SerializeField] private Color barkBandColor = new Color(0.045f, 0.047f, 0.045f, 1f);

        private Material trunkMaterial;
        private Material barkBandMaterial;

        public void SetTerrainFrame(SevenHexTerrainFrameDebugView newTerrainFrame)
        {
            terrainFrame = newTerrainFrame;
        }

        public void ApplyEarlyFogVisibilityDefaults()
        {
            seed = 20260720;
            treeCount = 78;
            placementRadiusMeters = 142f;
            centerClearingRadiusMeters = 13f;
            minTreeSpacingMeters = 4.85f;

            trunkHeightMultiplier = 3f;
            baseTrunkHeightRangeMeters = new Vector2(9f, 16f);
            trunkRadiusRangeMeters = new Vector2(0.36f, 0.78f);
            maxLeanDegrees = 4f;

            minBandCount = 5;
            maxBandCount = 10;
            bandHeightRangeMeters = new Vector2(0.05f, 0.12f);
            bandRadiusMultiplierRange = new Vector2(1.008f, 1.014f);
            lowestBandHeight01 = 0.10f;
            highestBandHeight01 = 0.92f;

            trunkColor = new Color(0.91f, 0.94f, 0.91f, 1f);
            barkBandColor = new Color(0.045f, 0.047f, 0.045f, 1f);
        }

        private void Start()
        {
            if (Application.isPlaying && rebuildOnStart)
            {
                Rebuild();
            }
        }

        [ContextMenu("Rebuild Prototype Birch Forest")]
        public void Rebuild()
        {
            DiscoverTerrainFrame();

            if (terrainFrame != null && terrainFrame.TerrainFrameData == null)
            {
                terrainFrame.Rebuild();
            }

            ClearTrees();
            EnsureMaterials();

            Transform root = new GameObject(TreeRootName).transform;
            root.SetParent(transform, false);

            System.Random random = new System.Random(seed);
            List<Vector3> acceptedPositions = new List<Vector3>(treeCount);
            int spawnedCount = 0;
            int skippedCount = 0;
            int maxAttempts = Mathf.Max(treeCount * MaxPlacementAttemptsPerTree, treeCount);
            Physics.SyncTransforms();

            for (int attempt = 0; attempt < maxAttempts && spawnedCount < treeCount; attempt++)
            {
                Vector2 planarPoint = RandomPointInCircle(random, placementRadiusMeters);

                if (planarPoint.magnitude < centerClearingRadiusMeters)
                {
                    continue;
                }

                Vector3 candidatePosition = GetPlacementOrigin() + new Vector3(planarPoint.x, 0f, planarPoint.y);

                if (!TryGroundPosition(candidatePosition, out Vector3 groundedPosition))
                {
                    skippedCount++;
                    continue;
                }

                if (IsTooClose(groundedPosition, acceptedPositions, minTreeSpacingMeters))
                {
                    continue;
                }

                acceptedPositions.Add(groundedPosition);
                SpawnBirchTree(root, groundedPosition, random, spawnedCount);
                spawnedCount++;
            }

            if (logRebuild)
            {
                Debug.Log($"Lost Forest prototype birch fog readability trees rebuilt. Spawned={spawnedCount}, SkippedGrounding={skippedCount}, HeightMultiplier={trunkHeightMultiplier:0.##}x, BarkBands={minBandCount}-{maxBandCount}");
            }
        }

        private void DiscoverTerrainFrame()
        {
            if (terrainFrame == null)
            {
                terrainFrame = Object.FindAnyObjectByType<SevenHexTerrainFrameDebugView>();
            }
        }

        private Vector3 GetPlacementOrigin()
        {
            return terrainFrame == null ? transform.position : terrainFrame.transform.position;
        }

        private bool TryGroundPosition(Vector3 candidatePosition, out Vector3 groundedPosition)
        {
            Vector3 rayStart = new Vector3(
                candidatePosition.x,
                candidatePosition.y + raycastStartHeightMeters,
                candidatePosition.z);

            RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, raycastDistanceMeters);
            float nearestTerrainDistance = float.PositiveInfinity;
            bool foundTerrain = false;
            groundedPosition = candidatePosition;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];

                if (!IsTerrainSurfaceHit(hit) || hit.distance >= nearestTerrainDistance)
                {
                    continue;
                }

                nearestTerrainDistance = hit.distance;
                groundedPosition = hit.point;
                foundTerrain = true;
            }

            return foundTerrain || terrainFrame == null;
        }

        private void SpawnBirchTree(Transform root, Vector3 basePosition, System.Random random, int index)
        {
            float height = Lerp(baseTrunkHeightRangeMeters, random) * Mathf.Max(0.01f, trunkHeightMultiplier);
            float radius = Lerp(trunkRadiusRangeMeters, random);
            float yaw = (float)random.NextDouble() * 360f;
            float leanX = Lerp(-maxLeanDegrees, maxLeanDegrees, random);
            float leanZ = Lerp(-maxLeanDegrees, maxLeanDegrees, random);
            Quaternion rotation = Quaternion.Euler(leanX, yaw, leanZ);

            Transform treeRoot = new GameObject($"Prototype Birch Tree {index:00}").transform;
            treeRoot.SetParent(root, false);
            treeRoot.position = basePosition;
            treeRoot.rotation = rotation;

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = $"Prototype Birch Trunk {index:00}";
            trunk.transform.SetParent(treeRoot, false);
            trunk.transform.localPosition = Vector3.up * (height * 0.5f);
            trunk.transform.localRotation = Quaternion.identity;
            trunk.transform.localScale = new Vector3(radius, height * 0.5f, radius);
            trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;
            RemoveCollider(trunk);

            int bandCount = Mathf.Clamp(
                FixedBandHeights01.Length,
                Mathf.Max(0, minBandCount),
                Mathf.Max(minBandCount, maxBandCount));

            for (int i = 0; i < bandCount; i++)
            {
                SpawnBarkBand(treeRoot, radius, height, index, i);
            }
        }

        private void SpawnBarkBand(
            Transform treeRoot,
            float trunkRadius,
            float trunkHeight,
            int treeIndex,
            int bandIndex)
        {
            float minHeight01 = Mathf.Clamp01(Mathf.Min(lowestBandHeight01, highestBandHeight01));
            float maxHeight01 = Mathf.Clamp01(Mathf.Max(lowestBandHeight01, highestBandHeight01));
            float fixedHeight01 = FixedBandHeights01[bandIndex % FixedBandHeights01.Length];
            float height01 = Mathf.Lerp(minHeight01, maxHeight01, fixedHeight01);
            float bandHeight = Mathf.Lerp(Mathf.Min(bandHeightRangeMeters.x, bandHeightRangeMeters.y), Mathf.Max(bandHeightRangeMeters.x, bandHeightRangeMeters.y), 0.5f);
            float bandRadius = trunkRadius * Mathf.Lerp(Mathf.Min(bandRadiusMultiplierRange.x, bandRadiusMultiplierRange.y), Mathf.Max(bandRadiusMultiplierRange.x, bandRadiusMultiplierRange.y), 0.5f);

            GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            band.name = $"Prototype Birch Bark Band {treeIndex:00}-{bandIndex:00}";
            band.transform.SetParent(treeRoot, false);
            band.transform.localPosition = Vector3.up * (trunkHeight * height01);
            band.transform.localRotation = Quaternion.identity;
            band.transform.localScale = new Vector3(bandRadius, bandHeight * 0.5f, bandRadius);
            band.GetComponent<Renderer>().sharedMaterial = barkBandMaterial;
            RemoveCollider(band);
        }

        private void EnsureMaterials()
        {
            trunkMaterial = trunkMaterial == null ? CreateMaterial("Prototype Birch Pale Trunk Material", trunkColor) : trunkMaterial;
            barkBandMaterial = barkBandMaterial == null ? CreateMaterial("Prototype Birch Black Bark Band Material", barkBandColor) : barkBandMaterial;
            trunkMaterial.color = trunkColor;
            barkBandMaterial.color = barkBandColor;
        }

        private void ClearTrees()
        {
            Transform existingRoot = transform.Find(TreeRootName);

            if (existingRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existingRoot.gameObject);
            }
            else
            {
                DestroyImmediate(existingRoot.gameObject);
            }
        }

        private static Vector2 RandomPointInCircle(System.Random random, float radius)
        {
            float angle = (float)(random.NextDouble() * Mathf.PI * 2.0);
            float distance = Mathf.Sqrt((float)random.NextDouble()) * Mathf.Max(0f, radius);
            return new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);
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

        private static bool IsTerrainSurfaceHit(RaycastHit hit)
        {
            if (hit.collider == null)
            {
                return false;
            }

            return hit.collider.gameObject.name.Contains("Terrain Surface");
        }

        private static float Lerp(Vector2 range, System.Random random)
        {
            return Lerp(range.x, range.y, random);
        }

        private static float Lerp(float min, float max, System.Random random)
        {
            return Mathf.Lerp(Mathf.Min(min, max), Mathf.Max(min, max), (float)random.NextDouble());
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Material material = new Material(FindShader("Universal Render Pipeline/Lit"));
            material.name = name;
            material.color = color;
            return material;
        }

        private static Shader FindShader(string preferredShader)
        {
            Shader shader = Shader.Find(preferredShader);

            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Standard");

            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Sprites/Default");
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
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }
    }
}
