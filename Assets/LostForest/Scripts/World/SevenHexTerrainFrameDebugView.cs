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

        [Header("Scale")]
        [SerializeField] private float hexFlatToFlatMeters = 100f;

        [Header("Terrain Surface")]
        [SerializeField] private bool showTerrainSurface = true;
        [SerializeField] private Color terrainSurfaceColor = new Color(0.92f, 0.96f, 1f, 1f);
        [SerializeField] private float terrainSurfaceLift = -0.08f;

        [Header("Points")]
        [SerializeField] private int heightSeed = 4242;
        [SerializeField] private float heightAmplitudeMeters = 26f;
        [SerializeField] private float visualHeightMultiplier = 1.45f;
        [SerializeField] private float broadHeightScale = 0.012f;
        [SerializeField] private float noiseHeightScale = 0.024f;
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

        [Header("Colors")]
        [SerializeField] private Color outlineColor = new Color(0.15f, 0.55f, 1f, 1f);
        [SerializeField] private Color interiorLineColor = new Color(0.55f, 0.85f, 1f, 0.75f);
        [SerializeField] private Color centerColor = new Color(1f, 0.92f, 0.25f, 1f);
        [SerializeField] private Color vertexColor = new Color(1f, 0.22f, 0.18f, 1f);
        [SerializeField] private Color edgeMidpointColor = new Color(0.25f, 1f, 0.55f, 1f);
        [SerializeField] private Color innerPointColor = new Color(0.86f, 0.48f, 1f, 1f);
        [SerializeField] private Color conformingTileAnchorColor = new Color(1f, 0.62f, 0.08f, 1f);

        private readonly Dictionary<string, SharedHeightPoint> sharedPoints = new Dictionary<string, SharedHeightPoint>();
        private int localPointReferenceCount;
        private int reusedPointReferenceCount;

        public float HexFlatToFlatMeters => Mathf.Max(1f, hexFlatToFlatMeters);
        public float HexOuterRadiusMeters => HexFlatToFlatMeters / SqrtThree;
        public IReadOnlyDictionary<string, SharedHeightPoint> SharedPoints => sharedPoints;

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
            sharedPoints.Clear();
            localPointReferenceCount = 0;
            reusedPointReferenceCount = 0;

            Material outlineMaterial = CreateLineMaterial("7 Hex Outline Material", outlineColor);
            Material interiorMaterial = CreateLineMaterial("7 Hex Interior Material", interiorLineColor);
            Material terrainMaterial = CreatePointMaterial("7 Hex Snow Terrain Surface Material", terrainSurfaceColor);
            Material centerMaterial = CreatePointMaterial("7 Hex Center Point Material", centerColor);
            Material vertexMaterial = CreatePointMaterial("7 Hex Vertex Point Material", vertexColor);
            Material edgeMaterial = CreatePointMaterial("7 Hex Edge Point Material", edgeMidpointColor);
            Material innerMaterial = CreatePointMaterial("7 Hex Inner Point Material", innerPointColor);

            List<Vector2Int> axialSlots = GetRadiusOneAxialSlots();

            for (int i = 0; i < axialSlots.Count; i++)
            {
                BuildSlot(axialSlots[i], outlineMaterial, interiorMaterial, terrainMaterial, centerMaterial, vertexMaterial, edgeMaterial, innerMaterial);
            }

            if (showConformingTileAnchors)
            {
                BuildConformingTileAnchorProof();
            }

            Debug.Log(BuildHeightPointReport());
        }

        private void BuildSlot(
            Vector2Int axial,
            Material outlineMaterial,
            Material interiorMaterial,
            Material terrainMaterial,
            Material centerMaterial,
            Material vertexMaterial,
            Material edgeMaterial,
            Material innerMaterial)
        {
            string slotLabel = GetSlotLabel(axial);
            Vector3 center = HexFrameMath.GetFlatTopHexCenterFromAxial(axial, HexOuterRadiusMeters);
            Vector3[] vertices = GetVertices(center);
            Vector3[] edgeMidpoints = GetEdgeMidpoints(vertices);
            Vector3[] innerPoints = GetInnerPoints(center, vertices);

            SharedHeightPoint centerPoint = RegisterPoint(slotLabel, "C", TerrainPointKind.Center, center, centerMaterial, centerPointSize);
            SharedHeightPoint[] vertexPoints = RegisterPoints(slotLabel, "V", TerrainPointKind.Vertex, vertices, vertexMaterial, boundaryPointSize);
            SharedHeightPoint[] edgePoints = RegisterPoints(slotLabel, "E", TerrainPointKind.EdgeMidpoint, edgeMidpoints, edgeMaterial, boundaryPointSize);
            SharedHeightPoint[] innerHeightPoints = RegisterPoints(slotLabel, "I", TerrainPointKind.Inner, innerPoints, innerMaterial, innerPointSize);

            Transform slotRoot = new GameObject($"Slot {slotLabel}").transform;
            slotRoot.SetParent(transform, false);

            if (showTerrainSurface)
            {
                CreateTerrainSurface(slotRoot, slotLabel, centerPoint, vertexPoints, edgePoints, innerHeightPoints, terrainMaterial);
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
        }

        private SharedHeightPoint[] RegisterPoints(string slotLabel, string labelPrefix, TerrainPointKind kind, IReadOnlyList<Vector3> positions, Material material, float size)
        {
            SharedHeightPoint[] points = new SharedHeightPoint[positions.Count];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = RegisterPoint(slotLabel, $"{labelPrefix}{i}", kind, positions[i], material, size);
            }

            return points;
        }

        private SharedHeightPoint RegisterPoint(string slotLabel, string localLabel, TerrainPointKind kind, Vector3 planarPosition, Material material, float size)
        {
            string globalKey = GetGlobalPointKey(planarPosition);
            string localReference = $"{slotLabel}.{localLabel}";
            localPointReferenceCount++;

            if (!sharedPoints.TryGetValue(globalKey, out SharedHeightPoint sharedPoint))
            {
                float height = GetHeight(planarPosition);
                Vector3 elevatedPosition = new Vector3(planarPosition.x, height * Mathf.Max(0f, visualHeightMultiplier), planarPosition.z);
                sharedPoint = new SharedHeightPoint(globalKey, kind, elevatedPosition, height);
                sharedPoints.Add(globalKey, sharedPoint);

                if (ShouldShowPointKind(kind))
                {
                    GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    pointObject.name = $"{globalKey} {kind}";
                    pointObject.transform.SetParent(transform, false);
                    pointObject.transform.position = elevatedPosition;
                    pointObject.transform.localScale = Vector3.one * size;
                    pointObject.GetComponent<Renderer>().sharedMaterial = material;
                }
            }
            else
            {
                reusedPointReferenceCount++;
            }

            sharedPoint.AddLocalReference(localReference);

            if (showPointLabels && kind != TerrainPointKind.Center)
            {
                string label = $"{localReference}\n{globalKey}\nH {sharedPoint.Height:0.0}m";
                CreateLabel(transform, $"Label {localReference}", label, sharedPoint.Position + Vector3.up * labelLift, GetColor(kind), labelSize * 0.58f);
            }

            return sharedPoint;
        }

        private void CreateTerrainSurface(
            Transform parent,
            string slotLabel,
            SharedHeightPoint centerPoint,
            IReadOnlyList<SharedHeightPoint> vertexPoints,
            IReadOnlyList<SharedHeightPoint> edgePoints,
            IReadOnlyList<SharedHeightPoint> innerPoints,
            Material terrainMaterial)
        {
            GameObject surfaceObject = new GameObject($"Slot {slotLabel} White Terrain Surface");
            surfaceObject.transform.SetParent(parent, false);

            Vector3[] meshVertices = new Vector3[19];
            meshVertices[0] = Lift(centerPoint.Position, terrainSurfaceLift);

            for (int i = 0; i < 6; i++)
            {
                meshVertices[1 + i] = Lift(vertexPoints[i].Position, terrainSurfaceLift);
                meshVertices[7 + i] = Lift(edgePoints[i].Position, terrainSurfaceLift);
                meshVertices[13 + i] = Lift(innerPoints[i].Position, terrainSurfaceLift);
            }

            List<int> triangles = new List<int>(72);

            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                int vertex = 1 + i;
                int nextVertex = 1 + next;
                int edge = 7 + i;
                int inner = 13 + i;
                int nextInner = 13 + next;

                triangles.Add(0);
                triangles.Add(nextInner);
                triangles.Add(inner);

                triangles.Add(inner);
                triangles.Add(edge);
                triangles.Add(vertex);

                triangles.Add(inner);
                triangles.Add(nextInner);
                triangles.Add(edge);

                triangles.Add(nextInner);
                triangles.Add(nextVertex);
                triangles.Add(edge);
            }

            Mesh mesh = new Mesh();
            mesh.name = $"Slot {slotLabel} Terrain Surface Mesh";
            mesh.SetVertices(meshVertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter meshFilter = surfaceObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = surfaceObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = terrainMaterial;
        }

        private void BuildConformingTileAnchorProof()
        {
            Transform proofRoot = new GameObject("Dropped Tile Anchors Conformed To Frame Heights").transform;
            proofRoot.SetParent(transform, false);

            Material markerMaterial = CreatePointMaterial("Conforming Tile Anchor Material", conformingTileAnchorColor);
            TileConstructionAnchors anchors = TileConstructionAnchors.CreatePrototypeHexAnchors(HexOuterRadiusMeters);
            int conformedCount = 0;

            foreach (TileAnchor anchor in anchors.AllContentAnchors)
            {
                if (anchor.Kind == TileAnchorKind.Prop)
                {
                    continue;
                }

                Vector3 rotatedLocal = TileHexAnchorMath.RotateLocalContent(anchor.LocalPosition, conformingTileOrientationIndex);
                string globalKey = GetGlobalPointKey(rotatedLocal);

                if (!sharedPoints.TryGetValue(globalKey, out SharedHeightPoint heightPoint))
                {
                    continue;
                }

                Vector3 markerPosition = heightPoint.Position + Vector3.up * conformingAnchorLift;
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = $"Conformed {anchor.Id} O{conformingTileOrientationIndex} -> {globalKey}";
                marker.transform.SetParent(proofRoot, false);
                marker.transform.position = markerPosition;
                marker.transform.localScale = Vector3.one * conformingAnchorSize;
                marker.GetComponent<Renderer>().sharedMaterial = markerMaterial;

                CreateLabel(
                    proofRoot,
                    $"Label {marker.name}",
                    $"{anchor.Id}\nO{conformingTileOrientationIndex}\n{globalKey}\nH {heightPoint.Height:0.0}m",
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

        private Vector3[] GetVertices(Vector3 center)
        {
            Vector3[] vertices = new Vector3[6];

            for (int i = 0; i < vertices.Length; i++)
            {
                float radians = Mathf.Deg2Rad * (60f * i);
                vertices[i] = center + new Vector3(Mathf.Cos(radians) * HexOuterRadiusMeters, 0f, Mathf.Sin(radians) * HexOuterRadiusMeters);
            }

            return vertices;
        }

        private static Vector3[] GetEdgeMidpoints(IReadOnlyList<Vector3> vertices)
        {
            Vector3[] edgeMidpoints = new Vector3[6];

            for (int i = 0; i < edgeMidpoints.Length; i++)
            {
                edgeMidpoints[i] = (vertices[i] + vertices[(i + 1) % 6]) * 0.5f;
            }

            return edgeMidpoints;
        }

        private static Vector3[] GetInnerPoints(Vector3 center, IReadOnlyList<Vector3> vertices)
        {
            Vector3[] innerPoints = new Vector3[6];

            for (int i = 0; i < innerPoints.Length; i++)
            {
                innerPoints[i] = Vector3.Lerp(center, vertices[i], 0.5f);
            }

            return innerPoints;
        }

        private static List<Vector2Int> GetRadiusOneAxialSlots()
        {
            return new List<Vector2Int>
            {
                Vector2Int.zero,
                new Vector2Int(1, 0),
                new Vector2Int(1, -1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(-1, 1),
                new Vector2Int(0, 1)
            };
        }

        private static string GetSlotLabel(Vector2Int axial)
        {
            if (axial == Vector2Int.zero)
            {
                return "Center";
            }

            if (axial == new Vector2Int(1, 0))
            {
                return "East";
            }

            if (axial == new Vector2Int(1, -1))
            {
                return "Northeast";
            }

            if (axial == new Vector2Int(0, -1))
            {
                return "Northwest";
            }

            if (axial == new Vector2Int(-1, 0))
            {
                return "West";
            }

            if (axial == new Vector2Int(-1, 1))
            {
                return "Southwest";
            }

            return "Southeast";
        }

        private static string GetGlobalPointKey(Vector3 position)
        {
            int x = Mathf.RoundToInt(position.x * 100f);
            int z = Mathf.RoundToInt(position.z * 100f);
            return $"HP_{x}_{z}";
        }

        private float GetHeight(Vector3 planarPosition)
        {
            float broad = Mathf.Sin((planarPosition.x + heightSeed * 0.37f) * broadHeightScale)
                + Mathf.Cos((planarPosition.z - heightSeed * 0.23f) * broadHeightScale);
            broad *= 0.5f;

            float noise = Mathf.PerlinNoise(
                heightSeed * 0.011f + planarPosition.x * noiseHeightScale,
                heightSeed * 0.017f + planarPosition.z * noiseHeightScale) - 0.5f;

            return (broad * 0.65f + noise * 0.7f) * Mathf.Max(0f, heightAmplitudeMeters);
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

        private static Vector3[] GetPositions(IReadOnlyList<SharedHeightPoint> points)
        {
            Vector3[] positions = new Vector3[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                positions[i] = points[i].Position;
            }

            return positions;
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

        private string BuildHeightPointReport()
        {
            int centerCount = 0;
            int vertexCount = 0;
            int edgeCount = 0;
            int innerCount = 0;
            int multiReferencePointCount = 0;
            SharedHeightPoint sampleSharedPoint = null;

            foreach (SharedHeightPoint point in sharedPoints.Values)
            {
                switch (point.Kind)
                {
                    case TerrainPointKind.Center:
                        centerCount++;
                        break;
                    case TerrainPointKind.Vertex:
                        vertexCount++;
                        break;
                    case TerrainPointKind.EdgeMidpoint:
                        edgeCount++;
                        break;
                    case TerrainPointKind.Inner:
                        innerCount++;
                        break;
                }

                if (point.ReferenceCount > 1)
                {
                    multiReferencePointCount++;

                    if (sampleSharedPoint == null)
                    {
                        sampleSharedPoint = point;
                    }
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Lost Forest Phase 2 Frame Height Point Debug");
            builder.AppendLine($"Hex flat-to-flat\t{HexFlatToFlatMeters:0.##}m");
            builder.AppendLine($"Height seed\t{heightSeed}");
            builder.AppendLine($"Height amplitude\t{heightAmplitudeMeters:0.##}m");
            builder.AppendLine($"Visual height multiplier\t{visualHeightMultiplier:0.##}x");
            builder.AppendLine($"Local point references\t{localPointReferenceCount}");
            builder.AppendLine($"Unique shared points\t{sharedPoints.Count}");
            builder.AppendLine($"Reused point references\t{reusedPointReferenceCount}");
            builder.AppendLine($"Multi-reference shared points\t{multiReferencePointCount}");
            builder.AppendLine($"Centers\t{centerCount}");
            builder.AppendLine($"Vertices\t{vertexCount}");
            builder.AppendLine($"Edge midpoints\t{edgeCount}");
            builder.AppendLine($"Inner points\t{innerCount}");
            builder.AppendLine($"Terrain surface\t{(showTerrainSurface ? "Enabled" : "Disabled")}");
            builder.AppendLine($"Tile conformity proof\t{(showConformingTileAnchors ? "Enabled" : "Disabled")}");
            builder.AppendLine($"Conforming tile orientation\t{conformingTileOrientationIndex} / {conformingTileOrientationIndex * 60} deg");

            if (sampleSharedPoint != null)
            {
                builder.AppendLine("Sample shared point");
                builder.AppendLine($"{sampleSharedPoint.PointId}\tKind={sampleSharedPoint.Kind}\tHeight={sampleSharedPoint.Height:0.00}m\tRefs={string.Join(", ", sampleSharedPoint.LocalReferences)}");
            }

            return builder.ToString();
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
