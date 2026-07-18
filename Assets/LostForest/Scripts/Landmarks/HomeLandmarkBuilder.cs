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
                anchorWorldPosition = GetSlotCenter(homeSlot);
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
            anchorWorldPosition = GetSlotCenter(homeSlot);
            transform.position = anchorWorldPosition + Vector3.up * Mathf.Max(0f, surfaceLiftMeters);
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            Material stoneMaterial = CreateStoneMaterial();
            CreateStone("Home Black Cylinder 01 Tall", new Vector3(0f, 0f, 3.33f), 0.63f, 6f, new Vector3(-1f, 4f, 0f), stoneMaterial);
            CreateStone("Home Black Cylinder 02 West", new Vector3(-1.83f, 0f, 2.33f), 0.52f, 4.67f, new Vector3(0.75f, -10f, -1f), stoneMaterial);
            CreateStone("Home Black Cylinder 03 East", new Vector3(1.83f, 0f, 2.5f), 0.55f, 5.17f, new Vector3(-0.75f, 12f, 1f), stoneMaterial);

            hasPlacement = true;
            LogPlacement(true, homeSlot, anchorWorldPosition);
            return true;
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

        private void CreateStone(string name, Vector3 localGroundPosition, float radius, float height, Vector3 localEulerAngles, Material stoneMaterial)
        {
            bool usesPrimitiveStone = stonePrefab == null;
            GameObject stone = usesPrimitiveStone ? GameObject.CreatePrimitive(PrimitiveType.Cylinder) : Instantiate(stonePrefab);
            stone.name = name;
            stone.transform.SetParent(transform, false);
            stone.transform.localPosition = localGroundPosition + Vector3.up * (height * 0.5f);
            stone.transform.localRotation = Quaternion.Euler(localEulerAngles);
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
        }

        private void LogPlacement(bool succeeded, TerrainSlotData slot, Vector3 worldPosition)
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

            Debug.Log($"Lost Forest Home Landmark placement: Succeeded={succeeded}, Slot={slotLabel}, Axial=({axial.x}, {axial.y}), World=({worldPosition.x:0.00}, {worldPosition.y:0.00}, {worldPosition.z:0.00})");
        }

        private static Vector3 GetSlotCenter(TerrainSlotData slot)
        {
            return slot.CenterPoint == null ? slot.WorldCenter : slot.CenterPoint.Position;
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
