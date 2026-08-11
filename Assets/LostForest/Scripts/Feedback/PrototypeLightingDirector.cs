using UnityEngine;
using UnityEngine.Rendering;

namespace LostForest.Phase2.Feedback
{
    [ExecuteAlways]
    public sealed class PrototypeLightingDirector : MonoBehaviour
    {
        public enum LightingState
        {
            Overcast,
            CloudsThinning,
            SunWavering,
            ReturningToOvercast
        }

        [Header("Calibrated 100% Direct Sun Reference")]
        [SerializeField] private Light directSun;
        [SerializeField] private float referenceDirectSunIntensity = 1.15f;
        [SerializeField, Range(0f, 1f)] private float referenceShadowStrength = 1f;
        [SerializeField] private Color referenceSunColor = Color.white;

        [Header("Overcast Ambient Snow Light")]
        [SerializeField] private Color overcastAmbientSkyColor = new Color(0.78f, 0.84f, 0.86f, 1f);
        [SerializeField] private Color overcastAmbientEquatorColor = new Color(0.64f, 0.70f, 0.72f, 1f);
        [SerializeField] private Color overcastAmbientGroundColor = new Color(0.50f, 0.56f, 0.58f, 1f);
        [SerializeField, Range(0f, 2f)] private float overcastAmbientIntensity = 1.05f;
        [SerializeField] private Color overcastSubtractiveShadowColor = new Color(0.78f, 0.84f, 0.88f, 1f);

        [Header("Cloud-Thinning Cycle")]
        [SerializeField] private Vector2 intervalRangeSeconds = new Vector2(120f, 240f);
        [SerializeField] private Vector2 windowDurationRangeSeconds = new Vector2(30f, 60f);
        [SerializeField] private Vector2 targetHoldRangeSeconds = new Vector2(5f, 12f);
        [SerializeField] private float transitionSpeedPercentPerSecond = 2.8f;
        [SerializeField] private float returnToOvercastSpeedPercentPerSecond = 4.25f;
        [SerializeField, Range(0f, 100f)] private float minimumSunPercent = 10f;
        [SerializeField, Range(0f, 100f)] private float maximumSunPercent = 50f;
        [SerializeField] private int randomSeed = 20260811;

        [Header("Development Debug Commands")]
        [SerializeField] private bool enableDevelopmentHotkeys = true;
        [SerializeField] private KeyCode forceCloudThinningKey = KeyCode.F7;
        [SerializeField] private KeyCode forceReturnToOvercastKey = KeyCode.F8;

        private System.Random random;
        private LightingState state = LightingState.Overcast;
        private float currentSunPercent;
        private float targetSunPercent;
        private float secondsUntilNextWindow;
        private float activeWindowSecondsRemaining;
        private float targetSecondsRemaining;
        private bool initialized;

        public LightingState CurrentState => state;
        public float CurrentSunPercent => currentSunPercent;
        public float TargetSunPercent => targetSunPercent;
        public float SecondsUntilNextWindow => secondsUntilNextWindow;
        public float ActiveWindowSecondsRemaining => activeWindowSecondsRemaining;
        public float MinimumSunPercent => minimumSunPercent;
        public float MaximumSunPercent => maximumSunPercent;
        public Vector2 IntervalRangeSeconds => intervalRangeSeconds;
        public Vector2 WindowDurationRangeSeconds => windowDurationRangeSeconds;
        public bool StartsFullyOvercast => Mathf.Approximately(currentSunPercent, 0f) && state == LightingState.Overcast;

        public void SetDirectSun(Light newDirectSun)
        {
            directSun = newDirectSun;
        }

        public void CaptureCurrentSunAsReference()
        {
            if (directSun == null)
            {
                return;
            }

            referenceDirectSunIntensity = Mathf.Max(0f, directSun.intensity);
            referenceShadowStrength = directSun.shadowStrength;
            referenceSunColor = directSun.color;
        }

