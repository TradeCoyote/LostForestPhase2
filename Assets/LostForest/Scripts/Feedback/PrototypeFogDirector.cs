using UnityEngine;

namespace LostForest.Phase2.Feedback
{
    [ExecuteAlways]
    public sealed class PrototypeFogDirector : MonoBehaviour
    {
        public enum FogCycleState
        {
            Normal,
            Thickening,
            Whiteout,
            Clearing
        }

        [Header("Normal Distance Fog")]
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private bool fogEnabled = true;
        [SerializeField] private FogMode fogMode = FogMode.Linear;
        [SerializeField] private Color fogColor = new Color(0.88f, 0.94f, 0.97f, 1f);
        [SerializeField] private float fogStartDistanceMeters = 5f;
        [SerializeField] private float fogEndDistanceMeters = 70f;
        [SerializeField] private Vector2 normalFogEndDistanceRangeMeters = new Vector2(50f, 70f);
        [SerializeField] private float normalFogWaverSpeedMetersPerSecond = 0.35f;
        [SerializeField] private float exponentialDensity = 0.02f;

        [Header("Rare Whiteout")]
        [SerializeField] private Vector2 whiteoutIntervalRangeSeconds = new Vector2(360f, 720f);
        [SerializeField] private Vector2 whiteoutHoldRangeSeconds = new Vector2(30f, 60f);
        [SerializeField] private float whiteoutFadeInSeconds = 15f;
        [SerializeField] private float whiteoutFadeOutSeconds = 12f;
        [SerializeField] private float whiteoutFogStartDistanceMeters;
        [SerializeField] private float whiteoutFogEndDistanceMeters = 0.65f;
        [SerializeField] private Color whiteoutFogColor = new Color(0.985f, 0.995f, 1f, 1f);
        [SerializeField] private int randomSeed = 20260812;

        [Header("Whiteout Wavering Views")]
        [SerializeField] private Vector2Int whiteoutGlimpseCountRange = new Vector2Int(1, 2);
        [SerializeField] private Vector2 whiteoutGlimpseDurationRangeSeconds = new Vector2(5f, 7f);
        [SerializeField, Range(0f, 0.25f)] private float whiteoutGlimpseVisibilityFraction = 0.1f;
        [SerializeField, Range(0f, 0.1f)] private float whiteoutGlimpseTimingJitter = 0.04f;

        [Header("Camera Backdrop")]
        [SerializeField] private bool tintMainCameraBackground = true;
        [SerializeField] private bool forceSolidFogBackground = true;

        [Header("Development Debug Commands")]
        [SerializeField] private bool enableDevelopmentHotkeys = true;
        [SerializeField] private KeyCode forceWhiteoutKey = KeyCode.F5;
        [SerializeField] private KeyCode forceClearKey = KeyCode.F6;

        private System.Random random;
        private FogCycleState state = FogCycleState.Normal;
        private float currentNormalFogEndDistanceMeters = 70f;
        private float targetNormalFogEndDistanceMeters = 60f;
        private float currentWhiteoutIntensity;
        private float secondsUntilNextWhiteout;
        private float whiteoutHoldSecondsRemaining;
        private float whiteoutHoldTotalSeconds;
        private float firstGlimpseCenterSeconds;
        private float secondGlimpseCenterSeconds;
        private float firstGlimpseDurationSeconds;
        private float secondGlimpseDurationSeconds;
        private int whiteoutGlimpseCount;
        private float transitionSecondsElapsed;
        private bool initialized;

        public FogCycleState CurrentState => state;
        public float CurrentNormalFogEndDistanceMeters => currentNormalFogEndDistanceMeters;
        public float TargetNormalFogEndDistanceMeters => targetNormalFogEndDistanceMeters;
        public float CurrentAppliedFogEndDistanceMeters { get; private set; }
        public float CurrentWhiteoutIntensity => currentWhiteoutIntensity;
        public float SecondsUntilNextWhiteout => secondsUntilNextWhiteout;
        public float WhiteoutHoldSecondsRemaining => whiteoutHoldSecondsRemaining;
        public Vector2 NormalFogEndDistanceRangeMeters => normalFogEndDistanceRangeMeters;
        public Vector2 WhiteoutIntervalRangeSeconds => whiteoutIntervalRangeSeconds;
        public Vector2 WhiteoutHoldRangeSeconds => whiteoutHoldRangeSeconds;
        public float WhiteoutFadeInSeconds => whiteoutFadeInSeconds;
        public int WhiteoutGlimpseCount => whiteoutGlimpseCount;
        public float WhiteoutGlimpseVisibilityFraction => whiteoutGlimpseVisibilityFraction;
        public float CurrentWhiteoutGlimpseVisibility => state == FogCycleState.Whiteout ? 1f - currentWhiteoutIntensity : 0f;

