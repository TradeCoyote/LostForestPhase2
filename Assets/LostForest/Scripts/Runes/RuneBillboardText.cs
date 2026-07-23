using UnityEngine;

namespace LostForest.Phase2.Runes
{
    public sealed class RuneBillboardText : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        public void SetTargetCamera(Camera newTargetCamera)
        {
            targetCamera = newTargetCamera;
            FaceTargetCamera();
        }

        private void LateUpdate()
        {
            FaceTargetCamera();
        }

        private void FaceTargetCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            Vector3 toCamera = transform.position - targetCamera.transform.position;

            if (toCamera.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }
    }
}