        public void ApplyPrototypeDefaults()
        {
            intervalRangeSeconds = new Vector2(120f, 240f);
            windowDurationRangeSeconds = new Vector2(30f, 60f);
            targetHoldRangeSeconds = new Vector2(5f, 12f);
            transitionSpeedPercentPerSecond = 2.8f;
            returnToOvercastSpeedPercentPerSecond = 4.25f;
            minimumSunPercent = 10f;
            maximumSunPercent = 50f;
            randomSeed = 20260811;
            overcastAmbientSkyColor = new Color(0.78f, 0.84f, 0.86f, 1f);
            overcastAmbientEquatorColor = new Color(0.64f, 0.70f, 0.72f, 1f);
            overcastAmbientGroundColor = new Color(0.50f, 0.56f, 0.58f, 1f);
            overcastAmbientIntensity = 1.05f;
            overcastSubtractiveShadowColor = new Color(0.78f, 0.84f, 0.88f, 1f);
        }

        public void ResetToOvercastAndScheduleNextWindow()
        {
            random = new System.Random(randomSeed);
            initialized = true;
            state = LightingState.Overcast;
            currentSunPercent = 0f;
            targetSunPercent = 0f;
            activeWindowSecondsRemaining = 0f;
            targetSecondsRemaining = 0f;
            secondsUntilNextWindow = RollRange(intervalRangeSeconds);
            ApplyLighting();
        }

        public void TickForValidation(float deltaSeconds)
        {
            Tick(deltaSeconds);
        }

        [ContextMenu("Force Immediate Cloud-Thinning Window")]
        public void ForceImmediateCloudThinningWindow()
        {
            EnsureInitialized();
            secondsUntilNextWindow = 0f;
            BeginCloudThinningWindow();
        }

        [ContextMenu("Force Return To Overcast")]
        public void ForceReturnToOvercast()
        {
            EnsureInitialized();
            state = LightingState.ReturningToOvercast;
            targetSunPercent = 0f;
            activeWindowSecondsRemaining = 0f;
            targetSecondsRemaining = 0f;
        }

