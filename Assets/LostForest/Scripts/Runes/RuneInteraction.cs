using UnityEngine;

namespace LostForest.Phase2.Runes
{
    public sealed class RuneInteraction : MonoBehaviour
    {
        [SerializeField] private RuneManager runeManager;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private KeyCode interactionKey = KeyCode.X;

        public void SetSources(RuneManager newRuneManager, Camera newTargetCamera)
        {
            runeManager = newRuneManager;
            targetCamera = newTargetCamera;
        }

        public void SetInteractionKey(KeyCode newInteractionKey)
        {
            interactionKey = newInteractionKey;
        }

        private void Awake()
        {
            DiscoverReferences();
        }

        private void Update()
        {
            if (!Input.GetKeyDown(interactionKey))
            {
                return;
            }

            DiscoverReferences();

            if (runeManager != null)
            {
                runeManager.TryInteract(transform, targetCamera);
            }
        }

        private void DiscoverReferences()
        {
            if (runeManager == null)
            {
                runeManager = FindAnyObjectByType<RuneManager>();
            }

            if (targetCamera == null)
            {
                targetCamera = GetComponentInChildren<Camera>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }
    }
}
