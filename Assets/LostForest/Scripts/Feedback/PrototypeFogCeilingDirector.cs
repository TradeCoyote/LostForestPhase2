using UnityEngine;
using UnityEngine.Rendering;

namespace LostForest.Phase2.Feedback
{
    /// <summary>
    /// A cheap player-centred fog layer that makes the upper forest feel closed
    /// in without replacing the global distance-fog / whiteout system. The
    /// sheet samples world-space noise, so it remains continuous as the player
    /// crosses hidden Slot boundaries.
    /// </summary>
    [ExecuteAlways]
    public sealed class PrototypeFogCeilingDirector : MonoBehaviour
    {
        private const string SurfaceName = "Wavering Fog Ceiling Surface";

        [Header("Source")]
        [SerializeField] private Transform player;

        [Header("Canopy Range")]
        [SerializeField] private bool ceilingEnabled = true;
        [Tooltip("The centre of the present placeholder-tree range (roughly 21-54m).")]
        [SerializeField, Min(1f)] private float referenceTreeHeightMeters = 42f;
        [Tooltip("0.60 starts the ceiling at three-fifths of the reference tree height.")]
        [SerializeField, Range(0.5f, 0.67f)] private float lowerCeilingHeightFraction = 0.6f;

        [Header("Living Fog Shape")]
        [SerializeField, Min(10f)] private float ceilingRadiusMeters = 95f;
        [SerializeField, Range(4, 32)] private int meshResolution = 16;
        [SerializeField, Min(0.001f)] private float worldNoiseScale = 0.028f;
        [SerializeField, Min(0.001f)] private float driftSpeed = 0.018f;
        [SerializeField] private Vector2 driftDirection = new Vector2(0.72f, 0.41f);
        [SerializeField] private int noiseSeed = 20260816;

        [Header("Visibility")]
        [SerializeField, Range(0.1f, 0.9f)] private float thickestVisibilityFraction = 0.1f;
        [SerializeField, Range(0.1f, 0.9f)] private float thinnestVisibilityFraction = 0.9f;
        [SerializeField] private Color fogCeilingColor = new Color(0.88f, 0.94f, 0.97f, 1f);

        private Transform surfaceTransform;
        private MeshFilter surfaceMeshFilter;
        private MeshRenderer surfaceMeshRenderer;
        private Mesh surfaceMesh;
        private Material surfaceMaterial;
        private Vector3[] vertices;
        private Color[] vertexColors;
        private int[] triangles;
        private int builtResolution = -1;
        private float elapsedSeconds;

        public float ReferenceTreeHeightMeters => Mathf.Max(1f, referenceTreeHeightMeters);
        public float LowerCeilingHeightFraction => Mathf.Clamp(lowerCeilingHeightFraction, 0.5f, 0.67f);
        public float MinimumCeilingHeightMeters => ReferenceTreeHeightMeters * LowerCeilingHeightFraction;
        public float MaximumCeilingHeightMeters => ReferenceTreeHeightMeters;
        public float ThickestVisibilityFraction => Mathf.Clamp(thickestVisibilityFraction, 0.1f, 0.9f);
        public float ThinnestVisibilityFraction => Mathf.Clamp(thinnestVisibilityFraction, ThickestVisibilityFraction, 0.9f);
        public float CurrentCeilingHeightAbovePlayerMeters { get; private set; }
        public float CurrentVisibilityFraction { get; private set; }

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
            UpdateSurface(0f);
        }

        public void ApplyPrototypeDefaults()
        {
            ceilingEnabled = true;
            referenceTreeHeightMeters = 42f;
            lowerCeilingHeightFraction = 0.6f;
            ceilingRadiusMeters = 95f;
            meshResolution = 16;
            worldNoiseScale = 0.028f;
            driftSpeed = 0.018f;
            driftDirection = new Vector2(0.72f, 0.41f);
            noiseSeed = 20260816;
            thickestVisibilityFraction = 0.1f;
            thinnestVisibilityFraction = 0.9f;
            fogCeilingColor = new Color(0.88f, 0.94f, 0.97f, 1f);
            elapsedSeconds = 0f;
            UpdateSurface(0f);
        }

