using UnityEngine;

namespace LostForest.Phase2.Player
{
    public sealed class FirstPersonCameraWalkBob : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private EarlyWalkThruFirstPersonController firstPersonController;
        [SerializeField] private PlayerTerrainMovementState terrainMovementState;

        [Header("U Bob")]
        [SerializeField] private bool enableWalkBob = true;
        [SerializeField] private float walkReferenceSpeedMetersPerSecond = 6.5f;
        [SerializeField] private float sprintReferenceSpeedMetersPerSecond = 10.5f;
        [SerializeField] private float cyclesPerMeter = 0.13f;
        [SerializeField] private float maxSideOffsetMeters = 0.026f;
        [SerializeField] private float maxVerticalDipMeters = 0.18f;
        [SerializeField] private float maxForwardOffsetMeters = 0.026f;
        [SerializeField] private float sprintAmplitudeMultiplier = 1.06f;
        [SerializeField] private float settleSpeed = 4.8f;

        private Vector3 neutralLocalPosition;
        private Vector3 currentOffset;
        private float phaseRadians;
        private bool hasNeutralPosition;

        public void SetCameraRoot(Transform newCameraRoot)
        {
            cameraRoot = newCameraRoot;
            CaptureNeutralPosition();
        }

        public void SetSources(EarlyWalkThruFirstPersonController controller, PlayerTerrainMovementState movementState)
        {
            firstPersonController = controller;
            terrainMovementState = movementState;
        }

        private void Awake()
        {
            DiscoverSourcesIfNeeded();
            CaptureNeutralPosition();
        }

        private void LateUpdate()
        {
            DiscoverSourcesIfNeeded();

            if (cameraRoot == null)
            {
                return;
            }

            if (!hasNeutralPosition)
            {
                CaptureNeutralPosition();
            }

            Vector3 targetOffset = ResolveTargetOffset();
            currentOffset = Vector3.Lerp(currentOffset, targetOffset, GetBlend01(settleSpeed));
            cameraRoot.localPosition = neutralLocalPosition + currentOffset;
        }

        private Vector3 ResolveTargetOffset()
        {
            if (!enableWalkBob || firstPersonController == null || terrainMovementState == null)
            {
                return Vector3.zero;
            }

            if (!firstPersonController.IsGrounded || !firstPersonController.IsMoving || terrainMovementState.FinalMovementSpeedMetersPerSecond <= 0.01f)
            {
                return Vector3.zero;
            }

            float speed = terrainMovementState.FinalMovementSpeedMetersPerSecond;
            float walkReferenceSpeed = Mathf.Max(0.01f, walkReferenceSpeedMetersPerSecond);
            float sprintReferenceSpeed = Mathf.Max(walkReferenceSpeed + 0.01f, sprintReferenceSpeedMetersPerSecond);
            float speed01 = Mathf.Clamp01(speed / sprintReferenceSpeed);
            float walk01 = Mathf.Clamp01(speed / walkReferenceSpeed);
            float amplitudeScale = Mathf.Lerp(
                Mathf.SmoothStep(0f, 1f, walk01) * 0.88f,
                1f,
                Mathf.SmoothStep(0f, 1f, speed01));
            float sprintScale = firstPersonController.IsSprinting ? sprintAmplitudeMultiplier : 1f;
            float cycleRate = Mathf.Max(0.01f, cyclesPerMeter) * speed;
            phaseRadians += cycleRate * Mathf.PI * 2f * Time.deltaTime;

            if (phaseRadians > Mathf.PI * 2f)
            {
                phaseRadians %= Mathf.PI * 2f;
            }

            float lateral = Mathf.Sin(phaseRadians) * 0.55f;
            float plantedStep = Mathf.Pow(Mathf.Abs(Mathf.Cos(phaseRadians)), 1.65f);
            float recoveryLift = Mathf.Max(0f, Mathf.Sin(phaseRadians)) * 0.18f;
            float verticalDip = -Mathf.Clamp01(plantedStep - recoveryLift);
            float forwardPulse = -Mathf.Sin(phaseRadians + Mathf.PI * 0.25f) * 0.55f;
            float sideOffset = lateral * maxSideOffsetMeters * amplitudeScale * sprintScale;
            float yOffset = verticalDip * maxVerticalDipMeters * amplitudeScale * sprintScale;
            float zOffset = forwardPulse * maxForwardOffsetMeters * amplitudeScale * sprintScale;
            return new Vector3(sideOffset, yOffset, zOffset);
        }

        private void DiscoverSourcesIfNeeded()
        {
            if (firstPersonController == null)
            {
                firstPersonController = GetComponent<EarlyWalkThruFirstPersonController>();
            }

            if (terrainMovementState == null)
            {
                terrainMovementState = GetComponent<PlayerTerrainMovementState>();
            }

            if (cameraRoot == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                cameraRoot = childCamera == null ? null : childCamera.transform;
            }
        }

        private void CaptureNeutralPosition()
        {
            if (cameraRoot == null)
            {
                hasNeutralPosition = false;
                return;
            }

            neutralLocalPosition = cameraRoot.localPosition;
            currentOffset = Vector3.zero;
            hasNeutralPosition = true;
        }

        private static float GetBlend01(float speed)
        {
            return 1f - Mathf.Exp(-Mathf.Max(0.01f, speed) * Time.deltaTime);
        }
    }
}
