using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostForest.Phase2.Player
{
    public sealed class PlayerCondition : MonoBehaviour
    {
        private const float FrozenChillEpsilon = 0.0001f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float stamina = 100f;
        [SerializeField] private float sprintDrainPerSecond = 8.5f;
        [SerializeField] private float staminaRecoveryPerSecond = 6.5f;
        [SerializeField] private float exhaustedRecoveryThreshold = 28f;

        [Header("Chill")]
        [SerializeField] private float maxChill = 100f;
        [SerializeField] private float chill = 0f;
        [SerializeField] private float baseChillGainPerSecond = 0.045f;
        [SerializeField, Range(0f, 1f)] private float walkingWarmthReduction = 0.1f;
        [SerializeField, Range(0f, 1f)] private float sprintWarmthReduction = 0.35f;
        [SerializeField] private float sprintChillReductionPerSecond = 0.055f;
        [SerializeField] private float stillnessChillMultiplier = 2f;

        [Header("Stamina Cap Pressure")]
        [SerializeField, Range(0f, 1f)] private float sprintFatigue = 0f;
        [SerializeField] private float sprintFatigueGainPerSecond = 0.06f;
        [SerializeField] private float sprintFatigueRecoveryPerSecond = 0.025f;
        [SerializeField, Range(0.05f, 1f)] private float minimumSprintFatigueCapMultiplier = 0.75f;
        [SerializeField] private float sprintFatigueCapExponent = 1.2f;

        [Header("Condition Movement")]
        [SerializeField, Range(0f, 1f)] private float minimumSpeedMultiplierBeforeFrozen = 0.18f;
        [SerializeField] private float chillSpeedSlowdownExponent = 2.25f;

        [Header("Prototype Game Over")]
        [SerializeField] private bool showPrototypeGameOverOverlay = true;
        [SerializeField] private float gameOverFadeSeconds = 1.25f;
        [SerializeField] private Color gameOverOverlayColor = new Color(0.02f, 0.08f, 0.18f, 0.88f);
        [SerializeField] private Color gameOverTextColor = new Color(0.82f, 0.93f, 1f, 1f);
        [SerializeField] private KeyCode playAgainKey = KeyCode.R;

        [Header("Debug")]
        [SerializeField] private bool logConditionMilestones = true;

        private bool exhausted;
        private bool lastLoggedExhausted;
        private int lastLoggedChillBand = -1;
        private bool gameOverTriggered;
        private float gameOverElapsedSeconds;
        private GameObject gameOverOverlayRoot;
        private Transform gameOverOverlayQuad;
        private Renderer gameOverOverlayRenderer;
        private Material gameOverOverlayMaterial;
        private TextMesh gameOverTitleText;
        private TextMesh gameOverSubtitleText;

        public float BaseMaxStamina => Mathf.Max(1f, maxStamina);
        public float MaxStamina => BaseMaxStamina;
        public float EffectiveMaxStamina => BaseMaxStamina * ChillStaminaCapMultiplier * SprintFatigueCapMultiplier;
        public float Stamina => Mathf.Clamp(stamina, 0f, EffectiveMaxStamina);
        public float StaminaNormalized => EffectiveMaxStamina <= 0.001f ? 0f : Stamina / EffectiveMaxStamina;
        public float MaxChill => Mathf.Max(1f, maxChill);
        public float Chill => Mathf.Clamp(chill, 0f, MaxChill);
        public float ChillNormalized => Chill / MaxChill;
        public float SprintFatigueNormalized => Mathf.Clamp01(sprintFatigue);
        public float ChillStaminaCapMultiplier => Mathf.Clamp01(1f - ChillNormalized);
        public float SprintFatigueCapMultiplier => Mathf.Lerp(1f, MinimumSprintFatigueCapMultiplier, Mathf.Pow(SprintFatigueNormalized, Mathf.Max(0.01f, sprintFatigueCapExponent)));
        public float ConditionSpeedMultiplier => IsFrozen
            ? 0f
            : Mathf.Lerp(1f, MinimumSpeedMultiplierBeforeFrozen, Mathf.Pow(ChillNormalized, Mathf.Max(0.01f, chillSpeedSlowdownExponent)));
        public bool IsExhausted => exhausted;
        public bool IsFrozen => Chill >= MaxChill - FrozenChillEpsilon;
        public bool IsGameOver => gameOverTriggered;
        public bool CanSprint => !IsFrozen && !IsGameOver && !exhausted && Stamina > Mathf.Max(0.01f, EffectiveMaxStamina * 0.02f);

        private float MinimumSprintFatigueCapMultiplier => Mathf.Clamp(minimumSprintFatigueCapMultiplier, 0.05f, 1f);
        private float MinimumSpeedMultiplierBeforeFrozen => Mathf.Clamp01(minimumSpeedMultiplierBeforeFrozen);

        public void ResetCondition()
        {
            sprintFatigue = 0f;
            stamina = BaseMaxStamina;
            chill = 0f;
            exhausted = false;
            lastLoggedExhausted = false;
            lastLoggedChillBand = -1;
            gameOverTriggered = false;
            gameOverElapsedSeconds = 0f;
            DestroyPrototypeGameOverOverlay();
        }

        public void ApplyPhase2PrototypeEconomyDefaults()
        {
            maxStamina = 100f;
            stamina = Mathf.Clamp(stamina, 0f, BaseMaxStamina);
            sprintDrainPerSecond = 8.5f;
            staminaRecoveryPerSecond = 6.5f;
            exhaustedRecoveryThreshold = 28f;
            maxChill = 100f;
            chill = Mathf.Clamp(chill, 0f, MaxChill);
            baseChillGainPerSecond = 0.045f;
            walkingWarmthReduction = 0.1f;
            sprintWarmthReduction = 0.35f;
            sprintChillReductionPerSecond = 0.055f;
            stillnessChillMultiplier = 2f;
            sprintFatigue = Mathf.Clamp01(sprintFatigue);
            sprintFatigueGainPerSecond = 0.06f;
            sprintFatigueRecoveryPerSecond = 0.025f;
            minimumSprintFatigueCapMultiplier = 0.75f;
            sprintFatigueCapExponent = 1.2f;
            minimumSpeedMultiplierBeforeFrozen = 0.18f;
            chillSpeedSlowdownExponent = 2.25f;
            gameOverFadeSeconds = 1.25f;
            gameOverOverlayColor = new Color(0.02f, 0.08f, 0.18f, 0.88f);
            gameOverTextColor = new Color(0.82f, 0.93f, 1f, 1f);
            playAgainKey = KeyCode.R;
            ClampStaminaToEffectiveCap();
        }

        public void Tick(float deltaTime, bool isMoving, bool wantsSprint, bool isSprinting)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (gameOverTriggered)
            {
                ClampStaminaToEffectiveCap();
                LogMilestones(false);
                return;
            }

            UpdateSprintFatigue(deltaTime, isSprinting);
            UpdateStamina(deltaTime, wantsSprint, isSprinting);
            UpdateChill(deltaTime, isMoving, isSprinting);
            ClampStaminaToEffectiveCap();
            UpdateExhaustionState();
            UpdateGameOverState();
            LogMilestones(isSprinting);
        }

        private void Update()
        {
            if (!gameOverTriggered)
            {
                return;
            }

            gameOverElapsedSeconds += Time.unscaledDeltaTime;
            UpdatePrototypeGameOverOverlay();

            if (Input.GetKeyDown(playAgainKey))
            {
                PlayAgain();
            }
        }

        private void OnValidate()
        {
            maxStamina = Mathf.Max(1f, maxStamina);
            stamina = Mathf.Clamp(stamina, 0f, BaseMaxStamina);
            sprintDrainPerSecond = Mathf.Max(0f, sprintDrainPerSecond);
            staminaRecoveryPerSecond = Mathf.Max(0f, staminaRecoveryPerSecond);
            exhaustedRecoveryThreshold = Mathf.Max(0f, exhaustedRecoveryThreshold);
            maxChill = Mathf.Max(1f, maxChill);
            chill = Mathf.Clamp(chill, 0f, MaxChill);
            baseChillGainPerSecond = Mathf.Max(0f, baseChillGainPerSecond);
            sprintChillReductionPerSecond = Mathf.Max(0f, sprintChillReductionPerSecond);
            stillnessChillMultiplier = Mathf.Max(0.01f, stillnessChillMultiplier);
            sprintFatigue = Mathf.Clamp01(sprintFatigue);
            sprintFatigueGainPerSecond = Mathf.Max(0f, sprintFatigueGainPerSecond);
            sprintFatigueRecoveryPerSecond = Mathf.Max(0f, sprintFatigueRecoveryPerSecond);
            sprintFatigueCapExponent = Mathf.Max(0.01f, sprintFatigueCapExponent);
            chillSpeedSlowdownExponent = Mathf.Max(0.01f, chillSpeedSlowdownExponent);
            gameOverFadeSeconds = Mathf.Max(0.01f, gameOverFadeSeconds);
        }

        private void UpdateSprintFatigue(float deltaTime, bool isSprinting)
        {
            float fatigueDelta = isSprinting
                ? Mathf.Max(0f, sprintFatigueGainPerSecond)
                : -Mathf.Max(0f, sprintFatigueRecoveryPerSecond);
            sprintFatigue = Mathf.Clamp01(sprintFatigue + fatigueDelta * deltaTime);
        }

        private void UpdateStamina(float deltaTime, bool wantsSprint, bool isSprinting)
        {
            if (isSprinting)
            {
                stamina -= Mathf.Max(0f, sprintDrainPerSecond) * deltaTime;
            }
            else
            {
                float recoveryMultiplier = wantsSprint ? 0.55f : 1f;
                stamina += Mathf.Max(0f, staminaRecoveryPerSecond) * recoveryMultiplier * deltaTime;
            }
        }

        private void ClampStaminaToEffectiveCap()
        {
            stamina = Mathf.Clamp(stamina, 0f, EffectiveMaxStamina);
        }

        private void UpdateChill(float deltaTime, bool isMoving, bool isSprinting)
        {
            float chillRate = Mathf.Max(0f, baseChillGainPerSecond);

            if (isSprinting)
            {
                chillRate *= Mathf.Max(0f, 1f - sprintWarmthReduction);
                chillRate -= Mathf.Max(0f, sprintChillReductionPerSecond);
            }
            else if (isMoving)
            {
                chillRate *= Mathf.Max(0f, 1f - walkingWarmthReduction);
            }
            else
            {
                chillRate *= Mathf.Max(0.01f, stillnessChillMultiplier);
            }

            chill = Mathf.Clamp(chill + chillRate * deltaTime, 0f, MaxChill);
        }

        private void UpdateExhaustionState()
        {
            if (EffectiveMaxStamina <= 0.01f)
            {
                exhausted = true;
                return;
            }

            if (Stamina <= 0.01f)
            {
                exhausted = true;
                return;
            }

            float recoveryThreshold = Mathf.Min(
                Mathf.Max(0f, exhaustedRecoveryThreshold),
                EffectiveMaxStamina * 0.8f);

            if (exhausted && Stamina >= recoveryThreshold)
            {
                exhausted = false;
            }
        }

        private void UpdateGameOverState()
        {
            if (!IsFrozen || gameOverTriggered)
            {
                return;
            }

            gameOverTriggered = true;
            gameOverElapsedSeconds = 0f;

            if (Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                EnsurePrototypeGameOverOverlay();
                UpdatePrototypeGameOverOverlay();
            }
        }

        private void PlayAgain()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if (Application.CanStreamedLevelBeLoaded(activeScene.name))
            {
                SceneManager.LoadScene(activeScene.name);
                return;
            }

            ResetCondition();

            if (Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDestroy()
        {
            DestroyPrototypeGameOverOverlay();
        }

        private void EnsurePrototypeGameOverOverlay()
        {
            if (!showPrototypeGameOverOverlay || gameOverOverlayRoot != null || !Application.isPlaying)
            {
                return;
            }

            Camera targetCamera = Camera.main;

            if (targetCamera == null)
            {
                targetCamera = GetComponentInChildren<Camera>();
            }

            if (targetCamera == null)
            {
                return;
            }

            Shader overlayShader = FindPrototypeOverlayShader();

            if (overlayShader == null)
            {
                return;
            }

            gameOverOverlayRoot = new GameObject("Prototype Game Over Overlay");
            gameOverOverlayRoot.transform.SetParent(targetCamera.transform, false);
            gameOverOverlayRoot.transform.localPosition = Vector3.zero;
            gameOverOverlayRoot.transform.localRotation = Quaternion.identity;
            gameOverOverlayRoot.transform.localScale = Vector3.one;

            GameObject quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.name = "Prototype Game Over Blue Screen";
            quadObject.transform.SetParent(gameOverOverlayRoot.transform, false);
            gameOverOverlayQuad = quadObject.transform;

            Collider quadCollider = quadObject.GetComponent<Collider>();

            if (quadCollider != null)
            {
                Destroy(quadCollider);
            }

            gameOverOverlayRenderer = quadObject.GetComponent<Renderer>();
            gameOverOverlayRenderer.sortingOrder = 1000;
            gameOverOverlayMaterial = new Material(overlayShader)
            {
                color = Color.clear
            };
            gameOverOverlayRenderer.material = gameOverOverlayMaterial;

            gameOverTitleText = CreatePrototypeGameOverText("Prototype Game Over Title", "Game Over", 96, 1001);
            gameOverSubtitleText = CreatePrototypeGameOverText("Prototype Game Over Subtitle", $"The cold has taken hold.\nPress {playAgainKey} to Play Again.", 42, 1001);
            UpdatePrototypeGameOverOverlayGeometry(targetCamera);
        }

        private TextMesh CreatePrototypeGameOverText(string objectName, string text, int fontSize, int sortingOrder)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(gameOverOverlayRoot.transform, false);
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

        private void UpdatePrototypeGameOverOverlay()
        {
            if (!showPrototypeGameOverOverlay || !gameOverTriggered || !Application.isPlaying)
            {
                return;
            }

            EnsurePrototypeGameOverOverlay();

            if (gameOverOverlayRoot == null)
            {
                return;
            }

            Camera targetCamera = gameOverOverlayRoot.GetComponentInParent<Camera>();

            if (targetCamera != null)
            {
                UpdatePrototypeGameOverOverlayGeometry(targetCamera);
            }

            float fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(gameOverElapsedSeconds / Mathf.Max(0.01f, gameOverFadeSeconds)));
            Color overlayColor = new Color(
                gameOverOverlayColor.r,
                gameOverOverlayColor.g,
                gameOverOverlayColor.b,
                gameOverOverlayColor.a * fade);
            Color textColor = new Color(
                gameOverTextColor.r,
                gameOverTextColor.g,
                gameOverTextColor.b,
                gameOverTextColor.a * fade);

            if (gameOverOverlayMaterial != null)
            {
                gameOverOverlayMaterial.color = overlayColor;
            }

            if (gameOverTitleText != null)
            {
                gameOverTitleText.color = textColor;
            }

            if (gameOverSubtitleText != null)
            {
                gameOverSubtitleText.color = textColor;
            }
        }

        private void UpdatePrototypeGameOverOverlayGeometry(Camera targetCamera)
        {
            float overlayDistance = Mathf.Max(targetCamera.nearClipPlane + 0.35f, 0.6f);
            float overlayHeight = 2f * overlayDistance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float overlayWidth = overlayHeight * targetCamera.aspect;

            if (gameOverOverlayQuad != null)
            {
                gameOverOverlayQuad.localPosition = new Vector3(0f, 0f, overlayDistance);
                gameOverOverlayQuad.localRotation = Quaternion.identity;
                gameOverOverlayQuad.localScale = new Vector3(overlayWidth * 1.08f, overlayHeight * 1.08f, 1f);
            }

            float textDistance = overlayDistance - 0.04f;

            if (gameOverTitleText != null)
            {
                gameOverTitleText.transform.localPosition = new Vector3(0f, overlayHeight * 0.12f, textDistance);
                gameOverTitleText.transform.localRotation = Quaternion.identity;
                gameOverTitleText.characterSize = Mathf.Clamp(overlayHeight * 0.04f, 0.025f, 0.075f);
            }

            if (gameOverSubtitleText != null)
            {
                gameOverSubtitleText.transform.localPosition = new Vector3(0f, -overlayHeight * 0.04f, textDistance);
                gameOverSubtitleText.transform.localRotation = Quaternion.identity;
                gameOverSubtitleText.characterSize = Mathf.Clamp(overlayHeight * 0.016f, 0.012f, 0.033f);
            }
        }

        private void DestroyPrototypeGameOverOverlay()
        {
            if (gameOverOverlayRoot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameOverOverlayRoot);
                }
                else
                {
                    DestroyImmediate(gameOverOverlayRoot);
                }
            }

            if (gameOverOverlayMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameOverOverlayMaterial);
                }
                else
                {
                    DestroyImmediate(gameOverOverlayMaterial);
                }
            }

            gameOverOverlayRoot = null;
            gameOverOverlayQuad = null;
            gameOverOverlayRenderer = null;
            gameOverOverlayMaterial = null;
            gameOverTitleText = null;
            gameOverSubtitleText = null;
        }

        private static Shader FindPrototypeOverlayShader()
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

        private void LogMilestones(bool isSprinting)
        {
            if (!logConditionMilestones || !Application.isPlaying)
            {
                return;
            }

            int chillBand = Mathf.Clamp(Mathf.FloorToInt(ChillNormalized * 4f), 0, 4);

            if (chillBand != lastLoggedChillBand)
            {
                lastLoggedChillBand = chillBand;
                Debug.Log(BuildDebugStatus("ChillBand", isSprinting), this);
            }

            if (exhausted != lastLoggedExhausted)
            {
                lastLoggedExhausted = exhausted;
                Debug.Log(BuildDebugStatus(exhausted ? "Exhausted" : "Recovered", isSprinting), this);
            }
        }

        public string BuildDebugStatus(string reason, bool isSprinting)
        {
            return $"Lost Forest Player Condition: Reason={reason}, Stamina={Stamina:0}/{EffectiveMaxStamina:0}, BaseStamina={BaseMaxStamina:0}, Chill={Chill:0}/{MaxChill:0}, ChillCapMultiplier={ChillStaminaCapMultiplier:0.00}, FatigueCapMultiplier={SprintFatigueCapMultiplier:0.00}, ConditionSpeedMultiplier={ConditionSpeedMultiplier:0.00}, Sprinting={isSprinting}, Exhausted={IsExhausted}, Frozen={IsFrozen}, GameOver={IsGameOver}";
        }
    }
}
