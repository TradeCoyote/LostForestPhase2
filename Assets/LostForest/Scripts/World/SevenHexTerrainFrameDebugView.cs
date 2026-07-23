using System.Collections.Generic;
using System.Text;
using LostForest.Phase2.Tiles;
using UnityEngine;

namespace LostForest.Phase2.World
{
    [ExecuteAlways]
    public sealed class SevenHexTerrainFrameDebugView : MonoBehaviour
    {
        private const float SqrtThree = 1.7320508f;
        private static readonly IReadOnlyDictionary<string, SharedHeightPoint> EmptySharedPoints = new Dictionary<string, SharedHeightPoint>();

        [Header("Scale")]
        [SerializeField] private float hexFlatToFlatMeters = 100f;

        [Header("Terrain Surface")]
        [SerializeField] private bool showTerrainSurface = true;
        [SerializeField] private Color terrainSurfaceColor = new Color(0.92f, 0.96f, 1f, 1f);
        [SerializeField] private float terrainSurfaceLift = -0.08f;
        [SerializeField] private bool addTerrainMeshColliders = true;
        [SerializeField] private bool markTerrainSurfaceStatic = false;

        [Header("Points")]
        [SerializeField] private int heightSeed = 4242;
        [SerializeField] private float heightAmplitudeMeters = 42f;
        [SerializeField] private float visualHeightMultiplier = 1.35f;
        [SerializeField] private float broadHeightScale = 0.0034f;
        [SerializeField] private float noiseHeightScale = 0.0022f;
        [SerializeField] private bool showCenters = true;
        [SerializeField] private bool showVertices = true;
        [SerializeField] private bool showEdgeMidpoints = true;
        [SerializeField] private bool showInnerPoints = true;
        [SerializeField] private float centerPointSize = 2.2f;
        [SerializeField] private float boundaryPointSize = 1.2f;
        [SerializeField] private float innerPointSize = 0.9f;

        [Header("Lines")]
        [SerializeField] private bool showHexOutlines = true;
        [SerializeField] private bool showInteriorLines = true;
        [SerializeField] private float lineWidth = 0.35f;
        [SerializeField] private float debugLineLift = 0.35f;

        [Header("Labels")]
        [SerializeField] private bool showCenterLabels = true;
        [SerializeField] private bool showPointLabels = false;
        [SerializeField] private float labelSize = 2.6f;
        [SerializeField] private float labelLift = 0.2f;

        [Header("Tile Conformity Proof")]
        [SerializeField] private bool showConformingTileAnchors = true;
        [SerializeField, Range(0, 5)] private int conformingTileOrientationIndex = 2;
        [SerializeField] private float conformingAnchorLift = 2f;
        [SerializeField] private float conformingAnchorSize = 2.4f;

        [Header("Placeholder Forest Content")]
        [SerializeField] private bool showPlaceholderForestContent = true;
        [SerializeField] private int placeholderContentSeed = 2026;
        [SerializeField, Range(0, 5)] private int placeholderContentOrientationIndex = 1;
        [SerializeField] private bool showPlaceholderContentLabels = true;

        [Header("Colors")]
        [SerializeField] private Color outlineColor = new Color(0.15f, 0.55f, 1f, 1f);
        [SerializeField] private Color interiorLineColor = new Color(0.55f, 0.85f, 1f, 0.75f);
        [SerializeField] private Color centerColor = new Color(1f, 0.92f, 0.25f, 1f);
        [SerializeField] private Color vertexColor = new Color(1f, 0.22f, 0.18f, 1f);
        [SerializeField] private Color edgeMidpointColor = new Color(0.25f, 1f, 0.55f, 1f);
        [SerializeField] private Color innerPointColor = new Color(0.86f, 0.48f, 1f, 1f);
        [SerializeField] private Color conformingTileAnchorColor = new Color(1f, 0.62f, 0.08f, 1f);
        [SerializeField] private Color placeholderTreeTrunkColor = new Color(0.42f, 0.43f, 0.39f, 1f);
        [SerializeField] private Color placeholderTreeSnowCapColor = new Color(0.88f, 0.93f, 0.95f, 1f);
        [SerializeField] private Color placeholderHomeTreeColor = new Color(0.58f, 0.50f, 0.34f, 1f);
        [SerializeField] private Color placeholderThreatTreeColor = new Color(0.13f, 0.14f, 0.15f, 1f);

