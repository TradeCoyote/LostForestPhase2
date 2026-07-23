using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Player
{
    public sealed class PlayerTerrainMovementState : MonoBehaviour
    {
        [Header("Terrain Sources")]
        [SerializeField] private PlayerGridAddressTracker gridAddressTracker;
        [SerializeField] private ActiveRegionRenderer activeRegionRenderer;

        [Header("Slope Speed Tuning")]
        [SerializeField] private float flatThresholdDegrees = 3f;
        [SerializeField] private float steepThresholdDegrees = 30f;
        [SerializeField] private float maxUphillSlowdownMultiplier = 0.5f;
        [SerializeField] private float maxDownhillBoostMultiplier = 1.65f;
        [SerializeField] private float maxSprintDownhillBoostMultiplier = 1.45f;
        [SerializeField] private float minSpeedMultiplier = 0.5f;
        [SerializeField] private float maxSpeedMultiplier = 1.75f;
        [SerializeField] private float multiplierSmoothingSpeed = 8f;

        public bool HasTerrainSample { get; private set; }
        public bool HasMovementIntent { get; private set; }
        public TerrainElevationSample CurrentElevationSample { get; private set; }
        public Vector3 MovementIntentWorldDirection { get; private set; }
        public float MovementIntentMagnitude01 { get; private set; }
        public float CurrentSlopeDegrees { get; private set; }
        public float SignedMovementGradeDegrees { get; private set; }
        public float SignedMovementGradeNormalized { get; private set; }
        public PlayerSlopeTravelState TravelState { get; private set; } = PlayerSlopeTravelState.Unknown;
        public float SpeedMultiplier { get; private set; } = 1f;
        public float TargetSpeedMultiplier { get; private set; } = 1f;
        public float BaseMovementSpeedMetersPerSecond { get; private set; }
        public float FinalMovementSpeedMetersPerSecond { get; private set; }
        public bool WantsSprint { get; private set; }
        public bool IsSprinting { get; private set; }

        public float FlatThresholdDegrees => Mathf.Max(0f, flatThresholdDegrees);
        public float SteepThresholdDegrees => Mathf.Max(FlatThresholdDegrees + 0.01f, steepThresholdDegrees);
        public float MaxUphillSlowdownMultiplier => Mathf.Clamp(maxUphillSlowdownMultiplier, 0.01f, 1f);
        public float MaxDownhillBoostMultiplier => Mathf.Max(1f, maxDownhillBoostMultiplier);
        public float MaxSprintDownhillBoostMultiplier => Mathf.Max(1f, maxSprintDownhillBoostMultiplier);

        public void SetSources(PlayerGridAddressTracker newGridAddressTracker, ActiveRegionRenderer newActiveRegionRenderer)
        {
            gridAddressTracker = newGridAddressTracker;
            activeRegionRenderer = newActiveRegionRenderer;
        }

        public float EvaluateMovement(
            Vector3 movementIntentWorldDirection,
            float movementIntentMagnitude01,
            float baseMovementSpeedMetersPerSecond,
            bool wantsSprint,
            bool isSprinting,
            float deltaTime)
        {
            DiscoverSourcesIfNeeded();

            BaseMovementSpeedMetersPerSecond = Mathf.Max(0f, baseMovementSpeedMetersPerSecond);
            MovementIntentMagnitude01 = Mathf.Clamp01(movementIntentMagnitude01);
            WantsSprint = wantsSprint;
            IsSprinting = isSprinting;
            MovementIntentWorldDirection = FlattenDirection(movementIntentWorldDirection);
            HasMovementIntent = MovementIntentMagnitude01 > 0.001f && MovementIntentWorldDirection.sqrMagnitude > 0.0001f;

            UpdateTerrainSample();
            TargetSpeedMultiplier = ResolveTargetSpeedMultiplier();
            SpeedMultiplier = ResolveSmoothedMultiplier(TargetSpeedMultiplier, deltaTime);
            FinalMovementSpeedMetersPerSecond = HasMovementIntent
                ? BaseMovementSpeedMetersPerSecond * SpeedMultiplier * MovementIntentMagnitude01
                : 0f;

            return FinalMovementSpeedMetersPerSecond;
        }

        public string BuildDebugSummary()
        {
            if (!HasTerrainSample)
            {
                return $"Move terrain --\nGrade --\nMult {SpeedMultiplier:0.00} Speed {FinalMovementSpeedMetersPerSecond:0.0}\nSprint {IsSprinting}";
            }

            return $"Move {TravelState} Slope {CurrentSlopeDegrees:0}deg\nGrade {SignedMovementGradeDegrees:+0.0;-0.0;0.0}deg ({SignedMovementGradeNormalized:+0.00;-0.00;0.00})\nMult {SpeedMultiplier:0.00} Speed {FinalMovementSpeedMetersPerSecond:0.0}\nSprint {IsSprinting}";
        }

        private void Awake()
        {
            DiscoverSourcesIfNeeded();
        }

        private void UpdateTerrainSample()
        {
            HasTerrainSample = false;
            CurrentElevationSample = default;
            CurrentSlopeDegrees = 0f;

            if (gridAddressTracker == null || activeRegionRenderer == null || gridAddressTracker.CurrentSlot == null)
            {
                SignedMovementGradeDegrees = 0f;
                SignedMovementGradeNormalized = 0f;
                TravelState = PlayerSlopeTravelState.Unknown;
                return;
            }

            if (!activeRegionRenderer.TrySampleTerrainElevation(gridAddressTracker.CurrentSlot, transform.position, out TerrainElevationSample elevationSample))
            {
                SignedMovementGradeDegrees = 0f;
                SignedMovementGradeNormalized = 0f;
                TravelState = PlayerSlopeTravelState.Unknown;
                return;
            }

            HasTerrainSample = true;
            CurrentElevationSample = elevationSample;
            CurrentSlopeDegrees = Mathf.Max(0f, elevationSample.SlopeDegrees);
            ResolveDirectionalGrade(elevationSample);
        }

        private float ResolveTargetSpeedMultiplier()
        {
            if (!HasTerrainSample || !HasMovementIntent)
            {
                return 1f;
            }

            float gradeMagnitudeDegrees = Mathf.Abs(SignedMovementGradeDegrees);

            if (gradeMagnitudeDegrees <= FlatThresholdDegrees)
            {
                return 1f;
            }

            float grade01 = Mathf.InverseLerp(FlatThresholdDegrees, SteepThresholdDegrees, gradeMagnitudeDegrees);
            float targetMultiplier = 1f;

            if (SignedMovementGradeDegrees > 0f)
            {
                targetMultiplier = Mathf.Lerp(1f, MaxUphillSlowdownMultiplier, grade01);
            }
            else if (SignedMovementGradeDegrees < 0f)
            {
                targetMultiplier = Mathf.Lerp(1f, MaxDownhillBoostMultiplier, grade01);

                if (IsSprinting)
                {
                    targetMultiplier = Mathf.Min(targetMultiplier, MaxSprintDownhillBoostMultiplier);
                }
            }

            return ClampSpeedMultiplier(targetMultiplier);
        }

        private void ResolveDirectionalGrade(TerrainElevationSample elevationSample)
        {
            if (!HasMovementIntent)
            {
                SignedMovementGradeDegrees = 0f;
                SignedMovementGradeNormalized = 0f;
                TravelState = PlayerSlopeTravelState.Flat;
                return;
            }

            Vector3 uphillDirection = elevationSample.UphillDirection;

            if (uphillDirection.sqrMagnitude <= 0.0001f || CurrentSlopeDegrees <= 0.001f)
            {
                SignedMovementGradeDegrees = 0f;
                SignedMovementGradeNormalized = 0f;
                TravelState = PlayerSlopeTravelState.Flat;
                return;
            }

            float uphillAlignment = Mathf.Clamp(Vector3.Dot(MovementIntentWorldDirection, uphillDirection), -1f, 1f);
            SignedMovementGradeDegrees = CurrentSlopeDegrees * uphillAlignment;

            if (Mathf.Abs(SignedMovementGradeDegrees) <= FlatThresholdDegrees)
            {
                TravelState = PlayerSlopeTravelState.Flat;
            }
            else
            {
                TravelState = SignedMovementGradeDegrees > 0f
                    ? PlayerSlopeTravelState.Uphill
                    : PlayerSlopeTravelState.Downhill;
            }

            SignedMovementGradeNormalized = Mathf.Clamp(
                SignedMovementGradeDegrees / SteepThresholdDegrees,
                -1f,
                1f);
        }

        private float ResolveSmoothedMultiplier(float targetMultiplier, float deltaTime)
        {
            if (deltaTime <= 0f || multiplierSmoothingSpeed <= 0f)
            {
                return ClampSpeedMultiplier(targetMultiplier);
            }

            float maxDelta = Mathf.Max(0.01f, multiplierSmoothingSpeed) * deltaTime;
            return ClampSpeedMultiplier(Mathf.MoveTowards(SpeedMultiplier, targetMultiplier, maxDelta));
        }

        private float ClampSpeedMultiplier(float multiplier)
        {
            float lower = Mathf.Max(0.01f, minSpeedMultiplier);
            float upper = Mathf.Max(1f, maxSpeedMultiplier);
            return Mathf.Clamp(multiplier, lower, upper);
        }

        private void DiscoverSourcesIfNeeded()
        {
            if (gridAddressTracker == null)
            {
                gridAddressTracker = GetComponent<PlayerGridAddressTracker>();
            }

            if (activeRegionRenderer == null)
            {
                activeRegionRenderer = FindAnyObjectByType<ActiveRegionRenderer>();
            }
        }

        private static Vector3 FlattenDirection(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            return direction.normalized;
        }
    }
}