        public bool ValidateConfiguration(out string failureReason)
        {
            if (directSun == null)
            {
                failureReason = "Lighting director has no direct sun assigned.";
                return false;
            }

            if (intervalRangeSeconds.x < 120f || intervalRangeSeconds.y > 240f || intervalRangeSeconds.x > intervalRangeSeconds.y)
            {
                failureReason = $"Lighting director interval range must stay within 120-240 seconds, got {intervalRangeSeconds.x:0.0}-{intervalRangeSeconds.y:0.0}.";
                return false;
            }

            if (windowDurationRangeSeconds.x < 30f || windowDurationRangeSeconds.y > 60f || windowDurationRangeSeconds.x > windowDurationRangeSeconds.y)
            {
                failureReason = $"Lighting director window duration must stay within 30-60 seconds, got {windowDurationRangeSeconds.x:0.0}-{windowDurationRangeSeconds.y:0.0}.";
                return false;
            }

            if (minimumSunPercent < 10f || maximumSunPercent > 50f || minimumSunPercent > maximumSunPercent)
            {
                failureReason = $"Lighting director sun range must stay inside 10-50%, got {minimumSunPercent:0.0}-{maximumSunPercent:0.0}.";
                return false;
            }

            if (transitionSpeedPercentPerSecond <= 0f || returnToOvercastSpeedPercentPerSecond <= 0f)
            {
                failureReason = "Lighting director transition speeds must be positive.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public string BuildDebugSummary()
        {
            return $"Light {state} Sun {currentSunPercent:0.0}% Target {targetSunPercent:0.0}%\nNext Window {FormatSeconds(secondsUntilNextWindow)} Active Window {FormatSeconds(activeWindowSecondsRemaining)}";
        }

        private void OnEnable()
        {
            EnsureInitialized();
            ApplyLighting();
        }

        private void Start()
        {
            EnsureInitialized();
            ApplyLighting();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                ApplyLighting();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleDevelopmentInput();
#endif

            Tick(Time.deltaTime);
        }

        private void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            EnsureInitialized();

            switch (state)
            {
                case LightingState.Overcast:
                    secondsUntilNextWindow = Mathf.Max(0f, secondsUntilNextWindow - deltaSeconds);

                    if (secondsUntilNextWindow <= 0f)
                    {
                        BeginCloudThinningWindow();
                    }
                    break;

                case LightingState.CloudsThinning:
                case LightingState.SunWavering:
                    activeWindowSecondsRemaining = Mathf.Max(0f, activeWindowSecondsRemaining - deltaSeconds);
                    targetSecondsRemaining = Mathf.Max(0f, targetSecondsRemaining - deltaSeconds);

                    if (targetSecondsRemaining <= 0f)
                    {
                        ChooseNextSunTarget();
                    }

                    currentSunPercent = MoveSunPercentToward(targetSunPercent, transitionSpeedPercentPerSecond, deltaSeconds);
                    state = LightingState.SunWavering;

                    if (activeWindowSecondsRemaining <= 0f)
                    {
                        state = LightingState.ReturningToOvercast;
                        targetSunPercent = 0f;
                        targetSecondsRemaining = 0f;
                    }
                    break;

                case LightingState.ReturningToOvercast:
                    currentSunPercent = MoveSunPercentToward(0f, returnToOvercastSpeedPercentPerSecond, deltaSeconds);

                    if (currentSunPercent <= 0.01f)
                    {
                        currentSunPercent = 0f;
                        targetSunPercent = 0f;
                        state = LightingState.Overcast;
                        secondsUntilNextWindow = RollRange(intervalRangeSeconds);
                    }
                    break;
            }

            ApplyLighting();
        }

        private void BeginCloudThinningWindow()
        {
            state = LightingState.CloudsThinning;
            activeWindowSecondsRemaining = RollRange(windowDurationRangeSeconds);
            ChooseNextSunTarget();
            ApplyLighting();
        }

        private void ChooseNextSunTarget()
        {
            targetSunPercent = RollRange(new Vector2(minimumSunPercent, maximumSunPercent));
            targetSecondsRemaining = RollRange(targetHoldRangeSeconds);
        }

        private float MoveSunPercentToward(float targetPercent, float speedPercentPerSecond, float deltaSeconds)
        {
            return Mathf.MoveTowards(currentSunPercent, targetPercent, Mathf.Max(0.01f, speedPercentPerSecond) * deltaSeconds);
        }

        private void ApplyLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = overcastAmbientSkyColor;
            RenderSettings.ambientEquatorColor = overcastAmbientEquatorColor;
            RenderSettings.ambientGroundColor = overcastAmbientGroundColor;
            RenderSettings.ambientIntensity = overcastAmbientIntensity;
            RenderSettings.subtractiveShadowColor = overcastSubtractiveShadowColor;

            if (directSun == null)
            {
                return;
            }

            float normalizedSun = Mathf.Clamp01(currentSunPercent / 100f);
            directSun.type = LightType.Directional;
            directSun.color = referenceSunColor;
            directSun.intensity = referenceDirectSunIntensity * normalizedSun;
            directSun.shadows = currentSunPercent <= 0.01f ? LightShadows.None : LightShadows.Soft;
            directSun.shadowStrength = referenceShadowStrength * Mathf.Clamp01(normalizedSun * 0.65f);
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            random = new System.Random(randomSeed);
            initialized = true;

            if (secondsUntilNextWindow <= 0f && state == LightingState.Overcast)
            {
                secondsUntilNextWindow = RollRange(intervalRangeSeconds);
            }
        }

        private float RollRange(Vector2 range)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            double unit = random == null ? 0.5d : random.NextDouble();
            return Mathf.Lerp(min, max, (float)unit);
        }

        private static string FormatSeconds(float seconds)
        {
            if (seconds <= 0f)
            {
                return "0s";
            }

            int wholeSeconds = Mathf.CeilToInt(seconds);
            int minutes = wholeSeconds / 60;
            int remainderSeconds = wholeSeconds % 60;
            return minutes > 0 ? $"{minutes:0}m {remainderSeconds:00}s" : $"{remainderSeconds:0}s";
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void HandleDevelopmentInput()
        {
            if (!enableDevelopmentHotkeys)
            {
                return;
            }

            if (forceCloudThinningKey != KeyCode.None && Input.GetKeyDown(forceCloudThinningKey))
            {
                ForceImmediateCloudThinningWindow();
            }

            if (forceReturnToOvercastKey != KeyCode.None && Input.GetKeyDown(forceReturnToOvercastKey))
            {
                ForceReturnToOvercast();
            }
        }
#endif
    }
}