        private TerrainFrameData terrainFrameData;
        private HexTerrainMeshData terrainMeshData;

        public float HexFlatToFlatMeters => Mathf.Max(1f, hexFlatToFlatMeters);
        public float HexOuterRadiusMeters => HexFlatToFlatMeters / SqrtThree;
        public TerrainFrameData TerrainFrameData => terrainFrameData;
        public HexTerrainMeshData TerrainMeshData => terrainMeshData;
        public IReadOnlyDictionary<string, SharedHeightPoint> SharedPoints => terrainFrameData == null ? EmptySharedPoints : terrainFrameData.SharedPoints;

        public TerrainSurfaceSampler CreateTerrainSurfaceSampler()
        {
            return new TerrainSurfaceSampler(terrainFrameData, terrainMeshData);
        }

        public void ApplyEarlyWalkThruVisualDefaults()
        {
            showTerrainSurface = true;
            addTerrainMeshColliders = true;

            showCenters = true;
            showVertices = true;
            showEdgeMidpoints = true;
            showInnerPoints = true;
            showHexOutlines = true;
            showInteriorLines = true;

            showCenterLabels = false;
            showPointLabels = false;
            showConformingTileAnchors = false;
            showPlaceholderForestContent = true;
            showPlaceholderContentLabels = false;
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                Rebuild();
            }
        }

        [ContextMenu("Rebuild 7 Hex Terrain Frame")]
        public void Rebuild()
        {
            ClearChildren();
            terrainFrameData = TerrainFrameGenerator.GenerateRadiusOne(CreateTerrainFrameSettings());
            terrainMeshData = new HexTerrainMeshData(transform, "7 Hex Terrain Mesh Debug View");
            terrainMeshData.SetColliderGenerationRequested(showTerrainSurface && addTerrainMeshColliders);

            Material outlineMaterial = CreateLineMaterial("7 Hex Outline Material", outlineColor);
            Material interiorMaterial = CreateLineMaterial("7 Hex Interior Material", interiorLineColor);
            Material terrainMaterial = CreatePointMaterial("7 Hex Snow Terrain Surface Material", terrainSurfaceColor);
            Material centerMaterial = CreatePointMaterial("7 Hex Center Point Material", centerColor);
            Material vertexMaterial = CreatePointMaterial("7 Hex Vertex Point Material", vertexColor);
            Material edgeMaterial = CreatePointMaterial("7 Hex Edge Point Material", edgeMidpointColor);
            Material innerMaterial = CreatePointMaterial("7 Hex Inner Point Material", innerPointColor);
            HexTerrainMeshSettings terrainMeshSettings = CreateTerrainMeshSettings();

            CreatePointMarkers(terrainFrameData, centerMaterial, vertexMaterial, edgeMaterial, innerMaterial);

            for (int i = 0; i < terrainFrameData.Slots.Count; i++)
            {
                terrainMeshData.Append(BuildSlot(terrainFrameData.Slots[i], outlineMaterial, interiorMaterial, terrainMaterial, terrainMeshSettings));
            }

            if (showConformingTileAnchors)
            {
                BuildConformingTileAnchorProof();
            }

            if (showPlaceholderForestContent)
            {
                BuildPlaceholderForestContent();
            }

            Debug.Log(BuildHeightPointReport(terrainFrameData));
        }

