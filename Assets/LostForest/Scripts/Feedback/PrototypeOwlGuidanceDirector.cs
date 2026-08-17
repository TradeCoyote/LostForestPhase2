using LostForest.Phase2.Core;
using LostForest.Phase2.Player;
using LostForest.Phase2.Runes;
using UnityEngine;
using UnityEngine.Rendering;

namespace LostForest.Phase2.Feedback
{
    /// <summary>
    /// A feather pickup summons a temporary, eye-height guide. It enters from
    /// the adjacent Slot in the player's forward direction, approaches them,
    /// then heads directly toward the Home altar.
    /// </summary>
    public sealed class PrototypeOwlGuidanceDirector : MonoBehaviour
    {
        private const string PresentationRootName = "Owl Guidance Presentation";

        private enum OwlFlightState
        {
            None,
            ApproachingPlayer,
            LeadingHome,
            FadingOut
        }

        [Header("Sources")]
        [SerializeField] private RuneManager runeManager;
        [SerializeField] private Transform player;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerCondition playerCondition;
        [SerializeField] private RunVictoryController runVictoryController;
        [SerializeField] private AudioClip owlClip;

        [Header("Owl Flight")]
        [SerializeField, Min(10f)] private float forwardTileDistanceMeters = 72f;
        [SerializeField] private float eyeHeightOffsetMeters = 0.2f;
        [SerializeField, Min(1f)] private float approachSpeedMetersPerSecond = 24f;
        [SerializeField, Min(1f)] private float homewardSpeedMetersPerSecond = 21f;
        [SerializeField, Min(0.5f)] private float turnNearPlayerDistanceMeters = 3f;
        [SerializeField, Min(1f)] private float homeArrivalDistanceMeters = 8f;
        [SerializeField, Min(0.1f)] private float owlTriangleSize = 1.85f;
        [SerializeField] private Color owlColor = new Color(0.72f, 0.56f, 0.34f, 1f);
        [Tooltip("How long the owl remains visible after it turns away from the player to lead toward Home.")]
        [SerializeField] private Vector2 leadHomeVisibleDurationSeconds = new Vector2(5f, 7f);
        [SerializeField, Min(0.1f)] private float fadeOutSeconds = 1.15f;

        [Header("Owl Audio")]
        [SerializeField, Range(0f, 1f)] private float owlVolume = 0.72f;
        [SerializeField, Min(1f)] private float owlAudioMaxDistanceMeters = 105f;

        [Header("Development Debug")]
        [Tooltip("Development-only preview shortcut. Press O while the Game view has focus.")]
        [SerializeField] private KeyCode forceOwlGuidanceKey = KeyCode.O;
        [SerializeField] private bool enableDevelopmentHotkey = true;
        [Tooltip("Logs feather-triggered and preview playback requests so playtests can distinguish a missed cue from a silent audio device.")]
        [SerializeField] private bool logOwlAudioEvents = true;

        private Transform presentationRoot;
        private Transform owlTransform;
        private MeshRenderer owlRenderer;
        private AudioSource owlAudioSource;
        private Mesh owlMesh;
        private Material owlMaterial;
        private MaterialPropertyBlock propertyBlock;
        private OwlFlightState state;
        private float fadeElapsedSeconds;
        private float leadHomeElapsedSeconds;
        private float leadHomeVisibleDuration;
        private bool subscribed;
        private int owlAudioPlaybackCount;

        public bool HasOwlClip => owlClip != null;
        public bool IsGuiding => state != OwlFlightState.None;
        public string CurrentState => state.ToString();
        public Vector2 LeadHomeVisibleDurationRangeSeconds => new Vector2(
            Mathf.Min(leadHomeVisibleDurationSeconds.x, leadHomeVisibleDurationSeconds.y),
            Mathf.Max(leadHomeVisibleDurationSeconds.x, leadHomeVisibleDurationSeconds.y));

