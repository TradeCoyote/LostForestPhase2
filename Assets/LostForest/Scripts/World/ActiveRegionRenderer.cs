using System.Collections.Generic;
using LostForest.Phase2.Landmarks;
using LostForest.Phase2.Runes;
using LostForest.Phase2.Tiles;
using UnityEngine;

namespace LostForest.Phase2.World
{
    public sealed class ActiveRegionRenderer : MonoBehaviour
    {
        private const float SqrtThree = 1.7320508f;
        private const string SlotRootName = "Rendered Active Slots";

        [Header("Active Window")]
        [SerializeField, Range(0, 3)] private int activeRadius = 1;
        [SerializeField] private bool logRenderUpdates = true;

        [Header("Terrain Surface")]
        [SerializeField] private Transform slotRoot;
        [SerializeField] private Color terrainSurfaceColor = new Color(0.92f, 0.96f, 1f, 1f);
        [SerializeField] private float terrainSurfaceLift = -0.08f;
        [SerializeField] private bool addTerrainMeshColliders = true;
        [SerializeField] private bool markTerrainSurfaceStatic;

        [Header("Terrain Heights")]
        [SerializeField] private int heightSeed = 4242;
        [SerializeField] private float heightAmplitudeMeters = 42f;
        [SerializeField] private float visualHeightMultiplier = 1.35f;
        [SerializeField] private float broadHeightScale = 0.0034f;
        [SerializeField] private float noiseHeightScale = 0.0022f;

        [Header("Placeholder Forest Content")]
        [SerializeField] private bool showPlaceholderForestContent = true;
        [SerializeField] private int placeholderContentSeed = 2026;

        [Header("Home Landmark")]
        [SerializeField] private bool showHomeStones = true;
        [SerializeField] private float homeStoneEmbedMeters = 0.08f;

        [Header("Rune Prototype")]
        [SerializeField] private RuneManager runeManager;

        [Header("Colors")]
        [SerializeField] private Color placeholderTreeTrunkColor = new Color(0.91f, 0.94f, 0.91f, 1f);
        [SerializeField] private Color placeholderTreeSnowCapColor = new Color(0.88f, 0.93f, 0.95f, 1f);
        [SerializeField] private Color placeholderHomeTreeColor = new Color(0.93f, 0.95f, 0.90f, 1f);
        [SerializeField] private Color placeholderThreatTreeColor = new Color(0.13f, 0.14f, 0.15f, 1f);
        [SerializeField] private Color placeholderBarkBandColor = new Color(0.045f, 0.047f, 0.045f, 1f);
        [SerializeField] private Color homeStoneColor = Color.black;

        private readonly Dictionary<string, RenderedSlotInstance> renderedSlots = new Dictionary<string, RenderedSlotInstance>();

        private FieldData fieldData;
        private float hexOuterRadiusMeters = 45f;
        private TileDefinitionRegistry tileDefinitionRegistry;
        private TileContentSpawner tileContentSpawner;
        private Material terrainMaterial;
        private Material trunkMaterial;
        private Material crownMaterial;
        private Material homeTrunkMaterial;
        private Material threatTrunkMaterial;
        private Material barkBandMaterial;
        private Material homeStoneMaterial;
        private FieldSlotData homeSlot;

        public int ActiveRadius => Mathf.Max(0, activeRadius);
        public int ActiveRenderedSlotCount => renderedSlots.Count;
        public FieldSlotData CurrentCenterSlot { get; private set; }
        public IReadOnlyDictionary<string, RenderedSlotInstance> RenderedSlots => renderedSlots;

        public void SetRuneManager(RuneManager newRuneManager)
        {
            runeManager = newRuneManager;
            tileContentSpawner = null;
        }

        public void Configure(FieldData newFieldData, float newHexOuterRadiusMeters)
        {
            fieldData = newFieldData;
            hexOuterRadiusMeters = Mathf.Max(1f, newHexOuterRadiusMeters);
            tileDefinitionRegistry = new TileDefinitionRegistry(hexOuterRadiusMeters);
            tileContentSpawner = null;
            homeSlot = FindHomeSlot(fieldData);
        }

        public void SetActiveRadius(int newActiveRadius)
        {
            activeRadius = Mathf.Clamp(newActiveRadius, 0, 3);
        }

        public void ApplyBroadSlopeTerrainDefaults()
        {
            heightAmplitudeMeters = 42f;
            visualHeightMultiplier = 1.35f;
            broadHeightScale = 0.0034f;
            noiseHeightScale = 0.0022f;
        }