        [ContextMenu("Validate Terrain Mesh Colliders")]
        public void ValidateTerrainMeshColliders()
        {
            if (terrainMeshData == null)
            {
                Debug.LogWarning("Lost Forest Phase 2 terrain collider validation could not run because no terrain mesh data exists. Rebuild the 7 hex terrain frame first.");
                return;
            }

            bool validationPassed = terrainMeshData.ValidateColliderReadiness(out string validationMessage);
            string logMessage = $"Lost Forest Phase 2 terrain collider validation: {validationMessage}";

            if (validationPassed)
            {
                Debug.Log(logMessage);
            }
            else
            {
                Debug.LogWarning(logMessage);
            }
        }

        private HexTerrainMeshData BuildSlot(
            TerrainSlotData slot,
            Material outlineMaterial,
            Material interiorMaterial,
            Material terrainMaterial,
            HexTerrainMeshSettings terrainMeshSettings)
        {
            string slotLabel = slot.Label;
            SharedHeightPoint centerPoint = slot.CenterPoint;
            IReadOnlyList<SharedHeightPoint> vertexPoints = slot.VertexPoints;
            IReadOnlyList<SharedHeightPoint> edgePoints = slot.EdgeMidpointPoints;
            IReadOnlyList<SharedHeightPoint> innerHeightPoints = slot.InnerPoints;
            HexTerrainMeshData slotMeshData = null;

            Transform slotRoot = new GameObject($"Slot {slotLabel}").transform;
            slotRoot.SetParent(transform, false);

            if (showTerrainSurface)
            {
                slotMeshData = HexTerrainMeshBuilder.BuildSlotSurface(slot, slotRoot, terrainMaterial, terrainMeshSettings);
            }

            if (showHexOutlines)
            {
                CreatePolyline(slotRoot, $"Slot {slotLabel} Outline", GetLiftedPositions(vertexPoints, debugLineLift), true, outlineMaterial, lineWidth);
            }

            if (showInteriorLines)
            {
                for (int i = 0; i < 6; i++)
                {
                    CreateLine(slotRoot, $"Slot {slotLabel} Center to V{i}", Lift(centerPoint.Position, debugLineLift), Lift(vertexPoints[i].Position, debugLineLift), interiorMaterial, lineWidth * 0.65f);
                    CreateLine(slotRoot, $"Slot {slotLabel} Inner Edge {i}", Lift(innerHeightPoints[i].Position, debugLineLift), Lift(innerHeightPoints[(i + 1) % 6].Position, debugLineLift), interiorMaterial, lineWidth * 0.55f);
                    CreateLine(slotRoot, $"Slot {slotLabel} Edge Mid to Inner {i}", Lift(edgePoints[i].Position, debugLineLift), Lift(innerHeightPoints[i].Position, debugLineLift), interiorMaterial, lineWidth * 0.45f);
                }
            }

            if (showCenterLabels)
            {
                CreateLabel(slotRoot, $"Label {slotLabel}", $"{slotLabel}.C\nTile --\nH {centerPoint.Height:0.0}m", centerPoint.Position + Vector3.up * labelLift, centerColor, labelSize);
            }

            return slotMeshData;
        }

        private TerrainFrameSettings CreateTerrainFrameSettings()
        {
            return new TerrainFrameSettings(
                hexFlatToFlatMeters,
                heightSeed,
                heightAmplitudeMeters,
                visualHeightMultiplier,
                broadHeightScale,
                noiseHeightScale,
                Vector3.zero);
        }

        private HexTerrainMeshSettings CreateTerrainMeshSettings()
        {
            return new HexTerrainMeshSettings(
                terrainSurfaceLift,
                addTerrainMeshColliders,
                markTerrainSurfaceStatic,
                false);
        }

