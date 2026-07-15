using UnityEngine;

namespace LostForest.Phase2.Player
{
    [RequireComponent(typeof(CharacterController))]
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
        [SerializeField] private float gravityMetersPerSecondSquared = -24f;
        [SerializeField] private float groundedDownwardVelocity = -2f;

        private CharacterController characterController;
        private float pitchDegrees;
        private float verticalVelocity;

        public bool IsGrounded => characterController != null && characterController.isGrounded;

        public void SetCameraRoot(Transform newCameraRoot)
        {
            cameraRoot = newCameraRoot;
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

            Vector3 planarMove = (transform.right * input.x + transform.forward * input.y) * walkSpeedMetersPerSecond;
            verticalVelocity += gravityMetersPerSecondSquared * Time.deltaTime;

            Vector3 velocity = new Vector3(planarMove.x, verticalVelocity, planarMove.z);
            characterController.Move(velocity * Time.deltaTime);
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
    }
}
