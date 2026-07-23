using UnityEngine;

namespace LostForest.Phase2.Feedback
{
    [ExecuteAlways]
    public sealed class PrototypeFogDirector : MonoBehaviour
    {
        [Header("Prototype Distance Fog")]
        [SerializeField] private bool applyOnEnable = true;
        [SerializeField] private bool fogEnabled = true;
        [SerializeField] private FogMode fogMode = FogMode.Linear;
        [SerializeField] private Color fogColor = new Color(0.88f, 0.94f, 0.97f, 1f);
        [SerializeField] private float fogStartDistanceMeters = 5f;
        [SerializeField] private float fogEndDistanceMeters = 70f;
        [SerializeField] private float exponentialDensity = 0.02f;

        [Header("Camera Backdrop")]
        [SerializeField] private bool tintMainCameraBackground = true;
        [SerializeField] private bool forceSolidFogBackground = true;

        public void ApplyEarlyFogDefaults()
        {
            fogEnabled = true;
            fogMode = FogMode.Linear;
            fogColor = new Color(0.88f, 0.94f, 0.97f, 1f);
            fogStartDistanceMeters = 5f;
            fogEndDistanceMeters = 70f;
            exponentialDensity = 0.02f;
            tintMainCameraBackground = true;
            forceSolidFogBackground = true;
        }

        private void OnEnable()
        {
            if (applyOnEnable)
            {
                ApplyFogSettings();
            }
        }

        private void Start()
        {
            ApplyFogSettings();
        }

        [ContextMenu("Apply Prototype Fog Settings")]
        public void ApplyFogSettings()
        {
            RenderSettings.fog = fogEnabled;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = Mathf.Max(0f, fogStartDistanceMeters);
            RenderSettings.fogEndDistance = Mathf.Max(RenderSettings.fogStartDistance + 1f, fogEndDistanceMeters);
            RenderSettings.fogDensity = Mathf.Max(0f, exponentialDensity);

            if (tintMainCameraBackground && Camera.main != null)
            {
                if (forceSolidFogBackground)
                {
                    Camera.main.clearFlags = CameraClearFlags.SolidColor;
                }

                Camera.main.backgroundColor = fogColor;
            }
        }
    }
}