        private void CreatePointMarkers(
            TerrainFrameData frameData,
            Material centerMaterial,
            Material vertexMaterial,
            Material edgeMaterial,
            Material innerMaterial)
        {
            if (frameData == null)
            {
                return;
            }

            for (int i = 0; i < frameData.SharedPointList.Count; i++)
            {
                SharedHeightPoint sharedPoint = frameData.SharedPointList[i];

                if (ShouldShowPointKind(sharedPoint.Kind))
                {
                    GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    pointObject.name = $"{sharedPoint.PointId} {sharedPoint.Kind}";
                    pointObject.transform.SetParent(transform, false);
                    pointObject.transform.position = sharedPoint.Position;
                    pointObject.transform.localScale = Vector3.one * GetPointSize(sharedPoint.Kind);
                    pointObject.GetComponent<Renderer>().sharedMaterial = GetPointMaterial(sharedPoint.Kind, centerMaterial, vertexMaterial, edgeMaterial, innerMaterial);
                }

                if (showPointLabels && sharedPoint.Kind != TerrainPointKind.Center)
                {
                    for (int referenceIndex = 0; referenceIndex < sharedPoint.LocalReferences.Count; referenceIndex++)
                    {
                        string localReference = sharedPoint.LocalReferences[referenceIndex];
                        string label = $"{localReference}\n{sharedPoint.PointId}\nH {sharedPoint.Height:0.0}m";
                        CreateLabel(transform, $"Label {localReference}", label, sharedPoint.Position + Vector3.up * labelLift, GetColor(sharedPoint.Kind), labelSize * 0.58f);
                    }
                }
            }
        }

        private void BuildConformingTileAnchorProof()
        {
            if (terrainFrameData == null)
            {
                return;
            }

            Transform proofRoot = new GameObject("Dropped Tile Anchors Conformed To Frame Heights").transform;
            proofRoot.SetParent(transform, false);

            Material markerMaterial = CreatePointMaterial("Conforming Tile Anchor Material", conformingTileAnchorColor);
            TileConstructionAnchors anchors = TileConstructionAnchors.CreatePrototypeHexAnchors(terrainFrameData.Settings.HexOuterRadiusMeters);
            int conformedCount = 0;

            foreach (TileAnchor anchor in anchors.AllContentAnchors)
            {
                if (anchor.Kind == TileAnchorKind.Prop)
                {
                    continue;
                }

                Vector3 rotatedLocal = TileHexAnchorMath.RotateLocalContent(anchor.LocalPosition, conformingTileOrientationIndex);

                if (!terrainFrameData.TryGetSharedPointAtPosition(rotatedLocal, out SharedHeightPoint heightPoint))
                {
                    continue;
                }

                Vector3 markerPosition = heightPoint.Position + Vector3.up * conformingAnchorLift;
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Conformed {anchor.Id} O{conformingTileOrientationIndex} -> {heightPoint.PointId}";
                marker.transform.SetParent(proofRoot, false);
                marker.transform.position = markerPosition;
                marker.transform.localScale = Vector3.one * conformingAnchorSize;
                marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;

                CreateLabel(
                    proofRoot,
                    $"Label {marker.name}",
                    $"{anchor.Id}\nO{conformingTileOrientationIndex}\n{heightPoint.PointId}\nH {heightPoint.Height:0.0}m",
                    markerPosition + Vector3.up * labelLift,
                    conformingTileAnchorColor,
                    labelSize * 0.52f);

                conformedCount++;
            }

            CreateLabel(
                proofRoot,
                "Tile Conformity Proof Label",
                $"DEV ONLY Tile conformity proof\nOrange cubes = rotated Tile anchors reading Frame height\nOrientation {conformingTileOrientationIndex} / {conformingTileOrientationIndex * 60} deg\nConformed anchors {conformedCount}",
                Vector3.up * 18f,
                conformingTileAnchorColor,
                labelSize);
        }

