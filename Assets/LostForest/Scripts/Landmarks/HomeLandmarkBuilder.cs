using UnityEngine;
using LostForest.Phase2.World;

namespace LostForest.Phase2.Landmarks
{
    public sealed class HomeLandmarkBuilder : MonoBehaviour
    {
        [SerializeField] private SevenHexTerrainFrameDebugView terrainFrame;
        [SerializeField] private HomeRegionDefinition homeRegion;
        [SerializeField] private bool rebuildOnStart = true;
        [SerializeField] private bool rebuildMissingFrameData = true;
        [SerializeField] private bool logPlacement = true;
        [Tooltip("Primitive stone roots are embedded downward by this amount after terrain sampling.")]
        [SerializeField] private float surfaceLiftMeters = 0.08f;
        [SerializeField] private Color stoneColor = Color.black;
        [SerializeField] private GameObject stonePrefab;

        private TerrainSlotData homeSlot;
        private Vector3 anchorWorldPosition;
        private bool hasPlacement;

        public TerrainSlotData HomeSlot => homeSlot;
        public Vector3 AnchorWorldPosition => anchorWorldPosition;
        public bool HasPlacement => hasPlacement;

        public void SetTerrainFrame(SevenHexTerrainFrameDebugView newTerrainFrame)
        {
            terrainFrame = newTerrainFrame;
        }

        public void SetHomeRegion(HomeRegionDefinition newHomeRegion)
        {
            homeRegion = newHomeRegion;
        }