        public bool ValidateConfiguration(out string failureReason)
        {
            if (ReferenceTreeHeightMeters <= 0f)
            {
                failureReason = "Fog-ceiling reference tree height must be positive.";
                return false;
            }

            if (LowerCeilingHeightFraction < 0.5f || LowerCeilingHeightFraction > 0.67f)
            {
                failureReason = $"Fog-ceiling lower height must stay between one-half and two-thirds of the tree height, got {LowerCeilingHeightFraction * 100f:0.0}%.";
                return false;
            }

            if (Mathf.Abs(ThickestVisibilityFraction - 0.1f) > 0.001f || Mathf.Abs(ThinnestVisibilityFraction - 0.9f) > 0.001f)
            {
                failureReason = $"Fog-ceiling visibility must span 10%-90%, got {ThickestVisibilityFraction * 100f:0.0}-{ThinnestVisibilityFraction * 100f:0.0}%.";
                return false;
            }

            if (ceilingRadiusMeters < 10f || meshResolution < 4 || worldNoiseScale <= 0f || driftSpeed <= 0f)
            {
                failureReason = "Fog-ceiling mesh and noise settings are invalid.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public void TickForValidation(float deltaSeconds)
        {
            elapsedSeconds += Mathf.Max(0f, deltaSeconds);
            UpdateSurface(0f);
        }

        public string BuildDebugSummary()
        {
            return $"Fog ceiling Height={CurrentCeilingHeightAbovePlayerMeters:0.0}m ({LowerCeilingHeightFraction * 100f:0}-{100f:0}% tree) Visibility={CurrentVisibilityFraction * 100f:0}% Radius={ceilingRadiusMeters:0}m";
        }

        private void OnEnable()
        {
            DiscoverPlayer();
            EnsureSurface();
            UpdateSurface(0f);
        }

        private void Update()
        {
            DiscoverPlayer();

            if (Application.isPlaying)
            {
                elapsedSeconds += Time.deltaTime;
            }

            UpdateSurface(0f);
        }

        private void OnValidate()
        {
            referenceTreeHeightMeters = Mathf.Max(1f, referenceTreeHeightMeters);
            lowerCeilingHeightFraction = Mathf.Clamp(lowerCeilingHeightFraction, 0.5f, 0.67f);
            ceilingRadiusMeters = Mathf.Max(10f, ceilingRadiusMeters);
            meshResolution = Mathf.Clamp(meshResolution, 4, 32);
            worldNoiseScale = Mathf.Max(0.001f, worldNoiseScale);
            driftSpeed = Mathf.Max(0.001f, driftSpeed);
            thickestVisibilityFraction = Mathf.Clamp(thickestVisibilityFraction, 0.1f, 0.9f);
            thinnestVisibilityFraction = Mathf.Clamp(thinnestVisibilityFraction, thickestVisibilityFraction, 0.9f);
        }

        private void OnDestroy()
        {
            DestroyUnityObject(surfaceMesh);
            DestroyUnityObject(surfaceMaterial);
        }

        private void DiscoverPlayer()
        {
            if (player == null && Camera.main != null)
            {
                player = Camera.main.transform.root;
            }
        }

        private void EnsureSurface()
        {
            if (surfaceTransform == null)
            {
                Transform existing = transform.Find(SurfaceName);
                surfaceTransform = existing == null ? new GameObject(SurfaceName).transform : existing;
                surfaceTransform.SetParent(transform, false);
            }

            if (surfaceMeshFilter == null)
            {
                surfaceMeshFilter = surfaceTransform.GetComponent<MeshFilter>();

                if (surfaceMeshFilter == null)
                {
                    surfaceMeshFilter = surfaceTransform.gameObject.AddComponent<MeshFilter>();
                }
            }

            if (surfaceMeshRenderer == null)
            {
                surfaceMeshRenderer = surfaceTransform.GetComponent<MeshRenderer>();

                if (surfaceMeshRenderer == null)
                {
                    surfaceMeshRenderer = surfaceTransform.gameObject.AddComponent<MeshRenderer>();
                }

                surfaceMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                surfaceMeshRenderer.receiveShadows = false;
                surfaceMeshRenderer.lightProbeUsage = LightProbeUsage.Off;
                surfaceMeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }

            if (surfaceMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                shader = shader == null ? Shader.Find("Unlit/Transparent") : shader;
                shader = shader == null ? Shader.Find("Unlit/Color") : shader;
                surfaceMaterial = new Material(shader)
                {
                    name = "Prototype Wavering Fog Ceiling Material",
                    hideFlags = HideFlags.DontSave,
                    color = fogCeilingColor,
                    renderQueue = 3000
                };
            }

            surfaceMeshRenderer.sharedMaterial = surfaceMaterial;
            EnsureMesh();
        }

        private void EnsureMesh()
        {
            if (surfaceMesh != null && builtResolution == meshResolution)
            {
                return;
            }

            DestroyUnityObject(surfaceMesh);
            surfaceMesh = new Mesh
            {
                name = "Prototype Wavering Fog Ceiling Mesh",
                hideFlags = HideFlags.DontSave
            };
            surfaceMesh.MarkDynamic();
            builtResolution = meshResolution;

            int vertexCountPerAxis = meshResolution + 1;
            int vertexCount = vertexCountPerAxis * vertexCountPerAxis;
            vertices = new Vector3[vertexCount];
            vertexColors = new Color[vertexCount];
            triangles = new int[meshResolution * meshResolution * 6];
            int triangleIndex = 0;

            for (int z = 0; z < meshResolution; z++)
            {
                for (int x = 0; x < meshResolution; x++)
                {
                    int a = z * vertexCountPerAxis + x;
                    int b = a + 1;
                    int c = a + vertexCountPerAxis;
                    int d = c + 1;

                    // Faces point down, toward the player beneath the ceiling.
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                    triangles[triangleIndex++] = c;
                }
            }

            surfaceMesh.vertices = vertices;
            surfaceMesh.colors = vertexColors;
            surfaceMesh.triangles = triangles;
            surfaceMeshFilter.sharedMesh = surfaceMesh;
        }

        private void UpdateSurface(float unusedDeltaSeconds)
        {
            EnsureSurface();

            if (surfaceTransform == null || surfaceMeshRenderer == null || surfaceMesh == null)
            {
                return;
            }

            surfaceMeshRenderer.enabled = ceilingEnabled && player != null;

            if (!ceilingEnabled || player == null)
            {
                return;
            }

            Vector3 playerPosition = player.position;
            surfaceTransform.position = new Vector3(playerPosition.x, playerPosition.y, playerPosition.z);
            surfaceTransform.rotation = Quaternion.identity;
            float diameter = ceilingRadiusMeters * 2f;
            int verticesPerAxis = meshResolution + 1;

            for (int z = 0; z <= meshResolution; z++)
            {
                float z01 = z / (float)meshResolution;

                for (int x = 0; x <= meshResolution; x++)
                {
                    float x01 = x / (float)meshResolution;
                    int index = z * verticesPerAxis + x;
                    float localX = Mathf.Lerp(-ceilingRadiusMeters, ceilingRadiusMeters, x01);
                    float localZ = Mathf.Lerp(-ceilingRadiusMeters, ceilingRadiusMeters, z01);
                    Vector3 worldPosition = surfaceTransform.position + new Vector3(localX, 0f, localZ);
                    EvaluateFogShape(worldPosition, out float heightAbovePlayer, out float visibilityFraction);
                    vertices[index] = new Vector3(localX, heightAbovePlayer, localZ);
                    vertexColors[index] = new Color(1f, 1f, 1f, 1f - visibilityFraction);
                }
            }

            surfaceMesh.vertices = vertices;
            surfaceMesh.colors = vertexColors;
            surfaceMesh.RecalculateBounds();
            surfaceMaterial.color = fogCeilingColor;
            CurrentCeilingHeightAbovePlayerMeters = EvaluateHeightAbovePlayer(playerPosition);
            CurrentVisibilityFraction = EvaluateVisibility(playerPosition);
        }

        private void EvaluateFogShape(Vector3 worldPosition, out float heightAbovePlayer, out float visibilityFraction)
        {
            float noise = SampleNoise(worldPosition);
            float easedNoise = noise * noise * (3f - 2f * noise);
            heightAbovePlayer = Mathf.Lerp(MinimumCeilingHeightMeters, MaximumCeilingHeightMeters, easedNoise);
            visibilityFraction = Mathf.Lerp(ThickestVisibilityFraction, ThinnestVisibilityFraction, easedNoise);
        }

        private float EvaluateHeightAbovePlayer(Vector3 worldPosition)
        {
            EvaluateFogShape(worldPosition, out float height, out _);
            return height;
        }

        private float EvaluateVisibility(Vector3 worldPosition)
        {
            EvaluateFogShape(worldPosition, out _, out float visibility);
            return visibility;
        }

        private float SampleNoise(Vector3 worldPosition)
        {
            Vector2 normalizedDirection = driftDirection.sqrMagnitude < 0.001f
                ? Vector2.right
                : driftDirection.normalized;
            float offset = elapsedSeconds * driftSpeed;
            float x = (worldPosition.x * worldNoiseScale) + normalizedDirection.x * offset + noiseSeed * 0.0017f;
            float z = (worldPosition.z * worldNoiseScale) + normalizedDirection.y * offset + noiseSeed * 0.0031f;
            return Mathf.PerlinNoise(x, z);
        }

        private static void DestroyUnityObject(Object unityObject)
        {
            if (unityObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(unityObject);
            }
            else
            {
                DestroyImmediate(unityObject);
            }
        }
    }
}