        public void RenderAround(FieldSlotData centerSlot)
        {
            if (fieldData == null || centerSlot == null)
            {
                return;
            }

            CurrentCenterSlot = centerSlot;
            EnsureMaterials();
            EnsureSlotRoot();

            Dictionary<string, int> desiredDistancesByAddress = BuildDesiredDistances(centerSlot);
            List<string> staleAddresses = new List<string>();

            foreach (KeyValuePair<string, RenderedSlotInstance> entry in renderedSlots)
            {
                if (!desiredDistancesByAddress.ContainsKey(entry.Key))
                {
                    staleAddresses.Add(entry.Key);
                }
            }

            for (int i = 0; i < staleAddresses.Count; i++)
            {
                RemoveRenderedSlot(staleAddresses[i]);
            }

            int addedCount = 0;

            foreach (KeyValuePair<string, int> desiredSlot in desiredDistancesByAddress)
            {
                if (renderedSlots.TryGetValue(desiredSlot.Key, out RenderedSlotInstance existingInstance))
                {
                    existingInstance.SetDistanceBand(desiredSlot.Value);
                    continue;
                }

                FieldSlotData fieldSlot = fieldData.GetSlot(desiredSlot.Key);

                if (fieldSlot == null)
                {
                    continue;
                }

                RenderedSlotInstance instance = CreateRenderedSlot(fieldSlot, desiredSlot.Value);
                renderedSlots[fieldSlot.Address] = instance;
                addedCount++;
            }

            if (logRenderUpdates)
            {
                Debug.Log(BuildRenderUpdateLog(centerSlot, addedCount, staleAddresses.Count));
            }
        }

        public bool TryGetRenderedSlot(FieldSlotData fieldSlot, out RenderedSlotInstance instance)
        {
            instance = null;

            if (fieldSlot == null)
            {
                return false;
            }

            return renderedSlots.TryGetValue(fieldSlot.Address, out instance);
        }

        public bool TrySampleSlotSurface(FieldSlotData fieldSlot, Vector3 worldXzPosition, out TerrainSurfaceSample sample)
        {
            if (TryGetRenderedSlot(fieldSlot, out RenderedSlotInstance instance))
            {
                return instance.TrySampleSurface(worldXzPosition, out sample);
            }

            sample = default;
            return false;
        }

        public bool TrySampleTerrainElevation(FieldSlotData fieldSlot, Vector3 worldXzPosition, out TerrainElevationSample elevationSample)
        {
            elevationSample = default;

            if (!TrySampleSlotSurface(fieldSlot, worldXzPosition, out TerrainSurfaceSample surfaceSample))
            {
                return false;
            }

            TerrainFrameSettings settings = CreateTerrainFrameSettings();
            FieldSlotData resolvedHomeSlot = homeSlot ?? FindHomeSlot(fieldData);
            float homeLogicalElevation = resolvedHomeSlot == null
                ? surfaceSample.GetLogicalElevationMeters(settings, terrainSurfaceLift)
                : TerrainFrameGenerator.GetLogicalHeightAtWorldPosition(resolvedHomeSlot.WorldCenter, settings);
            int hexDistanceFromHome = GetHexDistance(resolvedHomeSlot, fieldSlot);
            float planarDistanceFromHome = GetPlanarDistance(resolvedHomeSlot, worldXzPosition);
            Vector3 planarDirectionFromHome = GetPlanarDirection(resolvedHomeSlot, worldXzPosition);

            elevationSample = TerrainElevationSample.FromSurfaceSample(
                surfaceSample,
                settings,
                terrainSurfaceLift,
                homeLogicalElevation,
                hexDistanceFromHome,
                planarDistanceFromHome,
                planarDirectionFromHome);

            return true;
        }

        public void ClearRenderedSlots()
        {
            List<string> addresses = new List<string>(renderedSlots.Keys);

            for (int i = 0; i < addresses.Count; i++)
            {
                RemoveRenderedSlot(addresses[i]);
            }

            CurrentCenterSlot = null;
        }

        private Dictionary<string, int> BuildDesiredDistances(FieldSlotData centerSlot)
        {
            Dictionary<string, int> desiredDistances = new Dictionary<string, int>();
            int radius = ActiveRadius;

            for (int i = 0; i < fieldData.Slots.Count; i++)
            {
                FieldSlotData fieldSlot = fieldData.Slots[i];

                if (fieldSlot == null)
                {
                    continue;
                }

                int distance = HexFrameMath.GetHexDistance(centerSlot.AxialCoordinate, fieldSlot.AxialCoordinate);

                if (distance <= radius)
                {
                    desiredDistances[fieldSlot.Address] = distance;
                }
            }

            return desiredDistances;
        }