        private void BuildPlaceholderForestContent()
        {
            if (terrainFrameData == null)
            {
                return;
            }

            Transform contentRoot = new GameObject("Placeholder Tile Forest Content").transform;
            contentRoot.SetParent(transform, false);

            Material trunkMaterial = CreatePointMaterial("Placeholder Birch Trunk Material", placeholderTreeTrunkColor);
            Material crownMaterial = CreatePointMaterial("Placeholder Snow Cap Material", placeholderTreeSnowCapColor);
            Material homeTrunkMaterial = CreatePointMaterial("Placeholder Home Trunk Material", placeholderHomeTreeColor);
            Material threatTrunkMaterial = CreatePointMaterial("Placeholder Threat Trunk Material", placeholderThreatTreeColor);
            TileDefinitionRegistry registry = new TileDefinitionRegistry(terrainFrameData.Settings.HexOuterRadiusMeters);
            TileContentSpawner spawner = new TileContentSpawner(trunkMaterial, crownMaterial, homeTrunkMaterial, threatTrunkMaterial);
            TerrainSurfaceSampler surfaceSampler = CreateTerrainSurfaceSampler();
            int totalTrees = 0;
            int totalGroundedTrees = 0;
            int totalSkippedTrees = 0;

            for (int i = 0; i < terrainFrameData.Slots.Count; i++)
            {
                TerrainSlotData slot = terrainFrameData.Slots[i];
                int tileId = GetPrototypeTileId(slot, i);
                TileDefinition definition = registry.GetDefinition(tileId);
                int orientationIndex = GetPrototypeOrientation(slot, i);
                int spawnedCount = spawner.SpawnForestStandIns(
                    contentRoot,
                    slot,
                    definition,
                    surfaceSampler,
                    placeholderContentSeed,
                    orientationIndex,
                    terrainFrameData.Settings.HexOuterRadiusMeters,
                    out int groundedCount,
                    out int skippedCount);

                totalTrees += spawnedCount;
                totalGroundedTrees += groundedCount;
                totalSkippedTrees += skippedCount;

                if (showPlaceholderContentLabels)
                {
                    CreateLabel(
                        contentRoot,
                        $"Placeholder Content Label {slot.Label}",
                        $"{slot.Label}\nTile {definition.TileIdLabel}\n{definition.ContentCategory}\nTrees {spawnedCount}\nO{orientationIndex}",
                        slot.CenterPoint.Position + Vector3.up * (labelLift + 7f),
                        placeholderTreeTrunkColor,
                        labelSize * 0.62f);
                }
            }

            Debug.Log($"Lost Forest Phase 2 placeholder forest content: Seed={placeholderContentSeed}, Slots={terrainFrameData.Slots.Count}, Trees={totalTrees}, GroundedTrees={totalGroundedTrees}, SkippedTrees={totalSkippedTrees}, Orientation={placeholderContentOrientationIndex}/{placeholderContentOrientationIndex * 60}deg, {surfaceSampler.BuildStatsSummary("Tree grounding")}");
        }

        private static int GetPrototypeTileId(TerrainSlotData slot, int slotIndex)
        {
            if (slot.AxialCoordinate == Vector2Int.zero)
            {
                return FrameSettings.PlayerHomeTileId;
            }

            if (slot.AxialCoordinate == new Vector2Int(1, 0))
            {
                return FrameSettings.PursuerTileId;
            }

            return 10 + slotIndex;
        }

        private int GetPrototypeOrientation(TerrainSlotData slot, int slotIndex)
        {
            if (slot.AxialCoordinate == Vector2Int.zero)
            {
                return 0;
            }

            return Mathf.Abs(placeholderContentOrientationIndex + slotIndex) % 6;
        }

        private bool ShouldShowPointKind(TerrainPointKind kind)
        {
            switch (kind)
            {
                case TerrainPointKind.Center:
                    return showCenters;
                case TerrainPointKind.Vertex:
                    return showVertices;
                case TerrainPointKind.EdgeMidpoint:
                    return showEdgeMidpoints;
                case TerrainPointKind.Inner:
                    return showInnerPoints;
                default:
                    return true;
            }
        }

