using UnityEngine;

namespace LostForest.Phase2.Runes
{
    [RequireComponent(typeof(RuneId))]
    public sealed class RuneTreeMarker : MonoBehaviour
    {
        private const string LetterObjectName = "Rune Marker Letter";
        private const float DiscRadiusMeters = 0.48f;
        private const float DiscThicknessMeters = 0.06f;
        private const float LetterOffsetFromDiscMeters = 0.05f;
        private const float LetterCharacterSizeMeters = 0.09f;

        [SerializeField] private RuneManager runeManager;
        [SerializeField] private RuneId runeId;
        [SerializeField] private Renderer discRenderer;
        [SerializeField] private TextMesh letterText;
        [SerializeField] private string fieldSlotAddress;
        [SerializeField] private string markerKey;
        [SerializeField] private bool collected;

        public char Letter => runeId == null ? RuneId.NoRune : runeId.Letter;
        public string FieldSlotAddress => fieldSlotAddress;
        public string MarkerKey => markerKey;
        public bool IsAvailable => !collected && gameObject.activeInHierarchy;
        public Vector3 InteractionPosition => transform.position;

        public static RuneTreeMarker CreatePrototypeMarker(
            RuneTreeAnchor anchor,
            RuneManager manager,
            char runeLetter,
            string slotAddress,
            string stableMarkerKey,
            Material discMaterial,
            Color letterColor,
            Camera targetCamera)
        {
            if (anchor.TreeRoot == null || manager == null || !RuneId.IsValidRune(runeLetter))
            {
                return null;
            }

            GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerObject.name = $"Rune Tree Marker {runeLetter} {slotAddress} Tree {anchor.TreeIndex:00}";
            markerObject.transform.SetParent(anchor.TreeRoot, false);

            float markerHeight = Mathf.Clamp(anchor.TrunkHeightMeters * 0.32f, 2.2f, 3.7f);
            markerObject.transform.localPosition = (Vector3.up * markerHeight) + (Vector3.forward * ((anchor.TrunkRadiusMeters * 0.6f) + 0.12f));
            markerObject.transform.localRotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
            markerObject.transform.localScale = new Vector3(DiscRadiusMeters, DiscThicknessMeters * 0.5f, DiscRadiusMeters);

            Collider collider = markerObject.GetComponent<Collider>();

            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Renderer renderer = markerObject.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = discMaterial;
            }

            RuneTreeMarker marker = markerObject.AddComponent<RuneTreeMarker>();
            marker.Configure(
                manager,
                runeLetter,
                slotAddress,
                stableMarkerKey,
                renderer,
                letterColor,
                targetCamera,
                anchor.TreeRoot,
                markerObject.transform.position,
                markerObject.transform.up);
            return marker;
        }

        public void Configure(
            RuneManager newRuneManager,
            char runeLetter,
            string newFieldSlotAddress,
            string newMarkerKey,
            Renderer newDiscRenderer,
            Color letterColor,
            Camera targetCamera,
            Transform letterParent,
            Vector3 letterWorldPosition,
            Vector3 discFaceNormal)
        {
            runeManager = newRuneManager;
            fieldSlotAddress = newFieldSlotAddress;
            markerKey = newMarkerKey;
            discRenderer = newDiscRenderer == null ? GetComponent<Renderer>() : newDiscRenderer;
            runeId = GetComponent<RuneId>();
            runeId.SetRune(runeLetter);
            EnsureLetterText(letterColor, targetCamera, letterParent, letterWorldPosition, discFaceNormal);

            if (runeManager != null && runeManager.IsMarkerClaimed(markerKey))
            {
                SetCollectedWithoutNotify();
                return;
            }

            collected = false;
            runeManager?.RegisterMarker(this);
        }

        private void LateUpdate()
        {
            ApplyPerceptionVisibility();
        }

        public void SetCollected()
        {
            SetCollectedWithoutNotify();
            runeManager?.UnregisterMarker(this);
        }

        private void OnEnable()
        {
            if (runeManager != null && !collected)
            {
                runeManager.RegisterMarker(this);
            }
        }

        private void OnDisable()
        {
            runeManager?.UnregisterMarker(this);
        }

        private void EnsureLetterText(
            Color letterColor,
            Camera targetCamera,
            Transform letterParent,
            Vector3 letterWorldPosition,
            Vector3 discFaceNormal)
        {
            Transform resolvedParent = letterParent == null ? transform : letterParent;
            Transform existingLetter = resolvedParent.Find($"{LetterObjectName} {markerKey}");

            if (existingLetter == null)
            {
                existingLetter = new GameObject($"{LetterObjectName} {markerKey}").transform;
                existingLetter.SetParent(resolvedParent, true);
            }

            Vector3 normal = discFaceNormal.sqrMagnitude <= 0.0001f ? transform.up : discFaceNormal.normalized;
            existingLetter.position = letterWorldPosition + normal * LetterOffsetFromDiscMeters;
            existingLetter.localScale = Vector3.one;

            letterText = existingLetter.GetComponent<TextMesh>();

            if (letterText == null)
            {
                letterText = existingLetter.gameObject.AddComponent<TextMesh>();
            }

            letterText.text = runeId.LetterText;
            letterText.anchor = TextAnchor.MiddleCenter;
            letterText.alignment = TextAlignment.Center;
            letterText.fontSize = 72;
            letterText.characterSize = LetterCharacterSizeMeters;
            letterText.color = letterColor;

            RuneBillboardText billboardText = existingLetter.GetComponent<RuneBillboardText>();

            if (billboardText == null)
            {
                billboardText = existingLetter.gameObject.AddComponent<RuneBillboardText>();
            }

            billboardText.SetTargetCamera(targetCamera);
            ApplyPerceptionVisibility();
        }

        private void ApplyPerceptionVisibility()
        {
            bool markerVisible = !collected && (runeManager == null || runeManager.IsMarkerVisible(InteractionPosition));
            bool letterVisible = markerVisible && (runeManager == null || runeManager.IsMarkerLetterReadable(InteractionPosition));

            if (discRenderer != null)
            {
                discRenderer.enabled = markerVisible;
            }

            if (letterText != null)
            {
                letterText.gameObject.SetActive(letterVisible);
            }
        }

        private void SetCollectedWithoutNotify()
        {
            collected = true;

            if (letterText != null)
            {
                letterText.gameObject.SetActive(false);
            }

            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
