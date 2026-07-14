using LostForest.Phase2.Tiles;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.DebugTools
{
    [ExecuteAlways]
    public sealed class TileConstructionDebugRunner : MonoBehaviour
    {
        [Header("Frame Source")]
        [SerializeField] private int testRow = 13;
        [SerializeField] private int testColumn = 13;
        [SerializeField] private int testTileId = FrameSettings.PlayerHomeTileId;
        [SerializeField] private FieldSlotRole testSlotRole = FieldSlotRole.PlayerHomeSpawn;
        [SerializeField] private int orientationIndex = 2;
        [SerializeField] private float hexFlatToFlatMeters = 100f;

        [Header("Debug Output")]
        [SerializeField] private bool rebuildOnStart = true;
        [SerializeField] private bool logReportOnRebuild = true;
        [SerializeField] private bool showFrameTerrainAnchors = true;
        [SerializeField] private bool showRotatedContentAnchors = true;
        [SerializeField] private float markerLift = 0.35f;
        [SerializeField] private float frameMarkerSize = 1.15f;
        [SerializeField] private float contentMarkerSize = 1.85f;
        [SerializeField] private float labelSize = 2.4f;

        [Header("Colors")]
        [SerializeField] private Color frameAnchorColor = new Color(0.15f, 0.65f, 1f, 1f);
        [SerializeField] private Color contentAnchorColor = new Color(1f, 0.62f, 0.13f, 1f);
        [SerializeField] private Color propAnchorColor = new Color(1f, 0.2f, 0.25f, 1f);
        [SerializeField] private Color centerColor = new Color(1f, 0.95f, 0.25f, 1f);

        private const float SqrtThree = 1.7320508f;

        public float HexOuterRadiusMeters => Mathf.Max(1f, hexFlatToFlatMeters) / SqrtThree;

        private void Start()
        {
            if (rebuildOnStart)
            {
                Rebuild();
            }
        }

        [ContextMenu("Rebuild Tile Construction Debug")]
        public void Rebuild()
        {
            ClearChildren();

            FieldSlotData slot = CreateTestSlot();
            TileDefinitionRegistry registry = new TileDefinitionRegistry(HexOuterRadiusMeters);
            TileDefinition definition = registry.GetDefinition(slot.TileId);
            TileInstance instance = new TileInstance(definition, slot);

            Material frameMaterial = CreateMaterial("Frame Terrain Anchor Material", frameAnchorColor);
            Material contentMaterial = CreateMaterial("Tile Content Anchor Material", contentAnchorColor);
            Material propMaterial = CreateMaterial("Tile Prop Anchor Material", propAnchorColor);
            Material centerMaterial = CreateMaterial("Tile Center Anchor Material", centerColor);

            Transform frameRoot = new GameObject("Frame-owned fixed terrain anchors").transform;
            frameRoot.SetParent(transform, false);

            Transform tileRoot = new GameObject($"Dropped Tile {definition.TileIdLabel} content anchors").transform;
            tileRoot.SetParent(transform, false);

            if (showFrameTerrainAnchors)
            {
                BuildFrameTerrainAnchorMarkers(frameRoot, slot.WorldCenter, frameMaterial, centerMaterial);
            }

            if (showRotatedContentAnchors)
            {
                BuildTileContentAnchorMarkers(tileRoot, definition, instance, contentMaterial, propMaterial, centerMaterial);
            }

            CreateLabel(
                transform,
                "Tile Construction Contract Label",
                $"DEV ONLY Tile Construction Test\nSlot {slot.Address} fixed at {slot.WorldCenter}\nTile {definition.TileIdLabel} dropped with orientation {instance.OrientationIndex} / {instance.OrientationDegrees:0} deg\nBlue = Frame terrain anchors, Orange/Red = rotated Tile content anchors",
                slot.WorldCenter + Vector3.up * 5f,
                Color.white,
                labelSize);

            if (logReportOnRebuild)
            {
                UnityEngine.Debug.Log(BuildReport(slot, definition, instance, registry), this);
            }
        }

        private FieldSlotData CreateTestSlot()
        {
            int row = Mathf.Max(0, testRow);
            int column = Mathf.Max(0, testColumn);
            Vector2Int axial = HexFrameMath.OffsetToAxial(row, column);
            Vector3 worldCenter = HexFrameMath.GetFlatTopHexCenter(row, column, HexOuterRadiusMeters);

            return new FieldSlotData(
                HexFrameMath.GetAddress(row, column),
                row,
                column,
                axial,
                worldCenter,
                Mathf.Max(0, testTileId),
                Mathf.Clamp(orientationIndex, 0, 5),
                testSlotRole);
        }

        private void BuildFrameTerrainAnchorMarkers(Transform parent, Vector3 center, Material frameMaterial, Material centerMaterial)
        {
            Vector3[] corners = TileHexAnchorMath.GetFlatTopCorners(center, HexOuterRadiusMeters);
            Vector3[] edgeMidpoints = TileHexAnchorMath.GetEdgeMidpoints(corners);
            Vector3[] innerPoints = TileHexAnchorMath.GetInnerPoints(center, corners);

            CreateMarker(parent, "Frame.Center", center, centerMaterial, frameMarkerSize * 1.4f);

            for (int i = 0; i < 6; i++)
            {
                CreateMarker(parent, $"Frame.V{i} fixed", corners[i], frameMaterial, frameMarkerSize);
                CreateMarker(parent, $"Frame.E{i} fixed", edgeMidpoints[i], frameMaterial, frameMarkerSize);
                CreateMarker(parent, $"Frame.I{i} fixed", innerPoints[i], frameMaterial, frameMarkerSize * 0.9f);
            }
        }

        private void BuildTileContentAnchorMarkers(
            Transform parent,
            TileDefinition definition,
            TileInstance instance,
            Material contentMaterial,
            Material propMaterial,
            Material centerMaterial)
        {
            foreach (TileAnchor anchor in definition.Anchors.AllContentAnchors)
            {
                Vector3 position = instance.GetPlacedContentAnchorPosition(definition, anchor);
                Material material = anchor.Kind == TileAnchorKind.Prop ? propMaterial : contentMaterial;

                if (anchor.Kind == TileAnchorKind.Center)
                {
                    material = centerMaterial;
                }

                CreateMarker(parent, $"{anchor.Id} rotated", position, material, anchor.Kind == TileAnchorKind.Prop ? contentMarkerSize * 1.25f : contentMarkerSize);
            }
        }

        private string BuildReport(FieldSlotData slot, TileDefinition definition, TileInstance instance, TileDefinitionRegistry registry)
        {
            return string.Join(
                "\n",
                "Lost Forest Phase 2 Tile Construction Debug",
                instance.BuildDebugSummary(),
                $"Definition source=TileDefinitionRegistry, DefinitionsLoaded={registry.Definitions.Count}",
                $"Definition Role={definition.ReservedRole}, RuneEligible={definition.RuneEligible}, ContentSupportsRotation={definition.ContentSupportsRotation}",
                $"Frame/Slot owns address={slot.Address}, axial=({slot.AxialQ},{slot.AxialR}), world={slot.WorldCenter}",
                "Terrain anchors are rebuilt from the Slot center without applying Tile orientation.",
                "Tile content anchors are local to the Tile and are rotated around the Slot center.");
        }

        private void CreateMarker(Transform parent, string name, Vector3 position, Material material, float size)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position + Vector3.up * markerLift;
            marker.transform.localScale = Vector3.one * size;
            marker.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void CreateLabel(Transform parent, string name, string text, Vector3 position, Color color, float size)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = size;
            textMesh.fontSize = 32;
            textMesh.color = color;
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