        public void SetSources(
            RuneManager newRuneManager,
            Transform newPlayer,
            Camera newPlayerCamera,
            PlayerCondition newPlayerCondition,
            RunVictoryController newRunVictoryController)
        {
            Unsubscribe();
            runeManager = newRuneManager;
            player = newPlayer;
            playerCamera = newPlayerCamera;
            playerCondition = newPlayerCondition;
            runVictoryController = newRunVictoryController;
            Subscribe();
        }

        public void SetOwlClip(AudioClip newOwlClip)
        {
            owlClip = newOwlClip;
        }

        public void ApplyPrototypeDefaults()
        {
            forwardTileDistanceMeters = 72f;
            eyeHeightOffsetMeters = 0.2f;
            approachSpeedMetersPerSecond = 24f;
            homewardSpeedMetersPerSecond = 21f;
            turnNearPlayerDistanceMeters = 3f;
            homeArrivalDistanceMeters = 8f;
            owlTriangleSize = 1.85f;
            owlColor = new Color(0.72f, 0.56f, 0.34f, 1f);
            leadHomeVisibleDurationSeconds = new Vector2(5f, 7f);
            fadeOutSeconds = 1.15f;
            owlVolume = 0.72f;
            owlAudioMaxDistanceMeters = 105f;
            forceOwlGuidanceKey = KeyCode.O;
            enableDevelopmentHotkey = true;
            logOwlAudioEvents = true;
        }