        private RenderedSlotInstance CreateRenderedSlot(FieldSlotData fieldSlot, int distanceFromCenter)
        {
            GameObject slotObject = new GameObject($"Rendered Slot {fieldSlot.Address} Tile {fieldSlot.TileIdLabel}");
            slotObject.transform.SetParent(slotRoot, false);

            TerrainFrameData terrainFrameData = TerrainFrameGenerator.GenerateForFieldSlots(CreateTerrainFrameSettings(), new[] { fieldSlot });
            TerrainSlotData terrainSlot = terrainFrameData.SlotCount == 0 ? null : terrainFrameData.Slots[0];
            HexTerrainMeshData terrainMeshData = terrainSlot == null
                ? new HexTerrainMeshData(slotObject.transform, $"Slot {fieldSlot.Address} Empty Terrain Mesh")
                : HexTerrainMeshBuilder.BuildSlotSurface(terrainSlot, slotObject.transform, terrainMaterial, CreateTerrainMeshSettings());
            TerrainSurfaceSampler surfaceSampler = new TerrainSurfaceSampler(terrainFrameData, terrainMeshData);

            if (terrainSlot != null && showPlaceholderForestContent)
            {
                SpawnPlaceholderContent(slotObject.transform, fieldSlot, terrainSlot, surfaceSampler);
            }

            if (terrainSlot != null && showHomeStones && IsHomeSlot(fieldSlot))
            {
                bool placedHomeStones = HomeStoneLandmarkRenderer.SpawnHomeStones(
                    slotObject.transform,
                    terrainSlot,
                    surfaceSampler,
                    homeStoneMaterial,
                    homeStoneEmbedMeters,
                    out int groundedStoneCount,
                    out int skippedStoneCount,
                    runeManager);

                Debug.Log($"Lost Forest Home Landmark active slot render: Succeeded={placedHomeStones}, Slot={fieldSlot.Address}, Tile={fieldSlot.TileIdLabel}, GroundedStones={groundedStoneCount}, SkippedStones={skippedStoneCount}");
            }

            return new RenderedSlotInstance(fieldSlot, terrainFrameData, terrainSlot, slotObject, terrainMeshData, surfaceSampler, distanceFromCenter);
        }

        private void SpawnPlaceholderContent(
            Transform parent,
            FieldSlotData fieldSlot,
            TerrainSlotData terrainSlot,
            TerrainSurfaceSampler surfaceSampler)
        {
            if (tileDefinitionRegistry == null)
            {
                tileDefinitionRegistry = new TileDefinitionRegistry(hexOuterRadiusMeters);
            }

            if (tileContentSpawner == null)
            {
                tileContentSpawner = new TileContentSpawner(trunkMaterial, crownMaterial, homeTrunkMaterial, threatTrunkMaterial, barkBandMaterial);
            }

            Transform contentRoot = new GameObject($"Slot {fieldSlot.Address} Placeholder Tile Content").transform;
            contentRoot.SetParent(parent, false);

            TileDefinition definition = tileDefinitionRegistry.GetDefinition(fieldSlot.TileId);
            int spawnedCount = tileContentSpawner.SpawnForestStandIns(
                contentRoot,
                terrainSlot,
                definition,
                surfaceSampler,
                GetContentWorldSeed(),
                fieldSlot.OrientationIndex,
                hexOuterRadiusMeters,
                runeManager,
                fieldSlot,
                out int groundedCount,
                out int skippedCount);

            if (spawnedCount == 0 && groundedCount == 0 && skippedCount == 0)
            {
                DestroyEmptyContentRoot(contentRoot.gameObject);
            }
        }

        private void RemoveRenderedSlot(string address)
        {
            if (!renderedSlots.TryGetValue(address, out RenderedSlotInstance instance))
            {
                return;
            }

            renderedSlots.Remove(address);
            instance.Destroy();
        }

        private TerrainFrameSettings CreateTerrainFrameSettings()
        {
            return new TerrainFrameSettings(
                hexOuterRadiusMeters * SqrtThree,
                heightSeed,
                heightAmplitudeMeters,
                visualHeightMultiplier,
                broadHeightScale,
                noiseHeightScale,
                GetHomeWorldCenter());
        }

        private HexTerrainMeshSettings CreateTerrainMeshSettings()
        {
            return new HexTerrainMeshSettings(
                terrainSurfaceLift,
                addTerrainMeshColliders,
                markTerrainSurfaceStatic,
                false,
                "Active Grid Terrain Mesh Group",
                "Slot",
                "Snow Terrain Surface",
                "Terrain Surface Mesh",
                false);
        }

        private int GetContentWorldSeed()
        {
            unchecked
            {
                return ((fieldData == null ? 0 : fieldData.Seed) * 397) ^ placeholderContentSeed;
            }
        }

