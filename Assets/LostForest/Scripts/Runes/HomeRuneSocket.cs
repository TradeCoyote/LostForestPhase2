using UnityEngine;

namespace LostForest.Phase2.Runes
{
    [RequireComponent(typeof(RuneId))]
    public sealed class HomeRuneSocket : MonoBehaviour
    {
        private const string CylinderObjectName = "Home Rune Socket Cylinder";
        private const string LetterObjectName = "Home Rune Needed Letter";
        private const float NeededLetterCharacterSizeMeters = 0.07f;
        private const float NeededLetterVerticalOffsetMeters = 0.72f;
        private const float NeededLetterOutwardOffsetMeters = 0.12f;

        [SerializeField] private RuneManager runeManager;
        [SerializeField] private RuneId runeId;
        [SerializeField] private Renderer socketRenderer;
        [SerializeField] private TextMesh neededLetterText;
        [SerializeField] private int requiredRuneIndex = -1;
        [SerializeField] private bool deposited;

        private Material emptySocketMaterial;
        private Material depositedSocketMaterial;
        private Color neededLetterColor = Color.red;

        public char Letter => runeId == null ? RuneId.NoRune : runeId.Letter;
        public int RequiredRuneIndex => requiredRuneIndex;
        public bool IsDeposited => deposited;
        public Vector3 InteractionPosition => transform.position;

        public static HomeRuneSocket CreatePrototypeSocket(
            Transform parent,
            string name,
            RuneManager manager,
            char runeLetter,
            int runeIndex,
            Vector3 worldPosition,
            Vector3 outwardDirection,
            Material emptyMaterial,
            Material depositedMaterial,
            Color letterColor,
            Camera targetCamera)
        {
            if (parent == null || manager == null || !RuneId.IsValidRune(runeLetter))
            {
                return null;
            }

            Vector3 socketDirection = outwardDirection.sqrMagnitude <= 0.0001f ? Vector3.forward : outwardDirection.normalized;
            GameObject socketRoot = new GameObject(name);
            socketRoot.transform.SetParent(parent, true);
            socketRoot.transform.position = worldPosition;
            socketRoot.transform.rotation = Quaternion.FromToRotation(Vector3.up, socketDirection);
            socketRoot.transform.localScale = Vector3.one;

            GameObject socketCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            socketCylinder.name = CylinderObjectName;
            socketCylinder.transform.SetParent(socketRoot.transform, false);
            socketCylinder.transform.localPosition = Vector3.zero;
            socketCylinder.transform.localRotation = Quaternion.identity;
            socketCylinder.transform.localScale = new Vector3(0.7f, 0.58f, 0.7f);

            Collider collider = socketCylinder.GetComponent<Collider>();

            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Renderer renderer = socketCylinder.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = emptyMaterial;
            }

            GameObject letterObject = new GameObject(LetterObjectName);
            letterObject.transform.SetParent(socketRoot.transform, true);
            letterObject.transform.position = worldPosition + Vector3.up * NeededLetterVerticalOffsetMeters + socketDirection * NeededLetterOutwardOffsetMeters;
            letterObject.transform.localScale = Vector3.one;

            TextMesh letterText = letterObject.AddComponent<TextMesh>();
            letterText.anchor = TextAnchor.MiddleCenter;
            letterText.alignment = TextAlignment.Center;
            letterText.fontSize = 96;
            letterText.characterSize = NeededLetterCharacterSizeMeters;
            letterText.color = letterColor;

            RuneBillboardText billboardText = letterObject.AddComponent<RuneBillboardText>();
            billboardText.SetTargetCamera(targetCamera);

            HomeRuneSocket socket = socketRoot.AddComponent<HomeRuneSocket>();
            socket.Configure(manager, runeLetter, runeIndex, renderer, letterText, emptyMaterial, depositedMaterial, letterColor);
            return socket;
        }

        public void Configure(
            RuneManager newRuneManager,
            char runeLetter,
            int newRequiredRuneIndex,
            Renderer newSocketRenderer,
            TextMesh newNeededLetterText,
            Material newEmptySocketMaterial,
            Material newDepositedSocketMaterial,
            Color newNeededLetterColor)
        {
            runeManager = newRuneManager;
            requiredRuneIndex = newRequiredRuneIndex;
            socketRenderer = newSocketRenderer;
            neededLetterText = newNeededLetterText;
            emptySocketMaterial = newEmptySocketMaterial;
            depositedSocketMaterial = newDepositedSocketMaterial;
            neededLetterColor = newNeededLetterColor;

            runeId = GetComponent<RuneId>();
            runeId.SetRune(runeLetter);
            SetDeposited(runeManager != null && runeManager.IsRuneDeposited(runeId.Letter));
            runeManager?.RegisterSocket(this);
        }

        private void LateUpdate()
        {
            ApplyPerceptionVisibility();
        }

        public void SetDeposited(bool newDeposited)
        {
            deposited = newDeposited;

            if (socketRenderer != null)
            {
                socketRenderer.sharedMaterial = deposited ? depositedSocketMaterial : emptySocketMaterial;
            }

            if (neededLetterText != null)
            {
                neededLetterText.text = runeId == null ? string.Empty : runeId.LetterText;
                neededLetterText.color = neededLetterColor;
            }

            ApplyPerceptionVisibility();
        }

        private void OnEnable()
        {
            if (runeManager != null)
            {
                runeManager.RegisterSocket(this);
            }
        }

        private void OnDisable()
        {
            runeManager?.UnregisterSocket(this);
        }

        private void ApplyPerceptionVisibility()
        {
            bool socketVisible = runeManager == null || runeManager.IsHomeSocketVisible(InteractionPosition);
            bool letterVisible = !deposited && socketVisible && (runeManager == null || runeManager.IsHomeSocketLetterReadable(InteractionPosition));

            if (socketRenderer != null)
            {
                socketRenderer.enabled = socketVisible;
            }

            if (neededLetterText != null)
            {
                neededLetterText.gameObject.SetActive(letterVisible);
            }
        }
    }
}