        private static Vector3[] GetLiftedPositions(IReadOnlyList<SharedHeightPoint> points, float lift)
        {
            Vector3[] positions = new Vector3[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                positions[i] = Lift(points[i].Position, lift);
            }

            return positions;
        }

        private static Vector3 Lift(Vector3 position, float lift)
        {
            return position + Vector3.up * lift;
        }

        private string BuildHeightPointReport(TerrainFrameData frameData)
        {
            if (frameData == null)
            {
                return "Lost Forest Phase 2 Frame Height Point Debug\nNo TerrainFrameData generated.";
            }

            TerrainFrameSettings settings = frameData.Settings;
            SharedHeightPoint sampleSharedPoint = frameData.GetFirstMultiReferencePoint();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Lost Forest Phase 2 Frame Height Point Debug");
            builder.AppendLine($"Hex flat-to-flat\t{settings.HexFlatToFlatMeters:0.##}m");
            builder.AppendLine($"Height seed\t{settings.HeightSeed}");
            builder.AppendLine($"Height amplitude\t{settings.HeightAmplitudeMeters:0.##}m");
            builder.AppendLine($"Visual height multiplier\t{settings.VisualHeightMultiplier:0.##}x");
            builder.AppendLine($"Broad height scale\t{settings.BroadHeightScale:0.####}");
            builder.AppendLine($"Noise height scale\t{settings.NoiseHeightScale:0.####}");
            builder.AppendLine($"Home world center\t({settings.HomeWorldCenter.x:0.##}, {settings.HomeWorldCenter.z:0.##})");
            builder.AppendLine($"Slots\t{frameData.SlotCount}");
            builder.AppendLine($"Local point references\t{frameData.LocalPointReferenceCount}");
            builder.AppendLine($"Unique shared points\t{frameData.SharedPointCount}");
            builder.AppendLine($"Reused point references\t{frameData.ReusedPointReferenceCount}");
            builder.AppendLine($"Multi-reference shared points\t{frameData.CountMultiReferencePoints()}");
            builder.AppendLine($"Shared-boundary validation\t{(frameData.HasSharedBoundaryReuse ? "Passed" : "Failed")}");
            builder.AppendLine($"Centers\t{frameData.CountPointsByKind(TerrainPointKind.Center)}");
            builder.AppendLine($"Vertices\t{frameData.CountPointsByKind(TerrainPointKind.Vertex)}");
            builder.AppendLine($"Edge midpoints\t{frameData.CountPointsByKind(TerrainPointKind.EdgeMidpoint)}");
            builder.AppendLine($"Inner points\t{frameData.CountPointsByKind(TerrainPointKind.Inner)}");
            builder.AppendLine($"Terrain surface\t{(showTerrainSurface ? "Enabled" : "Disabled")}");
            builder.AppendLine($"Terrain mesh builder\t{HexTerrainMeshBuilder.BuilderName}");
            builder.AppendLine($"Terrain meshes generated\t{(terrainMeshData == null ? 0 : terrainMeshData.SurfaceCount)}");
            builder.AppendLine($"Terrain mesh vertices\t{(terrainMeshData == null ? 0 : terrainMeshData.TotalVertexCount)}");
            builder.AppendLine($"Terrain mesh triangles\t{(terrainMeshData == null ? 0 : terrainMeshData.TotalTriangleCount)}");
            builder.AppendLine($"Terrain mesh collider generation\t{(terrainMeshData != null && terrainMeshData.ColliderGenerationRequested ? "Enabled" : "Disabled")}");
            builder.AppendLine($"Terrain mesh collider mode\t{HexTerrainCollisionBuilder.PerSurfaceModeLabel}");
            builder.AppendLine($"Terrain mesh colliders\t{(terrainMeshData == null ? 0 : terrainMeshData.ColliderCount)}");
            builder.AppendLine($"Terrain meshes skipped\t{(terrainMeshData == null ? 0 : terrainMeshData.SkippedMeshCount)}");
            builder.AppendLine($"Terrain mesh colliders skipped\t{(terrainMeshData == null ? 0 : terrainMeshData.SkippedColliderCount)}");
            builder.AppendLine($"Terrain collider validation\t{GetTerrainColliderValidationLine()}");
            builder.AppendLine($"Tile conformity proof\t{(showConformingTileAnchors ? "Enabled" : "Disabled")}");
            builder.AppendLine($"Conforming tile orientation\t{conformingTileOrientationIndex} / {conformingTileOrientationIndex * 60} deg");
            builder.AppendLine($"Placeholder forest content\t{(showPlaceholderForestContent ? "Enabled" : "Disabled")}");
            builder.AppendLine($"Placeholder content seed\t{placeholderContentSeed}");
            builder.AppendLine($"Placeholder content orientation\t{placeholderContentOrientationIndex} / {placeholderContentOrientationIndex * 60} deg");

            if (sampleSharedPoint != null)
            {
                builder.AppendLine("Sample shared point");
                builder.AppendLine($"{sampleSharedPoint.PointId}\tKind={sampleSharedPoint.Kind}\tHeight={sampleSharedPoint.Height:0.00}m\tRefs={string.Join(", ", sampleSharedPoint.LocalReferences)}");
            }

            return builder.ToString();
        }

