using LostForest.Phase2.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace LostForest.Phase2.World
{
    public sealed class WorldEndFrostController : MonoBehaviour
    {
        private const string OverlayRootName = "World's End Frost Vignette";
        private const string FillQuadName = "World's End Frost Fill";
        private const string VignetteQuadName = "World's End Frost Edge Vignette";
        private const string ScreenPassMaterialName = "World's End Frost Screen Pass Material";
        private const int VignetteTextureSize = 128;

        [Header("Sources")]
        [SerializeField] private Transform player;
        [SerializeField] private PlayerGridAddressTracker gridAddressTracker;
        [SerializeField] private ActiveRegionRenderer activeRegionRenderer;
        [SerializeField] private PlayerCondition playerCondition;
        [SerializeField] private Camera targetCamera;

        [Header("Frost Territory")]
        [SerializeField, Range(1, 3)] private int maximumKnownRingDepth = 3;
        [SerializeField] private float secondsToFullExposure = 30f;
        [SerializeField] private float exposureRecoverySecondsInsideField = 8f;
        [SerializeField, Range(0f, 1f)] private float minimumFrostSpeedMultiplier = 0.18f;
        [SerializeField] private bool renderFrostAroundPlayer = true;
        [SerializeField] private bool clampMovementBeyondKnownRings = true;

        [Header("Prototype Vignette")]
        [SerializeField] private bool showFrostVignette = true;
        [FormerlySerializedAs("drawFrostVignetteInGui")]
        [SerializeField] private bool drawFrostVignetteInScreenPass = true;
        [SerializeField] private float vignetteRecoverySecondsInsideField = 180f;
        [SerializeField] private Color frostColor = new Color(0.34f, 0.78f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float minimumEdgeAlpha = 0.38f;
        [SerializeField, Range(0f, 1f)] private float minimumFillAlpha = 0.08f;
        [SerializeField, Range(0f, 1f)] private float maximumEdgeAlpha = 0.96f;
        [SerializeField, Range(0f, 1f)] private float maximumFillAlpha = 0.72f;
        [SerializeField, Range(0f, 1f)] private float frostCrystalLineStrength = 0.34f;
        [SerializeField, Range(0f, 1f)] private float frostCrystalFacetStrength = 0.18f;
        [SerializeField, Range(0.001f, 0.05f)] private float frostCrystalLineWidth = 0.018f;

        [Header("Debug")]
        [SerializeField] private bool logFrostTransitions = true;
        [SerializeField] private bool logFrostBarrierClamps = true;

        private FieldData fieldData;
        private float hexOuterRadiusMeters = 45f;
        private bool configured;
        private bool isInsidePlayableField = true;
        private bool lastLoggedInsidePlayableField = true;
        private int frostRingDepth;
        private int lastLoggedFrostRingDepth = -999;
        private Vector2Int currentAxialCoordinate;
        private FieldSlotData nearestPlayableSlot;
        private Transform overlayRoot;
        private Renderer fillRenderer;
        private Renderer vignetteRenderer;
        private Material fillMaterial;
        private Material vignetteMaterial;
        private Material screenPassMaterial;
        private Texture2D vignetteTexture;
        private int lastLoggedExposureBand = -1;
        private int lastLoggedVisualExposureBand = -1;
        private float lastBarrierClampLogTime = -999f;
        private bool cameraCallbackRegistered;

        public bool IsConfigured => configured && fieldData != null;
        public bool IsInsidePlayableField => isInsidePlayableField;
        public bool IsInFrostTerritory => IsConfigured && !isInsidePlayableField;
        public int FrostRingDepth => frostRingDepth;
        public bool IsFrostRingDepthKnown => frostRingDepth >= 0 && frostRingDepth <= MaximumKnownRingDepth;
        public int MaximumKnownRingDepth => Mathf.Clamp(maximumKnownRingDepth, 1, 3);
        public float FrostExposureNormalized { get; private set; }
        public float FrostExposurePercent => FrostExposureNormalized * 100f;
        public float VisualFrostExposureNormalized { get; private set; }
        public float VisualFrostExposurePercent => VisualFrostExposureNormalized * 100f;
        public float FrostSpeedMultiplier => Mathf.Lerp(1f, Mathf.Clamp01(minimumFrostSpeedMultiplier), FrostExposureNormalized);
        public float FrostChillPressurePerSecond { get; private set; }
        public Vector2Int CurrentAxialCoordinate => currentAxialCoordinate;
        public FieldSlotData NearestPlayableSlot => nearestPlayableSlot;

        public void Configure(
            FieldData newFieldData,
            float newHexOuterRadiusMeters,
            Transform newPlayer,
            PlayerGridAddressTracker newGridAddressTracker,
            ActiveRegionRenderer newActiveRegionRenderer,
            PlayerCondition newPlayerCondition,
            Camera newTargetCamera)
        {
            fieldData = newFieldData;
            hexOuterRadiusMeters = Mathf.Max(1f, newHexOuterRadiusMeters);
            player = newPlayer;
            gridAddressTracker = newGridAddressTracker;
            activeRegionRenderer = newActiveRegionRenderer;
            playerCondition = newPlayerCondition;
            targetCamera = newTargetCamera;
            configured = fieldData != null;
            RefreshTerritoryState(0f);
            ApplyFrostPressureToPlayer();
            UpdateFrostVignette();
        }

        public void ApplyPrototypeDefaults()
        {
            maximumKnownRingDepth = 3;
            secondsToFullExposure = 30f;
            exposureRecoverySecondsInsideField = 8f;
            minimumFrostSpeedMultiplier = 0.18f;
            renderFrostAroundPlayer = true;
            clampMovementBeyondKnownRings = true;
            showFrostVignette = true;
            drawFrostVignetteInScreenPass = true;
            vignetteRecoverySecondsInsideField = 180f;
            frostColor = new Color(0.34f, 0.78f, 1f, 1f);
            minimumEdgeAlpha = 0.38f;
            minimumFillAlpha = 0.08f;
            maximumEdgeAlpha = 0.96f;
            maximumFillAlpha = 0.72f;
            frostCrystalLineStrength = 0.34f;
            frostCrystalFacetStrength = 0.18f;
            frostCrystalLineWidth = 0.018f;
        }

        public bool TryClampPlanarVelocity(
            Vector3 currentPosition,
            Vector3 planarVelocityMetersPerSecond,
            float deltaTime,
            out Vector3 clampedPlanarVelocityMetersPerSecond)
        {
            clampedPlanarVelocityMetersPerSecond = planarVelocityMetersPerSecond;

            if (!clampMovementBeyondKnownRings || !IsConfigured || deltaTime <= 0f)
            {
                return false;
            }

            Vector3 planarVelocity = new Vector3(
                planarVelocityMetersPerSecond.x,
                0f,
                planarVelocityMetersPerSecond.z);

            if (planarVelocity.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            int currentDepth = FieldBoundaryMath.GetRingDepthAtWorldPosition(
                fieldData,
                hexOuterRadiusMeters,
                currentPosition,
                out _,
                out _);
            Vector3 targetPosition = currentPosition + planarVelocity * deltaTime;
            int targetDepth = FieldBoundaryMath.GetRingDepthAtWorldPosition(
                fieldData,
                hexOuterRadiusMeters,
                targetPosition,
                out _,
                out _);
            int maximumDepth = MaximumKnownRingDepth;

            if (targetDepth >= 0 && targetDepth <= maximumDepth)
            {
                return false;
            }

            if (currentDepth > maximumDepth && targetDepth < currentDepth)
            {
                return false;
            }

            float lower = 0f;
            float upper = 1f;

            for (int i = 0; i < 10; i++)
            {
                float midpoint = (lower + upper) * 0.5f;
                Vector3 probePosition = currentPosition + planarVelocity * (deltaTime * midpoint);
                int probeDepth = FieldBoundaryMath.GetRingDepthAtWorldPosition(
                    fieldData,
                    hexOuterRadiusMeters,
                    probePosition,
                    out _,
                    out _);

                if (probeDepth >= 0 && probeDepth <= maximumDepth)
                {
                    lower = midpoint;
                }
                else
                {
                    upper = midpoint;
                }
            }

            float safeScale = Mathf.Clamp01(lower * 0.96f);
            clampedPlanarVelocityMetersPerSecond = planarVelocityMetersPerSecond * safeScale;
            LogBarrierClampIfNeeded(currentDepth, targetDepth, safeScale);
            return true;
        }

        public string BuildDebugSummary()
        {
            if (!IsConfigured)
            {
                return "WorldEnd --";
            }

            string territory = IsInsidePlayableField ? "Field" : "Frost";
            string ringText = IsInsidePlayableField ? "0" : (frostRingDepth < 0 ? "--" : frostRingDepth.ToString());
            string nearest = nearestPlayableSlot == null ? "--" : nearestPlayableSlot.Address;
            return $"WorldEnd {territory} Ring {ringText} Near {nearest}\nFrost {FrostExposurePercent:0}% Vig {VisualFrostExposurePercent:0}% Rate {FrostChillPressurePerSecond:0.00}/s Speed x{FrostSpeedMultiplier:0.00}";
        }

        private void Awake()
        {
            DiscoverReferencesIfNeeded();
        }

        private void OnValidate()
        {
            InvalidateVignetteTexture();
        }

        private void OnEnable()
        {
            RegisterCameraCallbackIfNeeded();
        }

        private void Update()
        {
            DiscoverReferencesIfNeeded();
            RefreshTerritoryState(Time.deltaTime);
            ApplyFrostPressureToPlayer();
            RenderFrostTerritoryIfNeeded();
            UpdateFrostVignette();
            LogFrostTransitionIfNeeded();
        }

        private void OnDisable()
        {
            FrostChillPressurePerSecond = 0f;
            UnregisterCameraCallback();

            if (playerCondition != null)
            {
                playerCondition.SetFrostChillPressure(0f, 0f);
            }

            if (overlayRoot != null)
            {
                overlayRoot.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            UnregisterCameraCallback();
            DestroyOverlayResources();
        }

        private void RefreshTerritoryState(float deltaTime)
        {
            if (!IsConfigured || player == null)
            {
                isInsidePlayableField = true;
                frostRingDepth = 0;
                nearestPlayableSlot = null;
                currentAxialCoordinate = Vector2Int.zero;
                FrostExposureNormalized = Mathf.MoveTowards(
                    FrostExposureNormalized,
                    0f,
                    ResolveRecoveryStep(deltaTime));
                VisualFrostExposureNormalized = Mathf.MoveTowards(
                    VisualFrostExposureNormalized,
                    0f,
                    ResolveVignetteRecoveryStep(deltaTime));
                FrostChillPressurePerSecond = 0f;
                return;
            }

            Vector3 position = player.position;
            isInsidePlayableField = FieldBoundaryMath.TryResolvePlayableSlot(
                fieldData,
                hexOuterRadiusMeters,
                position,
                out _);
            frostRingDepth = FieldBoundaryMath.GetRingDepthAtWorldPosition(
                fieldData,
                hexOuterRadiusMeters,
                position,
                out currentAxialCoordinate,
                out nearestPlayableSlot);

            if (isInsidePlayableField)
            {
                FrostExposureNormalized = Mathf.MoveTowards(
                    FrostExposureNormalized,
                    0f,
                    ResolveRecoveryStep(deltaTime));
                VisualFrostExposureNormalized = Mathf.MoveTowards(
                    VisualFrostExposureNormalized,
                    0f,
                    ResolveVignetteRecoveryStep(deltaTime));
                FrostChillPressurePerSecond = 0f;
                return;
            }

            FrostExposureNormalized = Mathf.MoveTowards(
                FrostExposureNormalized,
                1f,
                ResolveExposureStep(deltaTime));
            VisualFrostExposureNormalized = Mathf.Max(VisualFrostExposureNormalized, FrostExposureNormalized);
            float maxChill = playerCondition == null ? 100f : playerCondition.MaxChill;
            FrostChillPressurePerSecond = maxChill / Mathf.Max(0.01f, secondsToFullExposure);
        }

        private float ResolveExposureStep(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return 0f;
            }

            return deltaTime / Mathf.Max(0.01f, secondsToFullExposure);
        }

        private float ResolveRecoveryStep(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return 0f;
            }

            return deltaTime / Mathf.Max(0.01f, exposureRecoverySecondsInsideField);
        }

        private float ResolveVignetteRecoveryStep(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return 0f;
            }

            return deltaTime / Mathf.Max(0.01f, vignetteRecoverySecondsInsideField);
        }

        private void ApplyFrostPressureToPlayer()
        {
            if (playerCondition == null)
            {
                return;
            }

            playerCondition.SetFrostChillPressure(FrostExposureNormalized, FrostChillPressurePerSecond);
        }

        private void RenderFrostTerritoryIfNeeded()
        {
            if (!renderFrostAroundPlayer || activeRegionRenderer == null || !IsConfigured)
            {
                return;
            }

            if (isInsidePlayableField)
            {
                return;
            }

            if (activeRegionRenderer.IsRenderingAroundFrostCenter &&
                activeRegionRenderer.CurrentRenderCenterAxial == currentAxialCoordinate)
            {
                return;
            }

            activeRegionRenderer.RenderAroundFrostAxial(currentAxialCoordinate);
        }

        private void UpdateFrostVignette()
        {
            if (drawFrostVignetteInScreenPass)
            {
                RegisterCameraCallbackIfNeeded();

                if (overlayRoot != null)
                {
                    overlayRoot.gameObject.SetActive(false);
                }

                return;
            }

            if (!showFrostVignette || VisualFrostExposureNormalized <= 0.0001f)
            {
                if (overlayRoot != null)
                {
                    overlayRoot.gameObject.SetActive(false);
                }

                return;
            }

            EnsureOverlay();

            if (overlayRoot == null || targetCamera == null)
            {
                return;
            }

            overlayRoot.gameObject.SetActive(true);
            UpdateOverlayGeometry();
            float exposure = Mathf.Clamp01(VisualFrostExposureNormalized);
            float smoothedExposure = Mathf.SmoothStep(0f, 1f, exposure);
            float edgeAlpha = Mathf.Lerp(minimumEdgeAlpha, maximumEdgeAlpha, smoothedExposure);
            float fillAlpha = Mathf.Lerp(minimumFillAlpha, maximumFillAlpha, Mathf.Pow(exposure, 1.25f));

            if (fillMaterial != null)
            {
                fillMaterial.color = WithAlpha(frostColor, fillAlpha);
            }

            if (vignetteMaterial != null)
            {
                vignetteMaterial.color = WithAlpha(frostColor, edgeAlpha);
            }
        }

        private void RegisterCameraCallbackIfNeeded()
        {
            if (cameraCallbackRegistered)
            {
                return;
            }

            Camera.onPostRender += HandleCameraPostRender;
            cameraCallbackRegistered = true;
        }

        private void UnregisterCameraCallback()
        {
            if (!cameraCallbackRegistered)
            {
                return;
            }

            Camera.onPostRender -= HandleCameraPostRender;
            cameraCallbackRegistered = false;
        }

        private void HandleCameraPostRender(Camera renderedCamera)
        {
            if (!drawFrostVignetteInScreenPass ||
                !showFrostVignette ||
                VisualFrostExposureNormalized <= 0.0001f ||
                (playerCondition != null && playerCondition.IsGameOver))
            {
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null && renderedCamera != targetCamera)
            {
                return;
            }

            DrawScreenPassFrostVignette();
        }

        private void DrawScreenPassFrostVignette()
        {
            Material material = GetScreenPassMaterial();

            if (material == null)
            {
                return;
            }

            float exposure = Mathf.Clamp01(VisualFrostExposureNormalized);
            float smoothedExposure = Mathf.SmoothStep(0f, 1f, exposure);

            GL.PushMatrix();
            GL.LoadOrtho();
            DrawScreenPassTexture(
                material,
                Texture2D.whiteTexture,
                WithAlpha(frostColor, Mathf.Lerp(minimumFillAlpha, maximumFillAlpha, Mathf.Pow(exposure, 1.25f))));
            DrawScreenPassTexture(
                material,
                GetVignetteTexture(),
                WithAlpha(frostColor, Mathf.Lerp(minimumEdgeAlpha, maximumEdgeAlpha, smoothedExposure)));
            GL.PopMatrix();
        }

        private void DrawScreenPassTexture(Material material, Texture texture, Color color)
        {
            material.mainTexture = texture;
            material.color = color;

            if (!material.SetPass(0))
            {
                return;
            }

            GL.Begin(GL.QUADS);
            GL.Color(Color.white);
            GL.TexCoord2(0f, 0f);
            GL.Vertex3(0f, 0f, 0f);
            GL.TexCoord2(1f, 0f);
            GL.Vertex3(1f, 0f, 0f);
            GL.TexCoord2(1f, 1f);
            GL.Vertex3(1f, 1f, 0f);
            GL.TexCoord2(0f, 1f);
            GL.Vertex3(0f, 1f, 0f);
            GL.End();
        }

        private void EnsureOverlay()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            if (overlayRoot == null)
            {
                Transform existingRoot = targetCamera.transform.Find(OverlayRootName);
                overlayRoot = existingRoot == null ? new GameObject(OverlayRootName).transform : existingRoot;
                overlayRoot.SetParent(targetCamera.transform, false);
            }

            fillRenderer = EnsureOverlayQuad(FillQuadName, overlayRoot, ref fillMaterial, null);
            vignetteRenderer = EnsureOverlayQuad(VignetteQuadName, overlayRoot, ref vignetteMaterial, GetVignetteTexture());

            if (fillRenderer != null)
            {
                fillRenderer.sortingOrder = 840;
            }

            if (vignetteRenderer != null)
            {
                vignetteRenderer.sortingOrder = 841;
            }
        }

        private Renderer EnsureOverlayQuad(
            string objectName,
            Transform parent,
            ref Material material,
            Texture2D mainTexture)
        {
            Transform quadTransform = parent.Find(objectName);

            if (quadTransform == null)
            {
                GameObject quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quadObject.name = objectName;
                quadTransform = quadObject.transform;
                quadTransform.SetParent(parent, false);

                Collider collider = quadObject.GetComponent<Collider>();

                if (collider != null)
                {
                    DestroyUnityObject(collider);
                }
            }

            Renderer renderer = quadTransform.GetComponent<Renderer>();

            if (renderer == null)
            {
                return null;
            }

            if (material == null)
            {
                material = new Material(FindOverlayShader())
                {
                    name = $"{objectName} Material",
                    color = Color.clear
                };
                material.renderQueue = 3000;
            }

            if (mainTexture != null)
            {
                material.mainTexture = mainTexture;
            }

            renderer.sharedMaterial = material;
            return renderer;
        }

        private void UpdateOverlayGeometry()
        {
            float fillDistance = Mathf.Max(targetCamera.nearClipPlane + 0.36f, 1.12f);
            float vignetteDistance = Mathf.Max(targetCamera.nearClipPlane + 0.34f, 1.10f);
            UpdateOverlayQuadGeometry(fillRenderer, fillDistance);
            UpdateOverlayQuadGeometry(vignetteRenderer, vignetteDistance);
        }

        private void UpdateOverlayQuadGeometry(Renderer renderer, float distance)
        {
            if (renderer == null)
            {
                return;
            }

            float overlayHeight = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float overlayWidth = overlayHeight * targetCamera.aspect;
            Transform quadTransform = renderer.transform;
            quadTransform.localPosition = new Vector3(0f, 0f, distance);
            quadTransform.localRotation = Quaternion.identity;
            quadTransform.localScale = new Vector3(overlayWidth * 1.08f, overlayHeight * 1.08f, 1f);
        }

        private Texture2D GetVignetteTexture()
        {
            if (vignetteTexture != null)
            {
                return vignetteTexture;
            }

            vignetteTexture = new Texture2D(VignetteTextureSize, VignetteTextureSize, TextureFormat.RGBA32, false)
            {
                name = "World's End Frost Vignette Texture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[VignetteTextureSize * VignetteTextureSize];
            Vector2[] shardStarts =
            {
                new Vector2(0.02f, 0.82f),
                new Vector2(0.05f, 0.22f),
                new Vector2(0.30f, 0.98f),
                new Vector2(0.70f, 0.98f),
                new Vector2(0.96f, 0.78f),
                new Vector2(0.96f, 0.24f),
                new Vector2(0.22f, 0.02f),
                new Vector2(0.78f, 0.02f)
            };
            Vector2[] shardEnds =
            {
                new Vector2(0.36f, 0.61f),
                new Vector2(0.34f, 0.39f),
                new Vector2(0.42f, 0.68f),
                new Vector2(0.58f, 0.68f),
                new Vector2(0.63f, 0.59f),
                new Vector2(0.66f, 0.40f),
                new Vector2(0.40f, 0.32f),
                new Vector2(0.60f, 0.32f)
            };
            float center = (VignetteTextureSize - 1) * 0.5f;
            float maxDistance = Mathf.Sqrt(2f) * center;
            float lineWidth = Mathf.Clamp(frostCrystalLineWidth, 0.001f, 0.05f);

            for (int y = 0; y < VignetteTextureSize; y++)
            {
                for (int x = 0; x < VignetteTextureSize; x++)
                {
                    Vector2 uv = new Vector2(
                        x / (float)(VignetteTextureSize - 1),
                        y / (float)(VignetteTextureSize - 1));
                    float dx = x - center;
                    float dy = y - center;
                    float distance01 = Mathf.Sqrt((dx * dx) + (dy * dy)) / maxDistance;
                    float edgeAlpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.34f, 0.98f, distance01));
                    float crystalEdgeWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.24f, 0.92f, distance01));
                    float angle = Mathf.Atan2(dy, dx);
                    float facet = Mathf.SmoothStep(
                        0.72f,
                        1f,
                        Mathf.Abs(Mathf.Sin((angle * 5f) + (distance01 * 20f)))) * crystalEdgeWeight;
                    float shardLine = 0f;

                    for (int i = 0; i < shardStarts.Length; i++)
                    {
                        float distanceToLine = DistanceToSegment(uv, shardStarts[i], shardEnds[i]);
                        shardLine = Mathf.Max(
                            shardLine,
                            1f - Mathf.SmoothStep(lineWidth * 0.35f, lineWidth, distanceToLine));
                    }

                    float crystalAlpha =
                        (shardLine * crystalEdgeWeight * frostCrystalLineStrength) +
                        (facet * frostCrystalFacetStrength);
                    float alpha = Mathf.Clamp01(edgeAlpha + crystalAlpha);
                    float highlight = Mathf.Clamp01(0.82f + (shardLine * 0.18f) + (facet * 0.08f));
                    pixels[(y * VignetteTextureSize) + x] = new Color(highlight, highlight, 1f, alpha);
                }
            }

            vignetteTexture.SetPixels(pixels);
            vignetteTexture.Apply(false, true);
            return vignetteTexture;
        }

        private void LogFrostTransitionIfNeeded()
        {
            if (!logFrostTransitions || !Application.isPlaying)
            {
                return;
            }

            int exposureBand = Mathf.Clamp(Mathf.FloorToInt(FrostExposureNormalized * 4f), 0, 4);
            int visualExposureBand = Mathf.Clamp(Mathf.FloorToInt(VisualFrostExposureNormalized * 4f), 0, 4);
            bool exposureBandChanged = !isInsidePlayableField && exposureBand != lastLoggedExposureBand;
            bool visualExposureBandChanged = isInsidePlayableField &&
                VisualFrostExposureNormalized > 0.0001f &&
                visualExposureBand != lastLoggedVisualExposureBand;

            if (lastLoggedInsidePlayableField == isInsidePlayableField &&
                lastLoggedFrostRingDepth == frostRingDepth &&
                !exposureBandChanged &&
                !visualExposureBandChanged)
            {
                return;
            }

            lastLoggedInsidePlayableField = isInsidePlayableField;
            lastLoggedFrostRingDepth = frostRingDepth;
            lastLoggedExposureBand = exposureBand;
            lastLoggedVisualExposureBand = visualExposureBand;
            Debug.Log($"Lost Forest World's End Frost: InsideField={isInsidePlayableField}, RingDepth={(frostRingDepth < 0 ? "--" : frostRingDepth.ToString())}, Exposure={FrostExposurePercent:0}%, VisualExposure={VisualFrostExposurePercent:0}%, ChillPressure={FrostChillPressurePerSecond:0.00}/s, FrostSpeedMultiplier={FrostSpeedMultiplier:0.00}, NearestFieldSlot={(nearestPlayableSlot == null ? "None" : nearestPlayableSlot.Address)}", this);
        }

        private void LogBarrierClampIfNeeded(int currentDepth, int targetDepth, float safeScale)
        {
            if (!logFrostBarrierClamps || !Application.isPlaying)
            {
                return;
            }

            if (Time.time - lastBarrierClampLogTime < 1f)
            {
                return;
            }

            lastBarrierClampLogTime = Time.time;
            Debug.Log($"Lost Forest World's End Barrier: Movement clamped at frost ring limit. CurrentRing={currentDepth}, TargetRing={targetDepth}, MaxRing={MaximumKnownRingDepth}, VelocityScale={safeScale:0.00}, Exposure={FrostExposurePercent:0}%, FrostSpeedMultiplier={FrostSpeedMultiplier:0.00}, ChillPressure={FrostChillPressurePerSecond:0.00}/s", this);
        }

        private void DiscoverReferencesIfNeeded()
        {
            if (gridAddressTracker == null)
            {
                gridAddressTracker = FindAnyObjectByType<PlayerGridAddressTracker>();
            }

            if (player == null && gridAddressTracker != null)
            {
                player = gridAddressTracker.transform;
            }

            if (player == null)
            {
                EarlyWalkThruFirstPersonController controller = FindAnyObjectByType<EarlyWalkThruFirstPersonController>();
                player = controller == null ? null : controller.transform;
            }

            if (activeRegionRenderer == null)
            {
                activeRegionRenderer = FindAnyObjectByType<ActiveRegionRenderer>();
            }

            if (playerCondition == null && player != null)
            {
                playerCondition = player.GetComponent<PlayerCondition>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null && player != null)
            {
                targetCamera = player.GetComponentInChildren<Camera>();
            }
        }

        private void DestroyOverlayResources()
        {
            if (overlayRoot != null)
            {
                DestroyUnityObject(overlayRoot.gameObject);
            }

            if (fillMaterial != null)
            {
                DestroyUnityObject(fillMaterial);
            }

            if (vignetteMaterial != null)
            {
                DestroyUnityObject(vignetteMaterial);
            }

            if (screenPassMaterial != null)
            {
                DestroyUnityObject(screenPassMaterial);
            }

            if (vignetteTexture != null)
            {
                DestroyUnityObject(vignetteTexture);
            }

            overlayRoot = null;
            fillRenderer = null;
            vignetteRenderer = null;
            fillMaterial = null;
            vignetteMaterial = null;
            screenPassMaterial = null;
            vignetteTexture = null;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        private void InvalidateVignetteTexture()
        {
            if (vignetteTexture == null)
            {
                return;
            }

            DestroyUnityObject(vignetteTexture);
            vignetteTexture = null;

            if (vignetteMaterial != null)
            {
                vignetteMaterial.mainTexture = null;
            }
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared <= 0.000001f)
            {
                return Vector2.Distance(point, start);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + (segment * t));
        }

        private static Shader FindOverlayShader()
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Unlit/Transparent");

            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Unlit/Color");
        }

        private Material GetScreenPassMaterial()
        {
            if (screenPassMaterial != null)
            {
                return screenPassMaterial;
            }

            Shader shader = FindOverlayShader();

            if (shader == null)
            {
                return null;
            }

            screenPassMaterial = new Material(shader)
            {
                name = ScreenPassMaterialName,
                hideFlags = HideFlags.HideAndDontSave,
                color = Color.clear
            };
            screenPassMaterial.renderQueue = 5000;
            return screenPassMaterial;
        }

        private static void DestroyUnityObject(Object objectToDestroy)
        {
            if (objectToDestroy == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(objectToDestroy);
            }
            else
            {
                DestroyImmediate(objectToDestroy);
            }
        }
    }
}
