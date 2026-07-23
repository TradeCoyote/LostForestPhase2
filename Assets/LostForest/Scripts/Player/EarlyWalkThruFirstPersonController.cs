using UnityEngine;

namespace LostForest.Phase2.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerTerrainMovementState))]
    [RequireComponent(typeof(FirstPersonCameraWalkBob))]
    public sealed class EarlyWalkThruFirstPersonController : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private float mouseSensitivity = 1.4f;
        [SerializeField] private float minPitchDegrees = -80f;
        [SerializeField] private float maxPitchDegrees = 80f;
        [SerializeField] private bool lockCursorOnPlay = true;

        [Header("Movement")]
        [SerializeField] private float walkSpeedMetersPerSecond = 6.5f;
        [SerializeField] private float sprintSpeedMetersPerSecond = 10.5f;
        [SerializeField] private KeyCode sprintKey = KeyCode.Space;
        [SerializeField] private PlayerCondition playerCondition;
        [SerializeField] private PlayerTerrainMovementState terrainMovementState;
        [SerializeField] private float gravityMetersPerSecondSquared = -24f;
        [SerializeField] private float groundedDownwardVelocity = -2f;

        [Header("Debug")]
        [SerializeField] private bool logSprintTransitions = true;

        private CharacterController characterController;
        private float pitchDegrees;
        private float verticalVelocity;
        private bool lastLoggedWantsSprint;
        private bool lastLoggedIsSprinting;

        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public bool IsMoving { get; private set; }
        public bool WantsSprint { get; private set; }
        public bool IsSprinting { get; private set; }
        public PlayerTerrainMovementState TerrainMovementState => terrainMovementState;
        public float TerrainAdjustedMovementSpeedMetersPerSecond { get; private set; }
        public float ConditionSpeedMultiplier => playerCondition == null ? 1f : playerCondition.ConditionSpeedMultiplier;
        public float FinalMovementSpeedMetersPerSecond { get; private set; }

        public void SetCameraRoot(Transform newCameraRoot)
        {
            cameraRoot = newCameraRoot;
        }

        public void SetPlayerCondition(PlayerCondition newPlayerCondition)
        {
            playerCondition = newPlayerCondition;
        }

        public void SetPlayerTerrainMovementState(PlayerTerrainMovementState newTerrainMovementState)
        {
            terrainMovementState = newTerrainMovementState;
        }

        public void SetSprintKey(KeyCode newSprintKey)
        {
            sprintKey = newSprintKey;
        }

        public void ResetVerticalVelocity()
        {
            verticalVelocity = 0f;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (cameraRoot == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                cameraRoot = childCamera == null ? null : childCamera.transform;
            }

            if (playerCondition == null)
            {
                playerCondition = GetComponent<PlayerCondition>();
            }

            if (terrainMovementState == null)
            {
                terrainMovementState = GetComponent<PlayerTerrainMovementState>();

                if (terrainMovementState == null)
                {
                    terrainMovementState = gameObject.AddComponent<PlayerTerrainMovementState>();
                }
            }
        }

        private void Start()
        {
            if (lockCursorOnPlay)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            HandleMouseLook();
            HandleWasdMovement();
        }

        private void HandleMouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up, mouseX, Space.World);

            pitchDegrees = Mathf.Clamp(pitchDegrees - mouseY, minPitchDegrees, maxPitchDegrees);

            if (cameraRoot != null)
            {
                cameraRoot.localRotation = Quaternion.Euler(pitchDegrees, 0f, 0f);
            }
        }

        private void HandleWasdMovement()
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedDownwardVelocity;
            }

            Vector2 input = ReadWasdInput();
            input = Vector2.ClampMagnitude(input, 1f);
            bool isFrozen = playerCondition != null && playerCondition.IsFrozen;

            if (isFrozen)
            {
                input = Vector2.zero;
            }

            IsMoving = input.sqrMagnitude > 0.001f;
            WantsSprint = !isFrozen && IsMoving && IsSprintInputHeld();
            IsSprinting = WantsSprint && (playerCondition == null || playerCondition.CanSprint);

            float baseSpeedMetersPerSecond = IsSprinting ? sprintSpeedMetersPerSecond : walkSpeedMetersPerSecond;
            float inputMagnitude = Mathf.Clamp01(input.magnitude);
            Vector3 movementDirection = FlattenPlanarDirection(transform.right * input.x + transform.forward * input.y);
            TerrainAdjustedMovementSpeedMetersPerSecond = terrainMovementState == null
                ? baseSpeedMetersPerSecond * inputMagnitude
                : terrainMovementState.EvaluateMovement(
                    movementDirection,
                    inputMagnitude,
                    baseSpeedMetersPerSecond,
                    WantsSprint,
                    IsSprinting,
                    Time.deltaTime);
            FinalMovementSpeedMetersPerSecond = isFrozen
                ? 0f
                : TerrainAdjustedMovementSpeedMetersPerSecond * ConditionSpeedMultiplier;
            Vector3 planarMove = movementDirection * FinalMovementSpeedMetersPerSecond;
            LogSprintTransitionIfNeeded();
            verticalVelocity += gravityMetersPerSecondSquared * Time.deltaTime;

            Vector3 velocity = new Vector3(planarMove.x, verticalVelocity, planarMove.z);
            characterController.Move(velocity * Time.deltaTime);

            if (playerCondition != null)
            {
                playerCondition.Tick(Time.deltaTime, IsMoving, WantsSprint, IsSprinting);
            }
        }

        private static Vector2 ReadWasdInput()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyCode.A))
            {
                x -= 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                x += 1f;
            }

            if (Input.GetKey(KeyCode.S))
            {
                y -= 1f;
            }

            if (Input.GetKey(KeyCode.W))
            {
                y += 1f;
            }

            return new Vector2(x, y);
        }

        private bool IsSprintInputHeld()
        {
            if (Input.GetKey(sprintKey))
            {
                return true;
            }

            return IsJumpButtonHeld();
        }

        private static bool IsJumpButtonHeld()
        {
            try
            {
                return Input.GetButton("Jump");
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private void LogSprintTransitionIfNeeded()
        {
            if (!logSprintTransitions || !Application.isPlaying)
            {
                return;
            }

            if (WantsSprint == lastLoggedWantsSprint && IsSprinting == lastLoggedIsSprinting)
            {
                return;
            }

            lastLoggedWantsSprint = WantsSprint;
            lastLoggedIsSprinting = IsSprinting;
            bool canSprint = playerCondition == null || playerCondition.CanSprint;
            float terrainMultiplier = terrainMovementState == null ? 1f : terrainMovementState.SpeedMultiplier;
            bool isFrozen = playerCondition != null && playerCondition.IsFrozen;
            Debug.Log($"Lost Forest Sprint Input: WantsSprint={WantsSprint}, Sprinting={IsSprinting}, CanSprint={canSprint}, Key={sprintKey}, JumpButton={IsJumpButtonHeld()}, Speed={FinalMovementSpeedMetersPerSecond:0.0}, TerrainSpeed={TerrainAdjustedMovementSpeedMetersPerSecond:0.0}, TerrainMultiplier={terrainMultiplier:0.00}, ConditionMultiplier={ConditionSpeedMultiplier:0.00}, Frozen={isFrozen}", this);
        }

        private static Vector3 FlattenPlanarDirection(Vector3 direction)
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