        public bool ValidateConfiguration(out string failureReason)
        {
            if (owlClip == null)
            {
                failureReason = "Owl guidance is missing Owl1.mp3.";
                return false;
            }

            if (forwardTileDistanceMeters < 10f || approachSpeedMetersPerSecond <= 0f || homewardSpeedMetersPerSecond <= 0f || turnNearPlayerDistanceMeters <= 0f || homeArrivalDistanceMeters <= 0f || owlTriangleSize <= 0f || fadeOutSeconds <= 0f || LeadHomeVisibleDurationRangeSeconds.x < 5f || LeadHomeVisibleDurationRangeSeconds.y > 7f)
            {
                failureReason = "Owl guidance flight settings are invalid.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public void ForceOwlGuidanceForDebug()
        {
            BeginGuidance();
        }

        public string BuildDebugSummary()
        {
            string leadTime = state == OwlFlightState.LeadingHome
                ? $" Lead={leadHomeElapsedSeconds:0.0}/{leadHomeVisibleDuration:0.0}s"
                : string.Empty;
            return $"Owl {state} Clip={(owlClip == null ? "Missing" : owlClip.name)} AudioPlays={owlAudioPlaybackCount}{leadTime}";
        }

        private void OnEnable()
        {
            DiscoverSources();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ClearOwl();
        }

        private void OnDestroy()
        {
            DestroyUnityObject(owlMaterial);
            DestroyUnityObject(owlMesh);
        }

        private void OnValidate()
        {
            forwardTileDistanceMeters = Mathf.Max(10f, forwardTileDistanceMeters);
            approachSpeedMetersPerSecond = Mathf.Max(1f, approachSpeedMetersPerSecond);
            homewardSpeedMetersPerSecond = Mathf.Max(1f, homewardSpeedMetersPerSecond);
            turnNearPlayerDistanceMeters = Mathf.Max(0.5f, turnNearPlayerDistanceMeters);
            homeArrivalDistanceMeters = Mathf.Max(1f, homeArrivalDistanceMeters);
            owlTriangleSize = Mathf.Max(0.1f, owlTriangleSize);
            float leadHomeMinimumSeconds = Mathf.Clamp(Mathf.Min(leadHomeVisibleDurationSeconds.x, leadHomeVisibleDurationSeconds.y), 5f, 7f);
            float leadHomeMaximumSeconds = Mathf.Clamp(Mathf.Max(leadHomeVisibleDurationSeconds.x, leadHomeVisibleDurationSeconds.y), leadHomeMinimumSeconds, 7f);
            leadHomeVisibleDurationSeconds = new Vector2(leadHomeMinimumSeconds, leadHomeMaximumSeconds);
            fadeOutSeconds = Mathf.Max(0.1f, fadeOutSeconds);
            owlVolume = Mathf.Clamp01(owlVolume);
            owlAudioMaxDistanceMeters = Mathf.Max(1f, owlAudioMaxDistanceMeters);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            DiscoverSources();
            Subscribe();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (enableDevelopmentHotkey && Input.GetKeyDown(forceOwlGuidanceKey))
            {
                ForceOwlGuidanceForDebug();
            }
#endif

            if (IsRunTerminal())
            {
                ClearOwl();
                return;
            }

            TickGuidance(Time.deltaTime);
        }

        private void DiscoverSources()
        {
            if (runeManager == null)
            {
                runeManager = FindAnyObjectByType<RuneManager>();
            }

            if (player == null)
            {
                PlayerGridAddressTracker tracker = FindAnyObjectByType<PlayerGridAddressTracker>();
                player = tracker == null ? null : tracker.transform;
            }

            if (playerCamera == null)
            {
                playerCamera = runeManager == null ? Camera.main : runeManager.PlayerCamera;
            }

            if (playerCondition == null)
            {
                playerCondition = FindAnyObjectByType<PlayerCondition>();
            }

            if (runVictoryController == null)
            {
                runVictoryController = FindAnyObjectByType<RunVictoryController>();
            }
        }

        private void Subscribe()
        {
            if (subscribed || runeManager == null)
            {
                return;
            }

            runeManager.OwlFeatherPickedUp += HandleOwlFeatherPickedUp;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || runeManager == null)
            {
                subscribed = false;
                return;
            }

            runeManager.OwlFeatherPickedUp -= HandleOwlFeatherPickedUp;
            subscribed = false;
        }

        private void HandleOwlFeatherPickedUp(string slotAddress)
        {
            if (logOwlAudioEvents)
            {
                Debug.Log($"Lost Forest Owl guidance requested: FeatherSlot={slotAddress}", this);
            }

            BeginGuidance();
        }

        private void BeginGuidance()
        {
            if (owlClip == null || playerCamera == null || runeManager == null || !runeManager.TryGetHomeWorldCenter(out _))
            {
                if (logOwlAudioEvents)
                {
                    Debug.LogWarning($"Lost Forest Owl guidance did not start: Clip={(owlClip == null ? "Missing" : owlClip.name)}, Camera={(playerCamera == null ? "Missing" : "Ready")}, RuneManager={(runeManager == null ? "Missing" : "Ready")}, Home={(runeManager != null && runeManager.TryGetHomeWorldCenter(out _) ? "Ready" : "Missing")}.", this);
                }

                return;
            }

            ClearOwl();
            EnsurePresentationResources();

            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude < 0.001f ? Vector3.forward : forward.normalized;
            Vector3 eyePosition = playerCamera.transform.position + Vector3.up * eyeHeightOffsetMeters;
            Vector3 spawnPosition = eyePosition + forward * forwardTileDistanceMeters;

            owlTransform.position = spawnPosition;
            owlTransform.gameObject.SetActive(true);
            owlAudioSource.transform.position = spawnPosition;
            owlAudioSource.volume = owlVolume;
            owlAudioSource.Play();
            owlAudioPlaybackCount++;

            if (logOwlAudioEvents)
            {
                Debug.Log($"Lost Forest Owl audio started: Play={owlAudioPlaybackCount}, Clip={owlClip.name} ({owlClip.length:0.00}s), Volume={owlVolume:0.00}, Playing={owlAudioSource.isPlaying}", this);
            }

            state = OwlFlightState.ApproachingPlayer;
            fadeElapsedSeconds = 0f;
            leadHomeElapsedSeconds = 0f;
            leadHomeVisibleDuration = 0f;
            SetOwlAlpha(1f);
        }

        private void TickGuidance(float deltaSeconds)
        {
            if (state == OwlFlightState.None || owlTransform == null)
            {
                return;
            }

            Vector3 target;

            switch (state)
            {
                case OwlFlightState.ApproachingPlayer:
                    target = GetPlayerEyePosition();
                    MoveOwlToward(target, approachSpeedMetersPerSecond, deltaSeconds);

                    if (PlanarDistance(owlTransform.position, target) <= turnNearPlayerDistanceMeters)
                    {
                        BeginLeadingHome();
                    }
                    break;

                case OwlFlightState.LeadingHome:
                    if (runeManager == null || !runeManager.TryGetHomeWorldCenter(out target))
                    {
                        BeginFadeOut();
                        break;
                    }

                    target.y = owlTransform.position.y;
                    MoveOwlToward(target, homewardSpeedMetersPerSecond, deltaSeconds);
                    leadHomeElapsedSeconds += Mathf.Max(0f, deltaSeconds);

                    if (leadHomeElapsedSeconds >= leadHomeVisibleDuration || PlanarDistance(owlTransform.position, target) <= homeArrivalDistanceMeters)
                    {
                        BeginFadeOut();
                    }
                    break;

                case OwlFlightState.FadingOut:
                    fadeElapsedSeconds += Mathf.Max(0f, deltaSeconds);
                    SetOwlAlpha(1f - Mathf.Clamp01(fadeElapsedSeconds / fadeOutSeconds));

                    if (fadeElapsedSeconds >= fadeOutSeconds)
                    {
                        ClearOwl();
                    }
                    break;
            }

            if (owlAudioSource != null && owlTransform != null)
            {
                owlAudioSource.transform.position = owlTransform.position;
            }
        }

        private void MoveOwlToward(Vector3 target, float speedMetersPerSecond, float deltaSeconds)
        {
            owlTransform.position = Vector3.MoveTowards(
                owlTransform.position,
                target,
                Mathf.Max(0f, speedMetersPerSecond) * Mathf.Max(0f, deltaSeconds));

            Camera camera = playerCamera == null ? Camera.main : playerCamera;

            if (camera != null)
            {
                Vector3 cameraDirection = camera.transform.position - owlTransform.position;

                if (cameraDirection.sqrMagnitude > 0.001f)
                {
                    owlTransform.rotation = Quaternion.LookRotation(cameraDirection.normalized, Vector3.up);
                }
            }
        }

        private Vector3 GetPlayerEyePosition()
        {
            if (playerCamera != null)
            {
                return playerCamera.transform.position + Vector3.up * eyeHeightOffsetMeters;
            }

            return player == null ? transform.position + Vector3.up * 1.8f : player.position + Vector3.up * (1.8f + eyeHeightOffsetMeters);
        }

        private void BeginFadeOut()
        {
            state = OwlFlightState.FadingOut;
            fadeElapsedSeconds = 0f;
        }

        private void BeginLeadingHome()
        {
            state = OwlFlightState.LeadingHome;
            leadHomeElapsedSeconds = 0f;
            leadHomeVisibleDuration = UnityEngine.Random.Range(
                LeadHomeVisibleDurationRangeSeconds.x,
                LeadHomeVisibleDurationRangeSeconds.y);

            if (logOwlAudioEvents)
            {
                Debug.Log($"Lost Forest Owl is leading Home and will fade after {leadHomeVisibleDuration:0.0}s.", this);
            }
        }

        private void ClearOwl()
        {
            state = OwlFlightState.None;
            fadeElapsedSeconds = 0f;
            leadHomeElapsedSeconds = 0f;
            leadHomeVisibleDuration = 0f;

            if (owlAudioSource != null)
            {
                owlAudioSource.Stop();
            }

            if (owlTransform != null)
            {
                owlTransform.gameObject.SetActive(false);
            }
        }

        private void EnsurePresentationResources()
        {
            if (presentationRoot == null)
            {
                Transform existing = transform.Find(PresentationRootName);
                presentationRoot = existing == null ? new GameObject(PresentationRootName).transform : existing;
                presentationRoot.SetParent(transform, false);
            }

            if (owlTransform == null)
            {
                GameObject owlObject = new GameObject("Tan Triangle Owl");
                owlObject.transform.SetParent(presentationRoot, false);
                owlTransform = owlObject.transform;
                MeshFilter meshFilter = owlObject.AddComponent<MeshFilter>();
                owlRenderer = owlObject.AddComponent<MeshRenderer>();
                meshFilter.sharedMesh = GetOwlMesh();
                owlRenderer.sharedMaterial = GetOwlMaterial();
                owlRenderer.shadowCastingMode = ShadowCastingMode.Off;
                owlRenderer.receiveShadows = false;
                owlRenderer.lightProbeUsage = LightProbeUsage.Off;
                owlRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                owlTransform.localScale = Vector3.one * owlTriangleSize;
                owlObject.SetActive(false);
            }

            if (owlAudioSource == null)
            {
                GameObject audioObject = new GameObject("Owl Guidance Audio");
                audioObject.transform.SetParent(presentationRoot, false);
                owlAudioSource = audioObject.AddComponent<AudioSource>();
                owlAudioSource.playOnAwake = false;
                owlAudioSource.loop = false;
                owlAudioSource.spatialBlend = 1f;
                owlAudioSource.spread = 110f;
                owlAudioSource.dopplerLevel = 0f;
                owlAudioSource.rolloffMode = AudioRolloffMode.Linear;
                owlAudioSource.minDistance = 2f;
                owlAudioSource.maxDistance = owlAudioMaxDistanceMeters;
            }

            owlAudioSource.clip = owlClip;
            owlTransform.localScale = Vector3.one * owlTriangleSize;
        }

        private Mesh GetOwlMesh()
        {
            if (owlMesh != null)
            {
                return owlMesh;
            }

            owlMesh = new Mesh
            {
                name = "Prototype Tan Triangle Owl Mesh",
                hideFlags = HideFlags.DontSave
            };
            owlMesh.vertices = new[]
            {
                new Vector3(-0.72f, -0.42f, 0f),
                new Vector3(0.72f, -0.42f, 0f),
                new Vector3(0f, 0.74f, 0f)
            };
            owlMesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up };
            owlMesh.triangles = new[] { 0, 1, 2, 2, 1, 0 };
            owlMesh.RecalculateBounds();
            return owlMesh;
        }