        public void ApplyEarlyFogDefaults()
        {
            fogEnabled = true;
            fogMode = FogMode.Linear;
            fogColor = new Color(0.88f, 0.94f, 0.97f, 1f);
            fogStartDistanceMeters = 5f;
            fogEndDistanceMeters = 70f;
            normalFogEndDistanceRangeMeters = new Vector2(50f, 70f);
            normalFogWaverSpeedMetersPerSecond = 0.35f;
            exponentialDensity = 0.02f;
            whiteoutIntervalRangeSeconds = new Vector2(360f, 720f);
            whiteoutHoldRangeSeconds = new Vector2(30f, 60f);
            whiteoutFadeInSeconds = 15f;
            whiteoutFadeOutSeconds = 12f;
            whiteoutFogStartDistanceMeters = 0f;
            whiteoutFogEndDistanceMeters = 0.65f;
            whiteoutFogColor = new Color(0.985f, 0.995f, 1f, 1f);
            randomSeed = 20260812;
            whiteoutGlimpseCountRange = new Vector2Int(1, 2);
            whiteoutGlimpseDurationRangeSeconds = new Vector2(5f, 7f);
            whiteoutGlimpseVisibilityFraction = 0.1f;
            whiteoutGlimpseTimingJitter = 0.04f;
            tintMainCameraBackground = true;
            forceSolidFogBackground = true;
            initialized = false;
        }

        public void ResetToNormalAndScheduleNextWhiteout()
        {
            random = new System.Random(randomSeed);
            initialized = true;
            state = FogCycleState.Normal;
            currentNormalFogEndDistanceMeters = Mathf.Clamp(
                fogEndDistanceMeters,
                normalFogEndDistanceRangeMeters.x,
                normalFogEndDistanceRangeMeters.y);
            targetNormalFogEndDistanceMeters = RollDistinctNormalFogTarget(currentNormalFogEndDistanceMeters);
            currentWhiteoutIntensity = 0f;
            whiteoutHoldSecondsRemaining = 0f;
            ResetWhiteoutGlimpses();
            transitionSecondsElapsed = 0f;
            secondsUntilNextWhiteout = RollRange(whiteoutIntervalRangeSeconds);
            ApplyCurrentFogSettings();
        }

        public void TickForValidation(float deltaSeconds)
        {
            Tick(deltaSeconds);
        }

        [ContextMenu("Force Immediate Whiteout")]
        public void ForceImmediateWhiteout()
        {
            EnsureInitialized();
            BeginWhiteout();
        }

        [ContextMenu("Force Fog Back To Normal")]
        public void ForceReturnToNormal()
        {
            EnsureInitialized();

            if (currentWhiteoutIntensity <= 0.001f)
            {
                CompleteWhiteoutCycle();
                return;
            }

            state = FogCycleState.Clearing;
            transitionSecondsElapsed = (1f - currentWhiteoutIntensity) * Mathf.Max(0.01f, whiteoutFadeOutSeconds);
            whiteoutHoldSecondsRemaining = 0f;
        }