        public bool TryGetHomeAnchorWorldPosition(out Vector3 worldPosition)
        {
            if (hasPlacement)
            {
                worldPosition = anchorWorldPosition;
                return true;
            }

            if (TryResolveHomeSlot(out TerrainSlotData resolvedHomeSlot))
            {
                homeSlot = resolvedHomeSlot;
                anchorWorldPosition = GetGroundedSlotCenter(homeSlot, CreateSurfaceSampler());
                worldPosition = anchorWorldPosition;
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        [ContextMenu("Rebuild Home Landmark")]
        public void RebuildLandmark()
        {
            TryRebuildLandmark();
        }

        public bool TryRebuildLandmark()
        {
            DiscoverSceneReferences();
            ClearChildren();

            homeSlot = null;
            anchorWorldPosition = Vector3.zero;
            hasPlacement = false;

            if (!TryResolveHomeSlot(out TerrainSlotData resolvedHomeSlot))
            {
                LogPlacement(false, null, Vector3.zero);
                return false;
            }

            homeSlot = resolvedHomeSlot;
            TerrainSurfaceSampler surfaceSampler = CreateSurfaceSampler();
            anchorWorldPosition = GetGroundedSlotCenter(homeSlot, surfaceSampler);
            transform.position = anchorWorldPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            Material stoneMaterial = CreateStoneMaterial();
            surfaceSampler?.ResetStats();
            int groundedStoneCount = 0;
            int skippedStoneCount = 0;

            CreateStone("Home Black Cylinder 01 Tall", new Vector3(0f, 0f, 3.33f), 0.63f, 6f, new Vector3(-1f, 4f, 0f), stoneMaterial, surfaceSampler, ref groundedStoneCount, ref skippedStoneCount);
            CreateStone("Home Black Cylinder 02 West", new Vector3(-1.83f, 0f, 2.33f), 0.52f, 4.67f, new Vector3(0.75f, -10f, -1f), stoneMaterial, surfaceSampler, ref groundedStoneCount, ref skippedStoneCount);
            CreateStone("Home Black Cylinder 03 East", new Vector3(1.83f, 0f, 2.5f), 0.55f, 5.17f, new Vector3(-0.75f, 12f, 1f), stoneMaterial, surfaceSampler, ref groundedStoneCount, ref skippedStoneCount);

            hasPlacement = groundedStoneCount > 0;
            LogPlacement(hasPlacement, homeSlot, anchorWorldPosition, groundedStoneCount, skippedStoneCount, surfaceSampler);
            return hasPlacement;
        }

        private void Reset()
        {
            DiscoverSceneReferences();
        }

        private void Start()
        {
            if (rebuildOnStart)
            {
                TryRebuildLandmark();
            }
        }

        private void DiscoverSceneReferences()
        {
            if (terrainFrame == null)
            {
                terrainFrame = FindAnyObjectByType<SevenHexTerrainFrameDebugView>();
            }

            if (homeRegion == null && terrainFrame != null)
            {
                homeRegion = terrainFrame.GetComponent<HomeRegionDefinition>();
            }

            if (homeRegion == null)
            {
                homeRegion = FindAnyObjectByType<HomeRegionDefinition>();
            }

            if (terrainFrame == null && homeRegion != null)
            {
                terrainFrame = homeRegion.GetComponent<SevenHexTerrainFrameDebugView>();
            }
        }

        private bool TryResolveHomeSlot(out TerrainSlotData resolvedHomeSlot)
        {
            resolvedHomeSlot = null;

            if (homeRegion == null)
            {
                return false;
            }

            TerrainFrameData frameData = terrainFrame == null ? null : terrainFrame.TerrainFrameData;

            if ((frameData == null || frameData.SlotCount == 0) && terrainFrame != null && rebuildMissingFrameData)
            {
                terrainFrame.Rebuild();
                frameData = terrainFrame.TerrainFrameData;
            }

            if (frameData != null && frameData.SlotCount > 0)
            {
                return homeRegion.TryGetHomeSlot(frameData, out resolvedHomeSlot);
            }

            return homeRegion.TryGetHomeSlot(out resolvedHomeSlot);
        }

        private void CreateStone(
            string name,
            Vector3 localGroundPosition,
            float radius,
            float height,
            Vector3 localEulerAngles,
            Material stoneMaterial,
            TerrainSurfaceSampler surfaceSampler,
            ref int groundedStoneCount,
            ref int skippedStoneCount)
        {
            Vector3 samplePosition = anchorWorldPosition + new Vector3(localGroundPosition.x, 0f, localGroundPosition.z);

            if (surfaceSampler == null || !surfaceSampler.TrySample(samplePosition, out TerrainSurfaceSample surfaceSample))
            {
                skippedStoneCount++;
                return;
            }

            GameObject stoneRoot = new GameObject($"{name} Grounded Root");
            stoneRoot.transform.position = surfaceSample.Position - Vector3.up * GetStoneEmbedMeters();
            stoneRoot.transform.rotation = Quaternion.identity;
            stoneRoot.transform.localScale = Vector3.one;
            stoneRoot.transform.SetParent(transform, true);

            bool usesPrimitiveStone = stonePrefab == null;
            GameObject stone = usesPrimitiveStone ? GameObject.CreatePrimitive(PrimitiveType.Cylinder) : Instantiate(stonePrefab);
            stone.name = name;
            stone.transform.SetParent(stoneRoot.transform, false);
            Quaternion localRotation = Quaternion.Euler(localEulerAngles);
            stone.transform.localPosition = localRotation * (Vector3.up * (height * 0.5f));
            stone.transform.localRotation = localRotation;
            stone.transform.localScale = usesPrimitiveStone
                ? new Vector3(radius * 2f, height * 0.5f, radius * 2f)
                : new Vector3(radius * 2f, height, radius * 2f);

            if (usesPrimitiveStone)
            {
                Renderer renderer = stone.GetComponent<Renderer>();

                if (renderer != null)
                {
                    renderer.sharedMaterial = stoneMaterial;
                }
            }

            groundedStoneCount++;
        }

        private void LogPlacement(
            bool succeeded,
            TerrainSlotData slot,
            Vector3 worldPosition,
            int groundedStoneCount = 0,
            int skippedStoneCount = 0,
            TerrainSurfaceSampler surfaceSampler = null)
        {
            if (!logPlacement)
            {
                return;
            }

            string slotLabel = slot == null ? "None" : slot.Label;
            Vector2Int axial = Vector2Int.zero;

            if (slot != null)
            {
                axial = slot.AxialCoordinate;
            }
            else if (homeRegion != null)
            {
                axial = homeRegion.HomeAxialCoordinate;
            }

            string groundingStats = surfaceSampler == null ? "Home stone grounding: Raycast=0, Fallback=0, Failed=0" : surfaceSampler.BuildStatsSummary("Home stone grounding");
            Debug.Log($"Lost Forest Home Landmark placement: Succeeded={succeeded}, Slot={slotLabel}, Axial=({axial.x}, {axial.y}), World=({worldPosition.x:0.00}, {worldPosition.y:0.00}, {worldPosition.z:0.00}), GroundedStones={groundedStoneCount}, SkippedStones={skippedStoneCount}, {groundingStats}");
        }

        private static Vector3 GetSlotCenter(TerrainSlotData slot)
        {
            return slot.CenterPoint == null ? slot.WorldCenter : slot.CenterPoint.Position;
        }

        private TerrainSurfaceSampler CreateSurfaceSampler()
        {
            return terrainFrame == null ? null : terrainFrame.CreateTerrainSurfaceSampler();
        }

        private static Vector3 GetGroundedSlotCenter(TerrainSlotData slot, TerrainSurfaceSampler surfaceSampler)
        {
            Vector3 slotCenter = GetSlotCenter(slot);

            if (surfaceSampler != null && surfaceSampler.TrySample(slotCenter, out TerrainSurfaceSample surfaceSample))
            {
                return surfaceSample.Position;
            }

            return slotCenter;
        }

        private float GetStoneEmbedMeters()
        {
            return Mathf.Clamp(surfaceLiftMeters, 0f, 0.25f);
        }

        private Material CreateStoneMaterial()
        {
            Material material = new Material(FindShader("Universal Render Pipeline/Lit"));
            material.name = "Home Landmark High Black Cylinder Material";
            material.color = stoneColor;
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

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