        private string GetTerrainColliderValidationLine()
        {
            if (terrainMeshData == null)
            {
                return "Not run";
            }

            bool validationPassed = terrainMeshData.ValidateColliderReadiness(out string validationMessage);
            return $"{(validationPassed ? "OK" : "Needs attention")} - {validationMessage}";
        }

        private void CreateLine(Transform parent, string name, Vector3 start, Vector3 end, Material material, float width)
        {
            CreatePolyline(parent, name, new[] { start, end }, false, material, width);
        }

        private static void CreatePolyline(Transform parent, string name, IReadOnlyList<Vector3> points, bool closeLoop, Material material, float width)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = material;
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = closeLoop ? points.Count + 1 : points.Count;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.numCapVertices = 2;

            for (int i = 0; i < points.Count; i++)
            {
                lineRenderer.SetPosition(i, points[i]);
            }

            if (closeLoop)
            {
                lineRenderer.SetPosition(points.Count, points[0]);
            }
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

        private static Material CreateLineMaterial(string name, Color color)
        {
            Material material = new Material(FindShader("Sprites/Default"));
            material.name = name;
            material.color = color;
            return material;
        }

        private static Material CreatePointMaterial(string name, Color color)
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

        private Color GetColor(TerrainPointKind kind)
        {
            switch (kind)
            {
                case TerrainPointKind.Vertex:
                    return vertexColor;
                case TerrainPointKind.EdgeMidpoint:
                    return edgeMidpointColor;
                case TerrainPointKind.Inner:
                    return innerPointColor;
                default:
                    return centerColor;
            }
        }

        private float GetPointSize(TerrainPointKind kind)
        {
            switch (kind)
            {
                case TerrainPointKind.Center:
                    return centerPointSize;
                case TerrainPointKind.Inner:
                    return innerPointSize;
                default:
                    return boundaryPointSize;
            }
        }

        private static Material GetPointMaterial(
            TerrainPointKind kind,
            Material centerMaterial,
            Material vertexMaterial,
            Material edgeMaterial,
            Material innerMaterial)
        {
            switch (kind)
            {
                case TerrainPointKind.Vertex:
                    return vertexMaterial;
                case TerrainPointKind.EdgeMidpoint:
                    return edgeMaterial;
                case TerrainPointKind.Inner:
                    return innerMaterial;
                default:
                    return centerMaterial;
            }
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
