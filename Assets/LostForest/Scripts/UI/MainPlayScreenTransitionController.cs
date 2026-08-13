using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LostForest.Phase2.UI
{
    [DisallowMultipleComponent]
    public sealed class MainPlayScreenTransitionController : MonoBehaviour
    {
        private const float MinimumDurationSeconds = 0.1f;

        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "Phase2_GridMovementFogTest";

        [Header("Title Controls")]
        [SerializeField] private Button beginButton;
        [SerializeField] private BeginRuneGlowGraphic runeGlow;
        [SerializeField] private Image whiteVeil;
        [SerializeField] private CanvasGroup findRunesText;
        [SerializeField] private CanvasGroup stopWhatText;
        [SerializeField] private CanvasGroup huntsYouText;

        [Header("Timing")]
        [SerializeField] private float titleTransitionSeconds = 5f;
        [SerializeField] private float whiteFadeDelaySeconds = 0.45f;
        [SerializeField] private float whiteFullyVisibleSeconds = 3.8f;
        [SerializeField] private float glowFadeStartSeconds = 3.45f;
        [SerializeField] private float firstMessageFadeSeconds = 1.4f;
        [SerializeField] private float secondMessageBeatSeconds = 0.9f;
        [SerializeField] private float secondMessageFadeSeconds = 1.4f;
        [SerializeField] private float allMessageHoldSeconds = 2f;
        [SerializeField] private float contextFadeOutSeconds = 1.6f;
        [SerializeField] private float huntsYouHoldSeconds = 1.5f;
        [SerializeField] private float gameplayFadeInSeconds = 2f;

        private bool transitionStarted;

        public bool IsTransitioning => transitionStarted;
        public bool HasOpeningInstructionSequence => findRunesText != null && stopWhatText != null && huntsYouText != null;
        public float AllMessageHoldSeconds => allMessageHoldSeconds;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SetWhiteAlpha(0f);
            SetInstructionAlpha(findRunesText, 0f);
            SetInstructionAlpha(stopWhatText, 0f);
            SetInstructionAlpha(huntsYouText, 0f);

            if (runeGlow != null)
            {
                runeGlow.SetGlow(0f, 0f);
            }

            if (beginButton != null)
            {
                beginButton.onClick.AddListener(BeginGame);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            if (beginButton != null)
            {
                beginButton.onClick.RemoveListener(BeginGame);
            }
        }

        private void OnValidate()
        {
            titleTransitionSeconds = Mathf.Max(MinimumDurationSeconds, titleTransitionSeconds);
            whiteFadeDelaySeconds = Mathf.Clamp(whiteFadeDelaySeconds, 0f, titleTransitionSeconds);
            whiteFullyVisibleSeconds = Mathf.Clamp(whiteFullyVisibleSeconds, whiteFadeDelaySeconds, titleTransitionSeconds);
            glowFadeStartSeconds = Mathf.Clamp(glowFadeStartSeconds, 0f, titleTransitionSeconds);
            firstMessageFadeSeconds = Mathf.Max(MinimumDurationSeconds, firstMessageFadeSeconds);
            secondMessageBeatSeconds = Mathf.Max(0f, secondMessageBeatSeconds);
            secondMessageFadeSeconds = Mathf.Max(MinimumDurationSeconds, secondMessageFadeSeconds);
            allMessageHoldSeconds = Mathf.Max(0f, allMessageHoldSeconds);
            contextFadeOutSeconds = Mathf.Max(MinimumDurationSeconds, contextFadeOutSeconds);
            huntsYouHoldSeconds = Mathf.Max(0f, huntsYouHoldSeconds);
            gameplayFadeInSeconds = Mathf.Max(MinimumDurationSeconds, gameplayFadeInSeconds);
        }

        public void BeginGame()
        {
            if (transitionStarted)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(gameplaySceneName) || !Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                Debug.LogError($"Lost Forest title screen cannot load gameplay scene '{gameplaySceneName}'. Add it to Build Settings.", this);
                return;
            }

            transitionStarted = true;

            if (beginButton != null)
            {
                beginButton.interactable = false;
            }

            StartCoroutine(PlayTitleTransitionAndLoad());
        }

        private IEnumerator PlayTitleTransitionAndLoad()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
            if (loadOperation == null)
            {
                RecoverFromLoadFailure();
                yield break;
            }

            loadOperation.allowSceneActivation = false;
            float elapsedSeconds = 0f;

            while (elapsedSeconds < titleTransitionSeconds || loadOperation.progress < 0.9f)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                UpdateTitleTransition(elapsedSeconds);
                yield return null;
            }

            SetWhiteAlpha(1f);
            if (runeGlow != null)
            {
                runeGlow.SetGlow(0f, elapsedSeconds * 5f);
            }

            yield return PlayOpeningInstructionSequence();

            loadOperation.allowSceneActivation = true;
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return FadeIntoGameplay();
            Destroy(gameObject);
        }

        private IEnumerator PlayOpeningInstructionSequence()
        {
            yield return FadeInstruction(0f, 1f, firstMessageFadeSeconds, findRunesText);

            if (secondMessageBeatSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(secondMessageBeatSeconds);
            }

            yield return FadeInstruction(0f, 1f, secondMessageFadeSeconds, stopWhatText, huntsYouText);

            if (allMessageHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(allMessageHoldSeconds);
            }

            yield return FadeInstruction(1f, 0f, contextFadeOutSeconds, findRunesText, stopWhatText);

            if (huntsYouHoldSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(huntsYouHoldSeconds);
            }
        }

        private void UpdateTitleTransition(float elapsedSeconds)
        {
            float whiteProgress = Mathf.InverseLerp(whiteFadeDelaySeconds, whiteFullyVisibleSeconds, elapsedSeconds);
            SetWhiteAlpha(Smooth01(whiteProgress));

            if (runeGlow == null)
            {
                return;
            }

            float wakeProgress = Smooth01(Mathf.InverseLerp(0f, 0.55f, elapsedSeconds));
            float fadeProgress = Smooth01(Mathf.InverseLerp(glowFadeStartSeconds, titleTransitionSeconds, elapsedSeconds));
            float pulse = 0.82f + Mathf.Sin(elapsedSeconds * 4.6f) * 0.12f + Mathf.Sin(elapsedSeconds * 8.1f) * 0.06f;
            float intensity = wakeProgress * (1f - fadeProgress) * pulse;
            runeGlow.SetGlow(intensity, elapsedSeconds * 4.2f);
        }

        private IEnumerator FadeIntoGameplay()
        {
            float elapsedSeconds = 0f;
            while (elapsedSeconds < gameplayFadeInSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                float progress = Smooth01(elapsedSeconds / gameplayFadeInSeconds);
                SetWhiteAlpha(1f - progress);
                SetInstructionAlpha(huntsYouText, 1f - progress);
                yield return null;
            }

            SetWhiteAlpha(0f);
            SetInstructionAlpha(huntsYouText, 0f);
        }

        private static IEnumerator FadeInstruction(float from, float to, float durationSeconds, params CanvasGroup[] groups)
        {
            float elapsedSeconds = 0f;
            float duration = Mathf.Max(MinimumDurationSeconds, durationSeconds);

            while (elapsedSeconds < duration)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(from, to, Smooth01(elapsedSeconds / duration));

                for (int i = 0; i < groups.Length; i++)
                {
                    SetInstructionAlpha(groups[i], alpha);
                }

                yield return null;
            }

            for (int i = 0; i < groups.Length; i++)
            {
                SetInstructionAlpha(groups[i], to);
            }
        }

        private void RecoverFromLoadFailure()
        {
            transitionStarted = false;
            SetWhiteAlpha(0f);
            SetInstructionAlpha(findRunesText, 0f);
            SetInstructionAlpha(stopWhatText, 0f);
            SetInstructionAlpha(huntsYouText, 0f);

            if (runeGlow != null)
            {
                runeGlow.SetGlow(0f, 0f);
            }

            if (beginButton != null)
            {
                beginButton.interactable = true;
            }

            Debug.LogError($"Lost Forest title screen failed to start loading '{gameplaySceneName}'.", this);
        }

        private void SetWhiteAlpha(float alpha)
        {
            if (whiteVeil == null)
            {
                return;
            }

            Color color = whiteVeil.color;
            color.a = Mathf.Clamp01(alpha);
            whiteVeil.color = color;
        }

        private static void SetInstructionAlpha(CanvasGroup group, float alpha)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = Mathf.Clamp01(alpha);
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