        public bool ValidateConfiguration(out string failureReason)
        {
            if (!IsOrderedRange(normalFogEndDistanceRangeMeters, 50f, 70f))
            {
                failureReason = $"Normal fog must waver inside 50-70 meters, got {normalFogEndDistanceRangeMeters.x:0.0}-{normalFogEndDistanceRangeMeters.y:0.0}.";
                return false;
            }

            if (!IsOrderedRange(whiteoutIntervalRangeSeconds, 360f, 720f))
            {
                failureReason = $"Whiteout interval must stay inside 360-720 seconds, got {whiteoutIntervalRangeSeconds.x:0.0}-{whiteoutIntervalRangeSeconds.y:0.0}.";
                return false;
            }

            if (!IsOrderedRange(whiteoutHoldRangeSeconds, 30f, 60f))
            {
                failureReason = $"Whiteout hold must stay inside 30-60 seconds, got {whiteoutHoldRangeSeconds.x:0.0}-{whiteoutHoldRangeSeconds.y:0.0}.";
                return false;
            }

            if (normalFogWaverSpeedMetersPerSecond <= 0f || whiteoutFadeInSeconds <= 0f || whiteoutFadeOutSeconds <= 0f)
            {
                failureReason = "Fog waver and whiteout fade speeds must be positive.";
                return false;
            }

            if (Mathf.Abs(whiteoutFadeInSeconds - 15f) > 0.001f)
            {
                failureReason = $"Whiteout thickening must take 15 seconds, got {whiteoutFadeInSeconds:0.0}.";
                return false;
            }

            if (whiteoutFogEndDistanceMeters > 1f || whiteoutFogEndDistanceMeters <= whiteoutFogStartDistanceMeters)
            {
                failureReason = $"Whiteout visibility must end within one meter, got {whiteoutFogStartDistanceMeters:0.00}-{whiteoutFogEndDistanceMeters:0.00}.";
                return false;
            }

            if (whiteoutGlimpseCountRange.x != 1 || whiteoutGlimpseCountRange.y != 2)
            {
                failureReason = $"Whiteouts must contain one or two wavering views, got {whiteoutGlimpseCountRange.x}-{whiteoutGlimpseCountRange.y}.";
                return false;
            }

            if (!IsOrderedRange(whiteoutGlimpseDurationRangeSeconds, 4f, 8f))
            {
                failureReason = $"Whiteout wavering views must last 4-8 seconds, got {whiteoutGlimpseDurationRangeSeconds.x:0.0}-{whiteoutGlimpseDurationRangeSeconds.y:0.0}.";
                return false;
            }

            if (whiteoutGlimpseVisibilityFraction < 0.08f || whiteoutGlimpseVisibilityFraction > 0.12f)
            {
                failureReason = $"Whiteout wavering views must restore about 10% visibility, got {whiteoutGlimpseVisibilityFraction * 100f:0.0}%.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public string BuildDebugSummary()
        {
            return $"Fog {state} End {CurrentAppliedFogEndDistanceMeters:0.0}m Normal {currentNormalFogEndDistanceMeters:0.0}->{targetNormalFogEndDistanceMeters:0.0}m\nWhiteout {currentWhiteoutIntensity * 100f:0}% Next {FormatSeconds(secondsUntilNextWhiteout)} Hold {FormatSeconds(whiteoutHoldSecondsRemaining)} Views {whiteoutGlimpseCount}";
        }

        private void OnEnable()
        {
            EnsureInitialized();

            if (applyOnEnable)
            {
                ApplyCurrentFogSettings();
            }
        }

        private void Start()
        {
            EnsureInitialized();
            ApplyCurrentFogSettings();
        }

        private void OnValidate()
        {
            normalFogEndDistanceRangeMeters = SortRange(normalFogEndDistanceRangeMeters, new Vector2(50f, 70f));
            whiteoutIntervalRangeSeconds = SortRange(whiteoutIntervalRangeSeconds, new Vector2(360f, 720f));
            whiteoutHoldRangeSeconds = SortRange(whiteoutHoldRangeSeconds, new Vector2(30f, 60f));
            normalFogWaverSpeedMetersPerSecond = Mathf.Max(0.01f, normalFogWaverSpeedMetersPerSecond);
            whiteoutFadeInSeconds = Mathf.Max(0.01f, whiteoutFadeInSeconds);
            whiteoutFadeOutSeconds = Mathf.Max(0.01f, whiteoutFadeOutSeconds);
            whiteoutFogStartDistanceMeters = Mathf.Max(0f, whiteoutFogStartDistanceMeters);
            whiteoutFogEndDistanceMeters = Mathf.Max(whiteoutFogStartDistanceMeters + 0.05f, whiteoutFogEndDistanceMeters);
            whiteoutGlimpseCountRange.x = Mathf.Clamp(whiteoutGlimpseCountRange.x, 1, 2);
            whiteoutGlimpseCountRange.y = Mathf.Clamp(whiteoutGlimpseCountRange.y, whiteoutGlimpseCountRange.x, 2);
            whiteoutGlimpseDurationRangeSeconds = SortRange(whiteoutGlimpseDurationRangeSeconds, new Vector2(5f, 7f));
            whiteoutGlimpseVisibilityFraction = Mathf.Clamp(whiteoutGlimpseVisibilityFraction, 0.08f, 0.12f);
        }

        private void Update()
        {
            EnsureInitialized();

            if (!Application.isPlaying)
            {
                ApplyCurrentFogSettings();
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
            UpdateNormalFogWaver(deltaSeconds);

            switch (state)
            {
                case FogCycleState.Normal:
                    secondsUntilNextWhiteout = Mathf.Max(0f, secondsUntilNextWhiteout - deltaSeconds);

                    if (secondsUntilNextWhiteout <= 0f)
                    {
                        BeginWhiteout();
                    }
                    break;

                case FogCycleState.Thickening:
                    transitionSecondsElapsed += deltaSeconds;
                    currentWhiteoutIntensity = Smooth01(transitionSecondsElapsed / Mathf.Max(0.01f, whiteoutFadeInSeconds));

                    if (currentWhiteoutIntensity >= 0.999f)
                    {
                        currentWhiteoutIntensity = 1f;
                        state = FogCycleState.Whiteout;
                        BeginWhiteoutHold();
                    }
                    break;

                case FogCycleState.Whiteout:
                    whiteoutHoldSecondsRemaining = Mathf.Max(0f, whiteoutHoldSecondsRemaining - deltaSeconds);
                    UpdateWhiteoutGlimpses();

                    if (whiteoutHoldSecondsRemaining <= 0f)
                    {
                        currentWhiteoutIntensity = 1f;
                        state = FogCycleState.Clearing;
                        transitionSecondsElapsed = 0f;
                    }
                    break;

                case FogCycleState.Clearing:
                    transitionSecondsElapsed += deltaSeconds;
                    currentWhiteoutIntensity = 1f - Smooth01(transitionSecondsElapsed / Mathf.Max(0.01f, whiteoutFadeOutSeconds));

                    if (currentWhiteoutIntensity <= 0.001f)
                    {
                        CompleteWhiteoutCycle();
                    }
                    break;
            }

            ApplyCurrentFogSettings();
        }

        private void UpdateNormalFogWaver(float deltaSeconds)
        {
            currentNormalFogEndDistanceMeters = Mathf.MoveTowards(
                currentNormalFogEndDistanceMeters,
                targetNormalFogEndDistanceMeters,
                normalFogWaverSpeedMetersPerSecond * deltaSeconds);

            if (Mathf.Abs(currentNormalFogEndDistanceMeters - targetNormalFogEndDistanceMeters) <= 0.05f)
            {
                targetNormalFogEndDistanceMeters = RollDistinctNormalFogTarget(currentNormalFogEndDistanceMeters);
            }
        }

        private void BeginWhiteout()
        {
            state = FogCycleState.Thickening;
            transitionSecondsElapsed = 0f;
            currentWhiteoutIntensity = 0f;
            secondsUntilNextWhiteout = 0f;
            whiteoutHoldSecondsRemaining = 0f;
            ResetWhiteoutGlimpses();
        }

        private void BeginWhiteoutHold()
        {
            whiteoutHoldTotalSeconds = RollRange(whiteoutHoldRangeSeconds);
            whiteoutHoldSecondsRemaining = whiteoutHoldTotalSeconds;
            whiteoutGlimpseCount = random.Next(whiteoutGlimpseCountRange.x, whiteoutGlimpseCountRange.y + 1);

            firstGlimpseDurationSeconds = RollRange(whiteoutGlimpseDurationRangeSeconds);
            secondGlimpseDurationSeconds = whiteoutGlimpseCount > 1
                ? RollRange(whiteoutGlimpseDurationRangeSeconds)
                : 0f;

            if (whiteoutGlimpseCount == 1)
            {
                firstGlimpseCenterSeconds = whiteoutHoldTotalSeconds * (0.5f + RollSignedTimingJitter());
                secondGlimpseCenterSeconds = 0f;
                return;
            }

            firstGlimpseCenterSeconds = whiteoutHoldTotalSeconds * (0.33f + RollSignedTimingJitter());
            secondGlimpseCenterSeconds = whiteoutHoldTotalSeconds * (0.67f + RollSignedTimingJitter());
        }

        private void UpdateWhiteoutGlimpses()
        {
            float elapsedSeconds = Mathf.Max(0f, whiteoutHoldTotalSeconds - whiteoutHoldSecondsRemaining);
            float glimpseEnvelope = EvaluateGlimpseEnvelope(elapsedSeconds, firstGlimpseCenterSeconds, firstGlimpseDurationSeconds);

            if (whiteoutGlimpseCount > 1)
            {
                glimpseEnvelope = Mathf.Max(
                    glimpseEnvelope,
                    EvaluateGlimpseEnvelope(elapsedSeconds, secondGlimpseCenterSeconds, secondGlimpseDurationSeconds));
            }

            currentWhiteoutIntensity = 1f - whiteoutGlimpseVisibilityFraction * glimpseEnvelope;
        }

        private void ResetWhiteoutGlimpses()
        {
            whiteoutHoldTotalSeconds = 0f;
            firstGlimpseCenterSeconds = 0f;
            secondGlimpseCenterSeconds = 0f;
            firstGlimpseDurationSeconds = 0f;
            secondGlimpseDurationSeconds = 0f;
            whiteoutGlimpseCount = 0;
        }

        private float RollSignedTimingJitter()
        {
            return Mathf.Lerp(-whiteoutGlimpseTimingJitter, whiteoutGlimpseTimingJitter, (float)random.NextDouble());
        }

        private static float EvaluateGlimpseEnvelope(float elapsedSeconds, float centerSeconds, float durationSeconds)
        {
            if (durationSeconds <= 0f)
            {
                return 0f;
            }

            float halfDuration = durationSeconds * 0.5f;
            float distanceFromCenter = Mathf.Abs(elapsedSeconds - centerSeconds);
            return Smooth01(1f - distanceFromCenter / Mathf.Max(0.01f, halfDuration));
        }

        private void CompleteWhiteoutCycle()
        {
            state = FogCycleState.Normal;
            transitionSecondsElapsed = 0f;
            currentWhiteoutIntensity = 0f;
            whiteoutHoldSecondsRemaining = 0f;
            ResetWhiteoutGlimpses();
            secondsUntilNextWhiteout = RollRange(whiteoutIntervalRangeSeconds);
        }

        [ContextMenu("Apply Prototype Fog Settings")]
        public void ApplyFogSettings()
        {
            EnsureInitialized();
            ApplyCurrentFogSettings();
        }

        private void ApplyCurrentFogSettings()
        {
            float intensity = Mathf.Clamp01(currentWhiteoutIntensity);
            float startDistance = Mathf.Lerp(fogStartDistanceMeters, whiteoutFogStartDistanceMeters, intensity);
            float endDistance = Mathf.Lerp(currentNormalFogEndDistanceMeters, whiteoutFogEndDistanceMeters, intensity);
            Color currentFogColor = Color.Lerp(fogColor, whiteoutFogColor, intensity);

            RenderSettings.fog = fogEnabled;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = currentFogColor;
            RenderSettings.fogStartDistance = Mathf.Max(0f, startDistance);
            RenderSettings.fogEndDistance = Mathf.Max(RenderSettings.fogStartDistance + 0.05f, endDistance);
            RenderSettings.fogDensity = Mathf.Max(0f, exponentialDensity);
            CurrentAppliedFogEndDistanceMeters = RenderSettings.fogEndDistance;

            if (tintMainCameraBackground && Camera.main != null)
            {
                if (forceSolidFogBackground)
                {
                    Camera.main.clearFlags = CameraClearFlags.SolidColor;
                }

                Camera.main.backgroundColor = currentFogColor;
            }
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                ResetToNormalAndScheduleNextWhiteout();
            }
        }

        private float RollDistinctNormalFogTarget(float currentDistance)
        {
            float minimumDifference = Mathf.Min(4f, (normalFogEndDistanceRangeMeters.y - normalFogEndDistanceRangeMeters.x) * 0.25f);
            float target = RollRange(normalFogEndDistanceRangeMeters);

            for (int attempt = 0; attempt < 5 && Mathf.Abs(target - currentDistance) < minimumDifference; attempt++)
            {
                target = RollRange(normalFogEndDistanceRangeMeters);
            }

            if (Mathf.Abs(target - currentDistance) < minimumDifference)
            {
                target = currentDistance <= (normalFogEndDistanceRangeMeters.x + normalFogEndDistanceRangeMeters.y) * 0.5f
                    ? normalFogEndDistanceRangeMeters.y
                    : normalFogEndDistanceRangeMeters.x;
            }

            return target;
        }

        private float RollRange(Vector2 range)
        {
            return Mathf.Lerp(range.x, range.y, (float)random.NextDouble());
        }

        private void HandleDevelopmentInput()
        {
            if (!enableDevelopmentHotkeys)
            {
                return;
            }

            if (Input.GetKeyDown(forceWhiteoutKey))
            {
                ForceImmediateWhiteout();
            }

            if (Input.GetKeyDown(forceClearKey))
            {
                ForceReturnToNormal();
            }
        }

        private static bool IsOrderedRange(Vector2 range, float minimum, float maximum)
        {
            return range.x >= minimum && range.y <= maximum && range.x <= range.y;
        }

        private static Vector2 SortRange(Vector2 range, Vector2 fallback)
        {
            if (range.x <= 0f && range.y <= 0f)
            {
                return fallback;
            }

            range.x = Mathf.Max(0f, range.x);
            range.y = Mathf.Max(0f, range.y);
            return range.x <= range.y ? range : new Vector2(range.y, range.x);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static string FormatSeconds(float seconds)
        {
            if (seconds <= 0f)
            {
                return "0s";
            }

            int roundedSeconds = Mathf.CeilToInt(seconds);
            int minutes = roundedSeconds / 60;
            int remainder = roundedSeconds % 60;
            return minutes > 0 ? $"{minutes}m {remainder:00}s" : $"{remainder}s";
        }
    }
}
