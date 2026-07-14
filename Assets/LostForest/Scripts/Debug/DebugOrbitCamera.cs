using UnityEngine;

namespace LostForest.Phase2.DebugTools
{
    [ExecuteAlways]
    public sealed class DebugOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Vector3 target = Vector3.zero;
        [SerializeField] private float radius = 230f;
        [SerializeField] private float minRadius = 80f;
        [SerializeField] private float maxRadius = 520f;
        [SerializeField] private float height = 135f;
        [SerializeField] private float minHeight = 35f;
        [SerializeField] private float maxHeight = 320f;
        [SerializeField] private float degreesPerSecond = 12f;
        [SerializeField] private float startAngleDegrees = -35f;
        [SerializeField] private float keyboardZoomMetersPerSecond = 120f;
        [SerializeField] private float scrollZoomMeters = 35f;
        [SerializeField] private float heightAdjustMetersPerSecond = 90f;
        [SerializeField] private bool rotateInEditMode;
        [SerializeField] private bool rotateInPlayMode = true;
        [SerializeField] private bool lookAtTarget = true;
        [SerializeField] private bool usePerspective = true;
        [SerializeField] private float fieldOfView = 45f;

        private float currentAngleDegrees;

        private void OnEnable()
        {
            currentAngleDegrees = startAngleDegrees;
            ApplyCameraSettings();
            UpdateCameraPosition();
        }

        private void Update()
        {
            HandleInput();

            bool shouldRotate = Application.isPlaying ? rotateInPlayMode : rotateInEditMode;

            if (shouldRotate)
            {
                currentAngleDegrees += degreesPerSecond * Time.deltaTime;
            }

            ApplyCameraSettings();
            UpdateCameraPosition();
        }

        private void HandleInput()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            float zoomInput = 0f;

            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
            {
                zoomInput -= 1f;
            }

            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
            {
                zoomInput += 1f;
            }

            float scrollInput = Input.mouseScrollDelta.y;
            radius += zoomInput * keyboardZoomMetersPerSecond * Time.deltaTime;
            radius -= scrollInput * scrollZoomMeters;
            radius = Mathf.Clamp(radius, Mathf.Max(1f, minRadius), Mathf.Max(minRadius, maxRadius));

            float heightInput = 0f;

            if (Input.GetKey(KeyCode.E))
            {
                heightInput += 1f;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                heightInput -= 1f;
            }

            height += heightInput * heightAdjustMetersPerSecond * Time.deltaTime;
            height = Mathf.Clamp(height, minHeight, Mathf.Max(minHeight, maxHeight));
        }

        [ContextMenu("Reset Orbit Angle")]
        public void ResetOrbitAngle()
        {
            currentAngleDegrees = startAngleDegrees;
            UpdateCameraPosition();
        }

        private void ApplyCameraSettings()
        {
            Camera camera = GetComponent<Camera>();

            if (camera == null)
            {
                return;
            }

            camera.orthographic = !usePerspective;
            camera.fieldOfView = Mathf.Clamp(fieldOfView, 20f, 90f);
        }

        private void UpdateCameraPosition()
        {
            float radians = currentAngleDegrees * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(radians) * radius, height, Mathf.Sin(radians) * radius);
            transform.position = target + offset;

            if (lookAtTarget)
            {
                transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
            }
        }
    }
}
