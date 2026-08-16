using LostForest.Phase2.Player;
using LostForest.Phase2.Feedback;
using LostForest.Phase2.Pursuer;
using LostForest.Phase2.Runes;
using LostForest.Phase2.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostForest.Phase2.Debugging
{
    public sealed class GridDebugHud : MonoBehaviour
    {
        private const string CameraHudObjectName = "Grid Debug Camera Panel";
        private const string CameraHudTextObjectName = "Grid Debug Camera Text";
        private const string CameraHudBackingObjectName = "Grid Debug Camera Backing";
        private const string RuneObjectiveHudObjectName = "Needed Rune Objective HUD";
        private const string RuneObjectiveGlyphObjectName = "Needed Rune Glyph";
        private const string RuneObjectiveFillObjectName = "Fill";
        private const string RuneObjectiveOutlineObjectName = "Outline";

        [SerializeField] private PlayerGridAddressTracker gridAddressTracker;
        [SerializeField] private PlayerCondition playerCondition;
        [SerializeField] private PlayerTerrainMovementState playerTerrainMovementState;
        [SerializeField] private ActiveRegionRenderer activeRegionRenderer;
        [SerializeField] private WorldEndFrostController worldEndFrostController;
        [SerializeField] private PrototypeLightingDirector lightingDirector;
        [SerializeField] private RuneManager runeManager;
        [SerializeField] private TheHunterPursuerController theHunter;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool showHud = true;
        [SerializeField] private KeyCode toggleHudKey = KeyCode.T;
        [SerializeField] private float cameraOverlayDistance = 0.9f;
        [SerializeField, Range(0f, 0.25f)] private float viewportInsetX = 0.045f;
        [SerializeField, Range(0f, 0.25f)] private float viewportInsetY = 0.06f;
        [SerializeField, Range(0.15f, 0.95f)] private float panelViewportWidth = 0.34f;
        [SerializeField, Range(0.15f, 0.95f)] private float panelViewportHeight = 0.84f;
        [SerializeField, Range(0.01f, 0.12f)] private float textInsetViewport = 0.025f;
        [SerializeField] private Color textColor = new Color(1f, 0f, 0.75f, 1f);
        [SerializeField] private Color backingColor = new Color(0.92f, 0.97f, 1f, 0f);
        [SerializeField] private Color neededRuneObjectiveColor = new Color(1f, 0.48f, 0.06f, 1f);
        [SerializeField] private Color depositedRuneObjectiveFillColor = Color.white;
        [SerializeField] private Color depositedRuneObjectiveOutlineColor = new Color(0.08f, 0.36f, 1f, 1f);

        private Transform hudRoot;
        private GridDebugMeshText hudText;
        private Transform runeObjectiveRoot;
        private readonly System.Collections.Generic.List<RuneObjectiveGlyph> runeObjectiveGlyphs = new System.Collections.Generic.List<RuneObjectiveGlyph>(3);
        private Renderer backingRenderer;
        private Material backingMaterial;
        private bool loggedScene;
        private bool loggedHudGeometry;

        private void Awake()
        {
            ApplyCompactDefaults();
        }

        public void SetSources(PlayerGridAddressTracker newGridAddressTracker, ActiveRegionRenderer newActiveRegionRenderer)
        {
            gridAddressTracker = newGridAddressTracker;
            activeRegionRenderer = newActiveRegionRenderer;
        }

        public void SetPlayerCondition(PlayerCondition newPlayerCondition)
        {
            playerCondition = newPlayerCondition;
        }

        public void SetPlayerTerrainMovementState(PlayerTerrainMovementState newPlayerTerrainMovementState)
        {
            playerTerrainMovementState = newPlayerTerrainMovementState;
        }

        public void SetRuneManager(RuneManager newRuneManager)
        {
            runeManager = newRuneManager;
        }

        public void SetTheHunter(TheHunterPursuerController newTheHunter)
        {
            theHunter = newTheHunter;
        }

        public void SetWorldEndFrostController(WorldEndFrostController newWorldEndFrostController)
        {
            worldEndFrostController = newWorldEndFrostController;
        }

        public void SetLightingDirector(PrototypeLightingDirector newLightingDirector)
        {
            lightingDirector = newLightingDirector;
        }

        public void SetCamera(Camera newTargetCamera)
        {
            targetCamera = newTargetCamera;
        }

        public void ApplyCompactDefaults()
        {
            cameraOverlayDistance = 0.9f;
            toggleHudKey = KeyCode.T;
            viewportInsetX = 0.045f;
            viewportInsetY = 0.06f;
            panelViewportWidth = 0.34f;
            panelViewportHeight = 0.84f;
            textInsetViewport = 0.025f;
            textColor = new Color(1f, 0f, 0.75f, 1f);
            backingColor = new Color(0.92f, 0.97f, 1f, 0f);
            neededRuneObjectiveColor = new Color(1f, 0.48f, 0.06f, 1f);
            depositedRuneObjectiveFillColor = Color.white;
            depositedRuneObjectiveOutlineColor = new Color(0.08f, 0.36f, 1f, 1f);

            if (hudText != null)
            {
                ConfigureHudPanel();
            }

            if (runeObjectiveRoot != null)
            {
                ConfigureRuneObjectiveHud();
            }
        }

        private void Start()
        {
            EnsureHudText();
            LogActiveSceneOnce();
        }

        private void LateUpdate()
        {
            HandleHudToggleInput();

            if (!showHud)
            {
                if (hudRoot != null)
                {
                    hudRoot.gameObject.SetActive(false);
                }

                if (runeObjectiveRoot != null)
                {
                    runeObjectiveRoot.gameObject.SetActive(false);
                }

                return;
            }

            EnsureHudText();
            EnsureRuneObjectiveHud();
            UpdateHudText();
            UpdateRuneObjectiveHud();
        }

        private void HandleHudToggleInput()
        {
            if (toggleHudKey != KeyCode.None && Input.GetKeyDown(toggleHudKey))
            {
                showHud = !showHud;
            }
        }

        private void EnsureHudText()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            RemoveLegacyHudObjects();
            Transform existingHud = targetCamera.transform.Find(CameraHudObjectName);

            if (existingHud != null)
            {
                hudRoot = existingHud;
                RemoveLegacyTextMesh(hudRoot.gameObject);
                Transform textTransform = existingHud.Find(CameraHudTextObjectName);

                if (textTransform == null)
                {
                    textTransform = new GameObject(CameraHudTextObjectName).transform;
                    textTransform.SetParent(existingHud, false);
                }

                RemoveLegacyTextMesh(textTransform.gameObject);
                hudText = textTransform.GetComponent<GridDebugMeshText>();

                if (hudText == null)
                {
                    hudText = textTransform.gameObject.AddComponent<GridDebugMeshText>();
                }

                EnsureBacking();
                ConfigureHudPanel();
                return;
            }

            hudRoot = new GameObject(CameraHudObjectName).transform;
            hudRoot.SetParent(targetCamera.transform, false);

            GameObject textObject = new GameObject(CameraHudTextObjectName);
            textObject.transform.SetParent(hudRoot, false);
            hudText = textObject.AddComponent<GridDebugMeshText>();

            EnsureBacking();
            ConfigureHudPanel();
        }

        private void ConfigureHudPanel()
        {
            if (hudRoot == null || hudText == null)
            {
                return;
            }

            Vector2 panelSize = ResolveCameraLocalPanelSize();
            hudRoot.localPosition = ResolveCameraLocalHudPosition(panelSize);
            hudRoot.localRotation = Quaternion.identity;
            hudRoot.localScale = Vector3.one;

            float panelWidth = panelSize.x;
            float panelHeight = panelSize.y;
            float insetBase = Mathf.Min(panelWidth, panelHeight);
            float textInsetX = Mathf.Clamp(insetBase * textInsetViewport, 0.01f, panelWidth * 0.2f);
            float textInsetY = Mathf.Clamp(insetBase * textInsetViewport, 0.01f, panelHeight * 0.2f);
            hudText.transform.localPosition = new Vector3((-panelWidth * 0.5f) + textInsetX, (panelHeight * 0.5f) - textInsetY, -0.04f);
            hudText.transform.localRotation = Quaternion.identity;
            hudText.transform.localScale = Vector3.one;
            hudText.Configure(textColor);

            if (backingRenderer != null)
            {
                backingRenderer.enabled = true;
                backingRenderer.sharedMaterial = GetBackingMaterial();
                backingRenderer.sortingOrder = -10;
                backingRenderer.transform.localPosition = new Vector3(0f, 0f, 0.04f);
                backingRenderer.transform.localRotation = Quaternion.identity;
                backingRenderer.transform.localScale = new Vector3(panelWidth, panelHeight, 1f);
            }
        }

        private Vector3 ResolveCameraLocalHudPosition(Vector2 panelSize)
        {
            Camera camera = targetCamera == null ? Camera.main : targetCamera;

            if (camera == null)
            {
                return new Vector3(-0.35f, 0.22f, 1f);
            }

            float distance = Mathf.Max(camera.nearClipPlane + 0.12f, cameraOverlayDistance);
            float halfHeight = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * distance;
            float halfWidth = halfHeight * camera.aspect;
            float left = -halfWidth + (halfWidth * 2f * viewportInsetX);
            float top = halfHeight - (halfHeight * 2f * viewportInsetY);
            float panelHalfWidth = Mathf.Min(panelSize.x * 0.5f, halfWidth * 0.95f);
            float panelHalfHeight = Mathf.Min(panelSize.y * 0.5f, halfHeight * 0.95f);
            float x = left + panelHalfWidth;
            float y = top - panelHalfHeight;
            return new Vector3(x, y, distance);
        }

        private Vector2 ResolveCameraLocalPanelSize()
        {
            Camera camera = targetCamera == null ? Camera.main : targetCamera;

            if (camera == null)
            {
                return new Vector2(0.55f, 0.45f);
            }

            float distance = Mathf.Max(camera.nearClipPlane + 0.12f, cameraOverlayDistance);
            float fullHeight = 2f * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * distance;
            float fullWidth = fullHeight * camera.aspect;
            float usableWidth = fullWidth * Mathf.Clamp01(1f - (viewportInsetX * 2f));
            float usableHeight = fullHeight * Mathf.Clamp01(1f - (viewportInsetY * 2f));
            return new Vector2(
                Mathf.Max(0.2f, usableWidth * panelViewportWidth),
                Mathf.Max(0.2f, usableHeight * panelViewportHeight));
        }

        private void EnsureBacking()
        {
            if (hudRoot == null)
            {
                return;
            }

            Transform backingTransform = hudRoot.Find(CameraHudBackingObjectName);

            if (backingTransform == null)
            {
                GameObject backingObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                backingObject.name = CameraHudBackingObjectName;
                backingTransform = backingObject.transform;
                backingTransform.SetParent(hudRoot, false);

                Collider collider = backingObject.GetComponent<Collider>();

                if (collider != null)
                {
                    Destroy(collider);
                }
            }

            backingRenderer = backingTransform.GetComponent<Renderer>();
        }

        private void EnsureRuneObjectiveHud()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            Transform existingRoot = targetCamera.transform.Find(RuneObjectiveHudObjectName);

            if (existingRoot == null)
            {
                existingRoot = new GameObject(RuneObjectiveHudObjectName).transform;
                existingRoot.SetParent(targetCamera.transform, false);
            }

            runeObjectiveRoot = existingRoot;
            ConfigureRuneObjectiveHud();
        }

        private void ConfigureRuneObjectiveHud()
        {
            if (runeObjectiveRoot == null)
            {
                return;
            }

            Vector2 panelSize = ResolveCameraLocalPanelSize();
            runeObjectiveRoot.localPosition = ResolveCameraLocalRuneObjectivePosition(panelSize);
            runeObjectiveRoot.localRotation = Quaternion.identity;
            runeObjectiveRoot.localScale = Vector3.one;
        }

        private Vector3 ResolveCameraLocalRuneObjectivePosition(Vector2 panelSize)
        {
            Camera camera = targetCamera == null ? Camera.main : targetCamera;

            if (camera == null)
            {
                return new Vector3(0.02f, 0.24f, 0.9f);
            }

            float distance = Mathf.Max(camera.nearClipPlane + 0.12f, cameraOverlayDistance);
            float halfHeight = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * distance;
            float halfWidth = halfHeight * camera.aspect;
            float left = -halfWidth + (halfWidth * 2f * viewportInsetX);
            float top = halfHeight - (halfHeight * 2f * viewportInsetY);
            float panelRight = left + panelSize.x;
            float x = panelRight + Mathf.Clamp(panelSize.x * 0.08f, 0.035f, 0.11f);
            float y = top - Mathf.Clamp(panelSize.y * 0.02f, 0.015f, 0.045f);
            return new Vector3(x, y, distance);
        }

        private void UpdateRuneObjectiveHud()
        {
            if (runeObjectiveRoot == null || runeManager == null)
            {
                if (runeObjectiveRoot != null)
                {
                    runeObjectiveRoot.gameObject.SetActive(false);
                }

                return;
            }

            runeObjectiveRoot.gameObject.SetActive(true);
            int runeCount = runeManager.NeededRuneCount;
            EnsureRuneObjectiveGlyphCount(runeCount);

            float glyphStep = 0.145f;
            Vector2 glyphBounds = new Vector2(0.105f, 0.13f);
            float outlineOffset = 0.007f;

            for (int i = 0; i < runeObjectiveGlyphs.Count; i++)
            {
                RuneObjectiveGlyph glyph = runeObjectiveGlyphs[i];
                bool active = i < runeCount;
                glyph.Root.gameObject.SetActive(active);

                if (!active)
                {
                    continue;
                }

                char runeLetter = runeManager.GetNeededRuneAt(i);
                bool deposited = runeManager.IsRuneDeposited(runeLetter);
                glyph.Root.localPosition = new Vector3(i * glyphStep, 0f, 0f);
                glyph.SetRune(
                    runeLetter,
                    glyphBounds,
                    deposited,
                    neededRuneObjectiveColor,
                    depositedRuneObjectiveFillColor,
                    depositedRuneObjectiveOutlineColor,
                    outlineOffset);
            }
        }

        private void EnsureRuneObjectiveGlyphCount(int runeCount)
        {
            while (runeObjectiveGlyphs.Count < runeCount)
            {
                int index = runeObjectiveGlyphs.Count;
                Transform glyphRoot = new GameObject($"{RuneObjectiveGlyphObjectName} {index + 1}").transform;
                glyphRoot.SetParent(runeObjectiveRoot, false);
                runeObjectiveGlyphs.Add(RuneObjectiveGlyph.Create(glyphRoot));
            }
        }

        private void RemoveLegacyHudObjects()
        {
            if (targetCamera == null)
            {
                return;
            }

            Transform legacyText = targetCamera.transform.Find(CameraHudTextObjectName);

            if (legacyText != null)
            {
                DestroyUnityObject(legacyText.gameObject);
            }

            Transform legacyPanel = targetCamera.transform.Find(CameraHudObjectName);

            if (legacyPanel != null)
            {
                RemoveLegacyTextMesh(legacyPanel.gameObject);
            }
        }

        private static void RemoveLegacyTextMesh(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            TextMesh legacyText = gameObject.GetComponent<TextMesh>();

            if (legacyText != null)
            {
                DestroyUnityObject(legacyText);
            }
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
                return;
            }

            DestroyImmediate(objectToDestroy);
        }

        private Material GetBackingMaterial()
        {
            if (backingMaterial == null)
            {
                backingMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    name = "Grid Debug HUD Backing Material"
                };
            }

            backingMaterial.color = backingColor;
            return backingMaterial;
        }

        private string BuildGridAddressText()
        {
            FieldSlotData slot = gridAddressTracker == null ? null : gridAddressTracker.CurrentSlot;

            if (slot != null)
            {
                return slot.Address;
            }

            return worldEndFrostController != null && worldEndFrostController.IsInFrostTerritory
                ? "Outside Field"
                : "--";
        }

        private string BuildElevationText()
        {
            FieldSlotData slot = gridAddressTracker == null ? null : gridAddressTracker.CurrentSlot;

            if (slot == null)
            {
                return "Elev --";
            }

            TerrainElevationSample elevationSample = GetCurrentElevationSample(slot);
            string landmarkTile = slot.IsLandmarkTile ? " Landmark Tile" : string.Empty;
            return $"Elev {elevationSample.LogicalElevationMeters:0.0} {elevationSample.ElevationBand} {elevationSample.Landform}{landmarkTile}";
        }

        private TerrainElevationSample GetCurrentElevationSample(FieldSlotData slot)
        {
            if (gridAddressTracker == null)
            {
                return default;
            }

            Vector3 playerPosition = gridAddressTracker.transform.position;

            if (activeRegionRenderer != null && activeRegionRenderer.TrySampleTerrainElevation(slot, playerPosition, out TerrainElevationSample elevationSample))
            {
                return elevationSample;
            }

            return new TerrainElevationSample(
                new TerrainSurfaceSample(playerPosition, Vector3.up, TerrainSurfaceSampleSource.FrameHeightFallback, null),
                playerPosition.y,
                playerPosition.y,
                TerrainElevationBand.Mid,
                TerrainLandform.Unknown,
                0f,
                0f,
                Vector3.zero,
                Vector3.zero,
                -1,
                0f,
                0f,
                Vector3.zero);
        }

        private string BuildConditionText()
        {
            if (playerCondition == null)
            {
                return string.Empty;
            }

            string state = BuildConditionStateText();
            return $"Sta {playerCondition.Stamina:0}/{playerCondition.EffectiveMaxStamina:0} ({playerCondition.StaminaNormalized * 100f:0}%){state}\nCap C{playerCondition.ChillStaminaCapMultiplier:0.00} F{playerCondition.SprintFatigueCapMultiplier:0.00}\nChill {playerCondition.ChillNormalized * 100f:0}% Move x{playerCondition.ConditionSpeedMultiplier:0.00}";
        }

        private string BuildMovementText()
        {
            DiscoverPlayerTerrainMovementStateIfNeeded();

            if (playerTerrainMovementState == null)
            {
                return string.Empty;
            }

            string sprint = playerTerrainMovementState.IsSprinting ? "Y" : "N";
            return $"Move {playerTerrainMovementState.TravelState} Slope {playerTerrainMovementState.CurrentSlopeDegrees:0}deg\nGrade {playerTerrainMovementState.SignedMovementGradeDegrees:+0.0;-0.0;0.0}deg x{playerTerrainMovementState.SpeedMultiplier:0.00}\nSpeed {playerTerrainMovementState.FinalMovementSpeedMetersPerSecond:0.0} Sprint {sprint}";
        }

        private void UpdateHudText()
        {
            if (hudText == null)
            {
                return;
            }

            hudRoot.gameObject.SetActive(true);
            ConfigureHudPanel();
            string movementText = BuildMovementText();
            string conditionText = BuildConditionText();
            string worldEndText = BuildWorldEndText();
            string lightingText = BuildLightingText();
            string runeText = BuildRuneText();
            string pursuerText = BuildPursuerText();
            string landmarkText = BuildLandmarkText();
            string text = $"{BuildGridAddressText()}\n{BuildElevationText()}{BuildOptionalLine(movementText)}{BuildOptionalLine(conditionText)}{BuildOptionalLine(worldEndText)}{BuildOptionalLine(lightingText)}{BuildOptionalLine(runeText)}{BuildOptionalLine(pursuerText)}{BuildOptionalLine(landmarkText)}";
            Vector2 textArea = ResolveCameraLocalPanelSize();
            hudText.SetText(text, textArea * 0.88f);

            if (!loggedHudGeometry)
            {
                loggedHudGeometry = true;
                Debug.Log($"Lost Forest Grid Debug HUD geometry: TextLength={text.Length}, RootLocal={hudRoot.localPosition}, TextLocal={hudText.transform.localPosition}, BackingLocal={(backingRenderer == null ? Vector3.zero : backingRenderer.transform.localPosition)}, BackingScale={(backingRenderer == null ? Vector3.zero : backingRenderer.transform.localScale)}", this);
            }
        }

        private void LogActiveSceneOnce()
        {
            if (loggedScene)
            {
                return;
            }

            loggedScene = true;
            Debug.Log($"Lost Forest Grid Debug HUD active in scene '{SceneManager.GetActiveScene().name}'. Camera text overlay enabled.");
        }

        private void DiscoverPlayerTerrainMovementStateIfNeeded()
        {
            if (playerTerrainMovementState != null || gridAddressTracker == null)
            {
                return;
            }

            playerTerrainMovementState = gridAddressTracker.GetComponent<PlayerTerrainMovementState>();
        }

        private string BuildWorldEndText()
        {
            if (worldEndFrostController == null)
            {
                worldEndFrostController = FindAnyObjectByType<WorldEndFrostController>();
            }

            if (worldEndFrostController == null)
            {
                return string.Empty;
            }

            string summary = worldEndFrostController.BuildDebugSummary();
            string frostTiles = activeRegionRenderer == null
                ? string.Empty
                : $"\nFrost Tiles Active: {activeRegionRenderer.ActiveRenderedFrostTileCount}/{activeRegionRenderer.OuterFrostRenderRings} rings";
            return $"{summary}{frostTiles}";
        }

        private string BuildLightingText()
        {
            if (lightingDirector == null)
            {
                lightingDirector = FindAnyObjectByType<PrototypeLightingDirector>();
            }

            return lightingDirector == null ? string.Empty : lightingDirector.BuildDebugSummary();
        }

        private static string BuildOptionalLine(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : $"\n{value}";
        }

        private string BuildRuneText()
        {
            if (runeManager == null)
            {
                return string.Empty;
            }

            string nearestMatchingSlot = "None";

            if (runeManager.TryGetNearestMatchingRuneSlotDebug(out string slotAddress, out char runeLetter, out float distanceMeters))
            {
                nearestMatchingSlot = $"{slotAddress} {runeLetter} {distanceMeters:0.0}m";
            }

            return $"Needed: {runeManager.NeededRunesDebugText}\nCarried: {runeManager.CarriedRuneDebugText}\nDeposited: {runeManager.DepositedRunesDebugText}\nActive Rune Markers: {runeManager.ActiveMarkerCount}\nNearest Matching Rune Slot: {nearestMatchingSlot}";
        }

        private string BuildPursuerText()
        {
            if (theHunter == null)
            {
                theHunter = FindAnyObjectByType<TheHunterPursuerController>();
            }

            return theHunter == null ? string.Empty : theHunter.BuildDebugSummary();
        }

        private string BuildLandmarkText()
        {
            if (activeRegionRenderer == null)
            {
                return string.Empty;
            }

            Vector3 probePosition = gridAddressTracker == null ? activeRegionRenderer.transform.position : gridAddressTracker.transform.position;
            string nearestLandmark = activeRegionRenderer.TryGetNearestLandmarkDebug(probePosition, out string debugText) ? debugText : "None";
            return $"Landmark Tiles Active: {activeRegionRenderer.ActiveRenderedLandmarkTileCount}\nActive Landmarks: {activeRegionRenderer.ActiveLandmarkInstanceCount}\nNearest Landmark: {nearestLandmark}";
        }

        private string BuildConditionStateText()
        {
            if (playerCondition.IsGameOver)
            {
                return " GAME OVER";
            }

            if (playerCondition.IsFrozen)
            {
                return " FROZEN";
            }

            return playerCondition.IsExhausted ? " EXH" : string.Empty;
        }

        private sealed class RuneObjectiveGlyph
        {
            private readonly GridDebugMeshText fillText;
            private readonly GridDebugMeshText[] outlineTexts;

            public Transform Root { get; }

            private RuneObjectiveGlyph(Transform root, GridDebugMeshText fill, GridDebugMeshText[] outlines)
            {
                Root = root;
                fillText = fill;
                outlineTexts = outlines;
            }

            public static RuneObjectiveGlyph Create(Transform root)
            {
                GridDebugMeshText[] outlines = new GridDebugMeshText[8];
                Vector2Int[] offsets =
                {
                    new Vector2Int(-1, -1),
                    new Vector2Int(0, -1),
                    new Vector2Int(1, -1),
                    new Vector2Int(-1, 0),
                    new Vector2Int(1, 0),
                    new Vector2Int(-1, 1),
                    new Vector2Int(0, 1),
                    new Vector2Int(1, 1)
                };

                for (int i = 0; i < outlines.Length; i++)
                {
                    Transform outlineTransform = new GameObject($"{RuneObjectiveOutlineObjectName} {i + 1}").transform;
                    outlineTransform.SetParent(root, false);
                    outlineTransform.localRotation = Quaternion.identity;
                    outlineTransform.localScale = Vector3.one;
                    outlineTransform.localPosition = new Vector3(offsets[i].x, offsets[i].y, 0.01f);
                    outlines[i] = outlineTransform.gameObject.AddComponent<GridDebugMeshText>();
                }

                Transform fillTransform = new GameObject(RuneObjectiveFillObjectName).transform;
                fillTransform.SetParent(root, false);
                fillTransform.localPosition = Vector3.zero;
                fillTransform.localRotation = Quaternion.identity;
                fillTransform.localScale = Vector3.one;
                GridDebugMeshText fill = fillTransform.gameObject.AddComponent<GridDebugMeshText>();

                return new RuneObjectiveGlyph(root, fill, outlines);
            }

            public void SetRune(
                char runeLetter,
                Vector2 bounds,
                bool deposited,
                Color neededColor,
                Color depositedFillColor,
                Color depositedOutlineColor,
                float outlineOffset)
            {
                string text = runeLetter.ToString();
                fillText.Configure(deposited ? depositedFillColor : neededColor);
                fillText.SetText(text, bounds);

                for (int i = 0; i < outlineTexts.Length; i++)
                {
                    Transform outlineTransform = outlineTexts[i].transform;
                    Vector3 direction = outlineTransform.localPosition.sqrMagnitude <= 0.0001f
                        ? Vector3.zero
                        : new Vector3(Mathf.Sign(outlineTransform.localPosition.x), Mathf.Sign(outlineTransform.localPosition.y), 0f);
                    outlineTransform.localPosition = direction * outlineOffset + Vector3.forward * 0.01f;
                    outlineTexts[i].gameObject.SetActive(deposited);

                    if (deposited)
                    {
                        outlineTexts[i].Configure(depositedOutlineColor);
                        outlineTexts[i].SetText(text, bounds);
                    }
                }
            }
        }
    }
}