        private bool IsHomeSlot(FieldSlotData fieldSlot)
        {
            return fieldSlot.Role == FieldSlotRole.PlayerHomeSpawn || fieldSlot.TileId == FrameSettings.PlayerHomeTileId;
        }

        private FieldSlotData FindHomeSlot(FieldData data)
        {
            if (data == null)
            {
                return null;
            }

            for (int i = 0; i < data.Slots.Count; i++)
            {
                FieldSlotData slot = data.Slots[i];

                if (slot != null && IsHomeSlot(slot))
                {
                    return slot;
                }
            }

            return data.SlotsFilled == 0 ? null : data.Slots[0];
        }

        private Vector3 GetHomeWorldCenter()
        {
            FieldSlotData resolvedHomeSlot = homeSlot ?? FindHomeSlot(fieldData);
            return resolvedHomeSlot == null ? Vector3.zero : resolvedHomeSlot.WorldCenter;
        }

        private static int GetHexDistance(FieldSlotData fromSlot, FieldSlotData toSlot)
        {
            if (fromSlot == null || toSlot == null)
            {
                return -1;
            }

            return HexFrameMath.GetHexDistance(fromSlot.AxialCoordinate, toSlot.AxialCoordinate);
        }

        private static float GetPlanarDistance(FieldSlotData fromSlot, Vector3 worldXzPosition)
        {
            if (fromSlot == null)
            {
                return 0f;
            }

            Vector3 from = fromSlot.WorldCenter;
            float deltaX = worldXzPosition.x - from.x;
            float deltaZ = worldXzPosition.z - from.z;
            return Mathf.Sqrt((deltaX * deltaX) + (deltaZ * deltaZ));
        }

        private static Vector3 GetPlanarDirection(FieldSlotData fromSlot, Vector3 worldXzPosition)
        {
            if (fromSlot == null)
            {
                return Vector3.zero;
            }

            Vector3 from = fromSlot.WorldCenter;
            Vector3 direction = new Vector3(worldXzPosition.x - from.x, 0f, worldXzPosition.z - from.z);
            return direction.sqrMagnitude <= 0.0001f ? Vector3.zero : direction.normalized;
        }

        private string BuildRenderUpdateLog(FieldSlotData centerSlot, int addedCount, int removedCount)
        {
            return $"Lost Forest Active Grid Render: Center={centerSlot.Address}, Row={centerSlot.RowIndex}, Column={centerSlot.ColumnIndex}, Axial=({centerSlot.AxialQ}, {centerSlot.AxialR}), Tile={centerSlot.TileIdLabel}, Orientation=O{centerSlot.OrientationIndex}/{centerSlot.OrientationDegrees:0}deg, Radius={ActiveRadius}, ActiveSlots={renderedSlots.Count}, Added={addedCount}, Removed={removedCount}";
        }

        private void EnsureSlotRoot()
        {
            if (slotRoot != null)
            {
                return;
            }

            Transform existingRoot = transform.Find(SlotRootName);

            if (existingRoot != null)
            {
                slotRoot = existingRoot;
                return;
            }

            GameObject rootObject = new GameObject(SlotRootName);
            rootObject.transform.SetParent(transform, false);
            slotRoot = rootObject.transform;
        }

        private void EnsureMaterials()
        {
            terrainMaterial = terrainMaterial == null ? CreateMaterial("Active Grid Snow Terrain Material", terrainSurfaceColor) : terrainMaterial;
            trunkMaterial = trunkMaterial == null ? CreateMaterial("Active Grid Birch Trunk Material", placeholderTreeTrunkColor) : trunkMaterial;
            crownMaterial = crownMaterial == null ? CreateMaterial("Active Grid Snow Cap Material", placeholderTreeSnowCapColor) : crownMaterial;
            homeTrunkMaterial = homeTrunkMaterial == null ? CreateMaterial("Active Grid Home Trunk Material", placeholderHomeTreeColor) : homeTrunkMaterial;
            threatTrunkMaterial = threatTrunkMaterial == null ? CreateMaterial("Active Grid Threat Trunk Material", placeholderThreatTreeColor) : threatTrunkMaterial;
            barkBandMaterial = barkBandMaterial == null ? CreateMaterial("Active Grid Birch Bark Band Material", placeholderBarkBandColor) : barkBandMaterial;
            homeStoneMaterial = homeStoneMaterial == null ? CreateMaterial("Active Grid Home Stone Material", homeStoneColor) : homeStoneMaterial;
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

        private static void DestroyEmptyContentRoot(GameObject contentRoot)
        {
            if (contentRoot == null || contentRoot.transform.childCount > 0)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(contentRoot);
            }
            else
            {
                Object.DestroyImmediate(contentRoot);
            }
        }
    }
}