        private Material GetOwlMaterial()
        {
            if (owlMaterial != null)
            {
                return owlMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            shader = shader == null ? Shader.Find("Unlit/Transparent") : shader;
            shader = shader == null ? Shader.Find("Unlit/Color") : shader;
            owlMaterial = new Material(shader)
            {
                name = "Prototype Tan Triangle Owl Material",
                hideFlags = HideFlags.DontSave,
                color = owlColor,
                renderQueue = 3001
            };
            return owlMaterial;
        }

        private void SetOwlAlpha(float alpha)
        {
            if (owlRenderer == null)
            {
                return;
            }

            propertyBlock = propertyBlock ?? new MaterialPropertyBlock();
            owlRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", new Color(owlColor.r, owlColor.g, owlColor.b, Mathf.Clamp01(alpha)));
            owlRenderer.SetPropertyBlock(propertyBlock);

            if (owlAudioSource != null)
            {
                owlAudioSource.volume = owlVolume * Mathf.Clamp01(alpha);
            }
        }

        private bool IsRunTerminal()
        {
            return (playerCondition != null && playerCondition.IsGameOver) ||
                (runVictoryController != null && runVictoryController.IsVictory);
        }

        private static float PlanarDistance(Vector3 left, Vector3 right)
        {
            Vector3 delta = left - right;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static void DestroyUnityObject(Object unityObject)
        {
            if (unityObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(unityObject);
            }
            else
            {
                DestroyImmediate(unityObject);
            }
        }
    }
}
