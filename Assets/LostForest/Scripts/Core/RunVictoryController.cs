using LostForest.Phase2.Player;
using LostForest.Phase2.Runes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostForest.Phase2.Core
{
    public sealed class RunVictoryController : MonoBehaviour
    {
        [Header("Run Sources")]
        [SerializeField] private RuneManager runeManager;
        [SerializeField] private PlayerCondition playerCondition;
        [SerializeField] private EarlyWalkThruFirstPersonController firstPersonController;
        [SerializeField] private RuneInteraction runeInteraction;
        [SerializeField] private Camera targetCamera;

        [Header("Victory Prompt")]
        [SerializeField] private bool showVictoryOverlay = true;
        [SerializeField] private float victoryFadeSeconds = 0.8f;
        [SerializeField] private Color victoryOverlayColor = new Color(0.025f, 0.09f, 0.075f, 0.9f);
        [SerializeField] private Color victoryTextColor = new Color(0.92f, 0.98f, 0.96f, 1f);
        [SerializeField] private KeyCode playAgainYesKey = KeyCode.Y;
        [SerializeField] private KeyCode playAgainNoKey = KeyCode.N;
        [SerializeField] private string openingSceneName = "MainPlayScreenLoop";

        [Header("Debug")]
        [SerializeField] private bool logVictoryState = true;

        private bool victoryTriggered;
        private float victoryElapsedSeconds;
        private float previousTimeScale = 1f;
        private bool pausedForVictory;
        private bool disabledControllerForVictory;
        private bool disabledRuneInteractionForVictory;
        private GameObject overlayRoot;
        private Transform overlayQuad;
        private Renderer overlayRenderer;
        private Material overlayMaterial;
        private TextMesh titleText;
        private TextMesh subtitleText;

        public bool IsVictory => victoryTriggered;
        public int ReturnedRuneStoneCount => runeManager == null ? 0 : runeManager.DepositedRuneCount;
        public KeyCode PlayAgainYesKey => playAgainYesKey;
        public KeyCode PlayAgainNoKey => playAgainNoKey;
        public string OpeningSceneName => openingSceneName;

        public void SetSources(
            RuneManager newRuneManager,
            PlayerCondition newPlayerCondition,
            EarlyWalkThruFirstPersonController newFirstPersonController,
            RuneInteraction newRuneInteraction,
            Camera newTargetCamera)
        {
            UnsubscribeFromRuneManager();
            runeManager = newRuneManager;
            playerCondition = newPlayerCondition;
            firstPersonController = newFirstPersonController;
            runeInteraction = newRuneInteraction;
            targetCamera = newTargetCamera;
            SubscribeToRuneManager();
        }

        public void ApplyPrototypeDefaults()
        {
            showVictoryOverlay = true;
            victoryFadeSeconds = 0.8f;
            victoryOverlayColor = new Color(0.025f, 0.09f, 0.075f, 0.9f);
            victoryTextColor = new Color(0.92f, 0.98f, 0.96f, 1f);
            playAgainYesKey = KeyCode.Y;
            playAgainNoKey = KeyCode.N;
            openingSceneName = "MainPlayScreenLoop";
        }

        public bool ValidateConfiguration(out string failureReason)
        {
            if (runeManager == null)
            {
                failureReason = "Victory controller has no RuneManager assigned.";
                return false;
            }

            if (playerCondition == null)
            {
                failureReason = "Victory controller has no PlayerCondition assigned.";
                return false;
            }

            if (playAgainYesKey == KeyCode.None || playAgainNoKey == KeyCode.None || playAgainYesKey == playAgainNoKey)
            {
                failureReason = "Victory controller requires distinct Yes and No keys.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(openingSceneName))
            {
                failureReason = "Victory controller has no opening scene assigned for Play Again.";
                return false;
            }

            if (victoryFadeSeconds <= 0f)
            {
                failureReason = "Victory controller fade time must be positive.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public bool EvaluateRunCompletion()
        {
            if (victoryTriggered || runeManager == null || !runeManager.IsRunComplete)
            {
                return victoryTriggered;
            }

            if (playerCondition != null && (playerCondition.IsFrozen || playerCondition.IsGameOver))
            {
                return false;
            }

            TriggerVictory();
            return victoryTriggered;
        }

        public void ResetVictoryState()
        {
            ReleaseVictoryPause();
            victoryTriggered = false;
            victoryElapsedSeconds = 0f;
            DestroyVictoryOverlay();
        }

        public string BuildDebugSummary()
        {
            int needed = runeManager == null ? 0 : runeManager.NeededRuneCount;
            return $"Run Victory={victoryTriggered} Returned={ReturnedRuneStoneCount}/{needed} Replay={playAgainYesKey}/{playAgainNoKey}";
        }

#if UNITY_EDITOR
        [ContextMenu("Force Three Rune Victory For Debug")]
        public void ForceVictoryForDebug()
        {
            DiscoverReferences();

            if (runeManager == null)
            {
                return;
            }

            for (int i = 0; i < runeManager.NeededRuneCount; i++)
            {
                runeManager.DepositNeededRuneForValidation(runeManager.GetNeededRuneAt(i));
            }

            EvaluateRunCompletion();
        }
#endif

        private void Awake()
        {
            DiscoverReferences();
        }

        private void OnEnable()
        {
            DiscoverReferences();
            SubscribeToRuneManager();
        }

        private void Start()
        {
            EvaluateRunCompletion();
        }

        private void Update()
        {
            if (!victoryTriggered)
            {
                EvaluateRunCompletion();
                return;
            }

            victoryElapsedSeconds += Time.unscaledDeltaTime;
            UpdateVictoryOverlay();

            if (Input.GetKeyDown(playAgainYesKey))
            {
                PlayAgain();
            }
            else if (Input.GetKeyDown(playAgainNoKey))
            {
                ExitGame();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromRuneManager();
            ResetVictoryState();
        }

        private void OnDestroy()
        {
            UnsubscribeFromRuneManager();
            ReleaseVictoryPause();
            DestroyVictoryOverlay();
        }

        private void OnValidate()
        {
            victoryFadeSeconds = Mathf.Max(0.01f, victoryFadeSeconds);
        }

        private void DiscoverReferences()
        {
            if (runeManager == null)
            {
                runeManager = FindAnyObjectByType<RuneManager>();
            }

            if (playerCondition == null)
            {
                playerCondition = FindAnyObjectByType<PlayerCondition>();
            }

            if (firstPersonController == null)
            {
                firstPersonController = FindAnyObjectByType<EarlyWalkThruFirstPersonController>();
            }

            if (runeInteraction == null)
            {
                runeInteraction = FindAnyObjectByType<RuneInteraction>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void SubscribeToRuneManager()
        {
            if (runeManager == null)
            {
                return;
            }

            runeManager.RunCompleted -= HandleRunCompleted;
            runeManager.RunCompleted += HandleRunCompleted;
        }

        private void UnsubscribeFromRuneManager()
        {
            if (runeManager != null)
            {
                runeManager.RunCompleted -= HandleRunCompleted;
            }
        }

        private void HandleRunCompleted()
        {
            EvaluateRunCompletion();
        }

        private void TriggerVictory()
        {
            victoryTriggered = true;
            victoryElapsedSeconds = 0f;

            if (logVictoryState)
            {
                Debug.Log($"Lost Forest Run Victory: Three rune stones returned Home. {BuildDebugSummary()}", this);
            }

            if (!Application.isPlaying)
            {
                return;
            }

            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            pausedForVictory = true;

            if (firstPersonController != null && firstPersonController.enabled)
            {
                firstPersonController.enabled = false;
                disabledControllerForVictory = true;
            }

            if (runeInteraction != null && runeInteraction.enabled)
            {
                runeInteraction.enabled = false;
                disabledRuneInteractionForVictory = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            EnsureVictoryOverlay();
            UpdateVictoryOverlay();
        }

        private void PlayAgain()
        {
            ReleaseVictoryPause();

            if (Application.CanStreamedLevelBeLoaded(openingSceneName))
            {
                SceneManager.LoadScene(openingSceneName);
                return;
            }

            Debug.LogError($"Lost Forest victory screen cannot return to opening scene '{openingSceneName}'.", this);
            ResetVictoryState();
        }

        private void ExitGame()
        {
            ReleaseVictoryPause();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ReleaseVictoryPause()
        {
            if (pausedForVictory)
            {
                Time.timeScale = Mathf.Max(0.01f, previousTimeScale);
                pausedForVictory = false;
            }

            if (disabledControllerForVictory && firstPersonController != null)
            {
                firstPersonController.enabled = true;
            }

            if (disabledRuneInteractionForVictory && runeInteraction != null)
            {
                runeInteraction.enabled = true;
            }

            disabledControllerForVictory = false;
            disabledRuneInteractionForVictory = false;
        }

        private void EnsureVictoryOverlay()
        {
            if (!showVictoryOverlay || overlayRoot != null || !Application.isPlaying)
            {
                return;
            }

            DiscoverReferences();

            if (targetCamera == null)
            {
                return;
            }

            Shader overlayShader = FindOverlayShader();

            if (overlayShader == null)
            {
                return;
            }

            overlayRoot = new GameObject("Prototype Victory Overlay");
            overlayRoot.transform.SetParent(targetCamera.transform, false);

            GameObject quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.name = "Prototype Victory Screen";
            quadObject.transform.SetParent(overlayRoot.transform, false);
            overlayQuad = quadObject.transform;

            Collider quadCollider = quadObject.GetComponent<Collider>();

            if (quadCollider != null)
            {
                Destroy(quadCollider);
            }

            overlayRenderer = quadObject.GetComponent<Renderer>();
            overlayRenderer.sortingOrder = 1100;
            overlayMaterial = new Material(overlayShader)
            {
                color = Color.clear
            };
            overlayRenderer.material = overlayMaterial;

            titleText = CreateOverlayText("Prototype Victory Title", "You Win", 96, 1101);
            subtitleText = CreateOverlayText(
                "Prototype Victory Subtitle",
                $"Three rune stones returned Home.\nPlay Again?  {playAgainYesKey} / {playAgainNoKey}",
                42,
                1101);
            UpdateOverlayGeometry(targetCamera);
        }

        private TextMesh CreateOverlayText(string objectName, string text, int fontSize, int sortingOrder)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(overlayRoot.transform, false);
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = fontSize;
            textMesh.richText = false;
            textMesh.color = Color.clear;

            Renderer textRenderer = textMesh.GetComponent<Renderer>();

            if (textRenderer != null)
            {
                textRenderer.sortingOrder = sortingOrder;
            }

            return textMesh;
        }

        private void UpdateVictoryOverlay()
        {
            if (!showVictoryOverlay || !victoryTriggered || !Application.isPlaying)
            {
                return;
            }

            EnsureVictoryOverlay();

            if (overlayRoot == null)
            {
                return;
            }

            Camera camera = targetCamera == null ? Camera.main : targetCamera;

            if (camera != null)
            {
                UpdateOverlayGeometry(camera);
            }

            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(victoryElapsedSeconds / Mathf.Max(0.01f, victoryFadeSeconds)));
            Color panelColor = new Color(victoryOverlayColor.r, victoryOverlayColor.g, victoryOverlayColor.b, victoryOverlayColor.a * fade);
            Color textColor = new Color(victoryTextColor.r, victoryTextColor.g, victoryTextColor.b, victoryTextColor.a * fade);

            if (overlayMaterial != null)
            {
                overlayMaterial.color = panelColor;
            }

            if (titleText != null)
            {
                titleText.color = textColor;
            }

            if (subtitleText != null)
            {
                subtitleText.color = textColor;
            }
        }

        private void UpdateOverlayGeometry(Camera camera)
        {
            float distance = Mathf.Max(camera.nearClipPlane + 0.35f, 0.6f);
            float height = 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * camera.aspect;

            if (overlayQuad != null)
            {
                overlayQuad.localPosition = new Vector3(0f, 0f, distance);
                overlayQuad.localRotation = Quaternion.identity;
                overlayQuad.localScale = new Vector3(width * 1.08f, height * 1.08f, 1f);
            }

            float textDistance = distance - 0.04f;

            if (titleText != null)
            {
                titleText.transform.localPosition = new Vector3(0f, height * 0.12f, textDistance);
                titleText.transform.localRotation = Quaternion.identity;
                titleText.characterSize = Mathf.Clamp(height * 0.04f, 0.025f, 0.075f);
            }

            if (subtitleText != null)
            {
                subtitleText.transform.localPosition = new Vector3(0f, -height * 0.04f, textDistance);
                subtitleText.transform.localRotation = Quaternion.identity;
                subtitleText.characterSize = Mathf.Clamp(height * 0.016f, 0.012f, 0.033f);
            }
        }

        private void DestroyVictoryOverlay()
        {
            if (overlayRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(overlayRoot);
                }
                else
                {
                    DestroyImmediate(overlayRoot);
                }
            }

            if (overlayMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(overlayMaterial);
                }
                else
                {
                    DestroyImmediate(overlayMaterial);
                }
            }

            overlayRoot = null;
            overlayQuad = null;
            overlayRenderer = null;
            overlayMaterial = null;
            titleText = null;
            subtitleText = null;
        }

        private static Shader FindOverlayShader()
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Unlit/Transparent");
            return shader == null ? Shader.Find("Unlit/Color") : shader;
        }
    }
}
