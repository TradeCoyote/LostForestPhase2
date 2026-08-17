using System.Collections.Generic;
using System.IO;
using LostForest.Phase2.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LostForest.Phase2.Editor
{
    public static class MainPlayScreenLoopBootstrap
    {
        private const string ScenePath = "Assets/LostForest/Scenes/MainPlayScreenLoop.unity";
        private const string GameplayScenePath = "Assets/LostForest/Scenes/Phase2_GridMovementFogTest.unity";
        private const string BackgroundPath = "Assets/LostForest/Art/KeyArt/MainPlayScreen_OthalaBirchForest_v3_BackgroundNoTalisman.png";
        private const string TalismanPath = "Assets/LostForest/Art/KeyArt/MainPlayScreen_OthalaTalisman_FreshBlood.png";

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Vector2 TalismanAttachPosition = new Vector2(-360f, 370f);
        private static readonly Vector2 TalismanSize = new Vector2(222f, 560f);
        private static readonly Vector2 BeginButtonPosition = new Vector2(405f, -315f);
        private static readonly Vector2 BeginButtonSize = new Vector2(660f, 225f);
        private static readonly Vector2 BeginGlowPosition = new Vector2(408f, -300f);
        private static readonly Vector2 BeginGlowSize = new Vector2(440f, 150f);
        private static readonly Vector2 FirstMessagePosition = new Vector2(0f, 250f);
        private static readonly Vector2 FirstMessageSize = new Vector2(1400f, 150f);
        private static readonly Vector2 SecondMessagePosition = new Vector2(0f, 85f);
        private static readonly Vector2 SecondMessageSize = new Vector2(1250f, 145f);

        [MenuItem("Lost Forest/Bootstrap/Create or Repair Main Play Screen Loop")]
        public static void CreateOrRepairMainPlayScreenLoop()
        {
            Sprite backgroundSprite = LoadSprite(BackgroundPath, false);
            Sprite talismanSprite = LoadSprite(TalismanPath, true);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateTitleCamera();

            GameObject canvasObject = new GameObject(
                "Main Play Screen Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(MainPlayScreenLoopAnimator));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            StretchToParent(canvasRect);

            RectTransform background = CreateImage("Clean Background", canvasRect, backgroundSprite, false);
            StretchToParent(background);

            RectTransform talismanPivot = CreateRect("Talisman Pivot", canvasRect);
            talismanPivot.anchorMin = new Vector2(0.5f, 0.5f);
            talismanPivot.anchorMax = new Vector2(0.5f, 0.5f);
            talismanPivot.pivot = new Vector2(0.5f, 0.5f);
            talismanPivot.anchoredPosition = TalismanAttachPosition;
            talismanPivot.sizeDelta = Vector2.zero;

            RectTransform talismanVisual = CreateImage("Fresh Blood Othala Talisman", talismanPivot, talismanSprite, false);
            talismanVisual.anchorMin = new Vector2(0.5f, 0.5f);
            talismanVisual.anchorMax = new Vector2(0.5f, 0.5f);
            talismanVisual.pivot = new Vector2(0.5f, 0.98f);
            talismanVisual.anchoredPosition = Vector2.zero;
            talismanVisual.sizeDelta = TalismanSize;

            RectTransform snowLayer = CreateRect("Looping Snow Layer", canvasRect);
            StretchToParent(snowLayer);
            snowLayer.SetAsLastSibling();

            Button beginButton = CreateBeginButtonHitArea(canvasRect);
            EnsureEventSystem();

            MainPlayScreenTransitionController transitionController = CreateTransitionCanvas(beginButton);

            MainPlayScreenLoopAnimator animator = canvasObject.GetComponent<MainPlayScreenLoopAnimator>();
            SerializedObject serializedAnimator = new SerializedObject(animator);
            SetProperty(serializedAnimator, "loopDurationSeconds", 15f);
            SetProperty(serializedAnimator, "snowClusterCount", 20);
            SetProperty(serializedAnimator, "snowLayer", snowLayer);
            SetProperty(serializedAnimator, "talismanPivot", talismanPivot);
            SetProperty(serializedAnimator, "talismanVisual", talismanVisual);
            SetProperty(serializedAnimator, "freshBloodAccent", talismanVisual.GetComponent<Image>());
            serializedAnimator.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureOpeningSceneIsFirstInBuild();
            AssetDatabase.SaveAssets();

            Debug.Log($"Lost Forest main play screen saved at {ScenePath}. It is now the first scene in the build.", transitionController);
        }

        [MenuItem("Lost Forest/Bootstrap/Open Main Play Screen Loop")]
        public static void OpenMainPlayScreenLoop()
        {
            if (!File.Exists(ScenePath))
            {
                CreateOrRepairMainPlayScreenLoop();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Lost Forest/Bootstrap/Validate Main Play Screen")]
        public static void ValidateMainPlayScreen()
        {
            if (!File.Exists(ScenePath))
            {
                throw new FileNotFoundException($"Main play screen scene is missing: {ScenePath}", ScenePath);
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new InvalidDataException($"Could not open main play screen scene: {ScenePath}");
            }

            RequireSceneComponent<MainPlayScreenLoopAnimator>();
            MainPlayScreenTransitionController transitionController = RequireSceneComponent<MainPlayScreenTransitionController>();
            RequireSceneComponent<BeginRuneGlowGraphic>();
            RequireSceneComponent<Button>();
            RequireSceneComponent<EventSystem>();
            RequireSceneComponent<Camera>();

            if (!transitionController.HasOpeningInstructionSequence || Mathf.Abs(transitionController.AllMessageHoldSeconds - 2f) > 0.001f)
            {
                throw new InvalidDataException("Main play screen must have the full frost-message sequence and hold the complete message for two seconds.");
            }

            RequireFrostMessage("Frost Message - Find The Runes", "FIND THE RUNES");
            RequireFrostMessage("Frost Message - Stop What", "STOP WHAT");
            RequireFrostMessage("Frost Message - Hunts You", "HUNTS YOU");

            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (buildScenes.Length < 2 || !buildScenes[0].enabled || buildScenes[0].path != ScenePath)
            {
                throw new InvalidDataException("Main play screen must be the first enabled scene in Build Settings.");
            }

            bool gameplayIncluded = false;
            for (int i = 1; i < buildScenes.Length; i++)
            {
                if (buildScenes[i].enabled && buildScenes[i].path == GameplayScenePath)
                {
                    gameplayIncluded = true;
                    break;
                }
            }

            if (!gameplayIncluded)
            {
                throw new InvalidDataException("Gameplay scene must follow the main play screen in Build Settings.");
            }

            Debug.Log("Lost Forest main play screen validation passed: title loop, Begin hit area, blue rune transition, frost-message sequence, and gameplay build order are ready.");
        }

        [MenuItem("Lost Forest/Preview/Play Main Screen Begin Transition")]
        public static void PlayMainScreenBeginTransition()
        {
            MainPlayScreenTransitionController controller = Object.FindFirstObjectByType<MainPlayScreenTransitionController>();
            if (!EditorApplication.isPlaying || controller == null)
            {
                throw new InvalidDataException("Open the main play screen and enter Play mode before previewing its Begin transition.");
            }

            controller.BeginGame();
        }

        [MenuItem("Lost Forest/Preview/Play Main Screen Begin Transition", true)]
        public static bool CanPlayMainScreenBeginTransition()
        {
            return EditorApplication.isPlaying && Object.FindFirstObjectByType<MainPlayScreenTransitionController>() != null;
        }

        private static Sprite LoadSprite(string assetPath, bool alphaIsTransparency)
        {
            if (!File.Exists(assetPath))
            {
                throw new FileNotFoundException($"Required main play screen asset is missing: {assetPath}", assetPath);
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = alphaIsTransparency;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            if (sprite == null)
            {
                throw new InvalidDataException($"Could not import sprite asset: {assetPath}");
            }

            return sprite;
        }

        private static RectTransform CreateImage(string name, Transform parent, Sprite sprite, bool raycastTarget)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = raycastTarget;
            image.preserveAspect = false;

            return imageObject.GetComponent<RectTransform>();
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject.GetComponent<RectTransform>();
        }

        private static void CreateTitleCamera()
        {
            GameObject cameraObject = new GameObject("Title Screen Camera", typeof(Camera));
            Camera titleCamera = cameraObject.GetComponent<Camera>();
            titleCamera.clearFlags = CameraClearFlags.SolidColor;
            titleCamera.backgroundColor = Color.white;
            titleCamera.cullingMask = 0;
            titleCamera.depth = -100f;
            titleCamera.useOcclusionCulling = false;
            cameraObject.tag = "MainCamera";
        }

        private static Button CreateBeginButtonHitArea(Transform parent)
        {
            GameObject buttonObject = new GameObject("Begin Button Hit Area", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = BeginButtonPosition;
            buttonRect.sizeDelta = BeginButtonSize;

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.01f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;

            Navigation navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            button.navigation = navigation;

            return button;
        }

        private static MainPlayScreenTransitionController CreateTransitionCanvas(Button beginButton)
        {
            GameObject transitionObject = new GameObject(
                "Opening Scene Transition Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(MainPlayScreenTransitionController));

            Canvas canvas = transitionObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = transitionObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform transitionRect = transitionObject.GetComponent<RectTransform>();
            StretchToParent(transitionRect);

            RectTransform whiteVeilRect = CreateImage("White Loading Veil", transitionRect, null, false);
            StretchToParent(whiteVeilRect);
            Image whiteVeil = whiteVeilRect.GetComponent<Image>();
            whiteVeil.color = new Color(1f, 1f, 1f, 0f);

            GameObject glowObject = new GameObject("Magical Blue Begin Runes", typeof(RectTransform), typeof(CanvasRenderer), typeof(BeginRuneGlowGraphic));
            glowObject.transform.SetParent(transitionRect, false);
            RectTransform glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.pivot = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = BeginGlowPosition;
            glowRect.sizeDelta = BeginGlowSize;

            BeginRuneGlowGraphic runeGlow = glowObject.GetComponent<BeginRuneGlowGraphic>();
            runeGlow.raycastTarget = false;
            runeGlow.SetRuneText("BEGIN");
            runeGlow.SetGlow(0f, 0f);

            CanvasGroup findRunesText = CreateFrostTextGroup(
                "Frost Message - Find The Runes",
                transitionRect,
                "FIND THE RUNES",
                FirstMessageSize.x,
                FirstMessageSize.y);
            RectTransform findRunesRect = findRunesText.GetComponent<RectTransform>();
            findRunesRect.anchorMin = new Vector2(0.5f, 0.5f);
            findRunesRect.anchorMax = new Vector2(0.5f, 0.5f);
            findRunesRect.pivot = new Vector2(0.5f, 0.5f);
            findRunesRect.anchoredPosition = FirstMessagePosition;

            RectTransform secondMessageLine = CreateRect("Frost Message - Second Line", transitionRect);
            secondMessageLine.anchorMin = new Vector2(0.5f, 0.5f);
            secondMessageLine.anchorMax = new Vector2(0.5f, 0.5f);
            secondMessageLine.pivot = new Vector2(0.5f, 0.5f);
            secondMessageLine.anchoredPosition = SecondMessagePosition;
            secondMessageLine.sizeDelta = SecondMessageSize;

            HorizontalLayoutGroup secondLineLayout = secondMessageLine.gameObject.AddComponent<HorizontalLayoutGroup>();
            secondLineLayout.childAlignment = TextAnchor.MiddleCenter;
            secondLineLayout.spacing = 84f;
            secondLineLayout.childControlWidth = true;
            secondLineLayout.childControlHeight = true;
            secondLineLayout.childForceExpandWidth = false;
            secondLineLayout.childForceExpandHeight = false;

            CanvasGroup stopWhatText = CreateFrostTextGroup(
                "Frost Message - Stop What",
                secondMessageLine,
                "STOP WHAT",
                505f,
                130f);
            CanvasGroup huntsYouText = CreateFrostTextGroup(
                "Frost Message - Hunts You",
                secondMessageLine,
                "HUNTS YOU",
                525f,
                130f);

            MainPlayScreenTransitionController controller = transitionObject.GetComponent<MainPlayScreenTransitionController>();
            SerializedObject serializedController = new SerializedObject(controller);
            SetProperty(serializedController, "beginButton", beginButton);
            SetProperty(serializedController, "runeGlow", runeGlow);
            SetProperty(serializedController, "whiteVeil", whiteVeil);
            SetProperty(serializedController, "findRunesText", findRunesText);
            SetProperty(serializedController, "stopWhatText", stopWhatText);
            SetProperty(serializedController, "huntsYouText", huntsYouText);
            SetProperty(serializedController, "firstMessageFadeSeconds", 1.4f);
            SetProperty(serializedController, "secondMessageBeatSeconds", 0.9f);
            SetProperty(serializedController, "secondMessageFadeSeconds", 1.4f);
            SetProperty(serializedController, "allMessageHoldSeconds", 2f);
            SetProperty(serializedController, "contextFadeOutSeconds", 1.6f);
            SetProperty(serializedController, "huntsYouHoldSeconds", 1.5f);
            SetProperty(serializedController, "gameplayFadeInSeconds", 2f);
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        private static CanvasGroup CreateFrostTextGroup(
            string objectName,
            Transform parent,
            string message,
            float preferredWidth,
            float preferredHeight)
        {
            GameObject rootObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement));
            rootObject.transform.SetParent(parent, false);

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(preferredWidth, preferredHeight);

            CanvasGroup canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            LayoutElement layoutElement = rootObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.preferredHeight = preferredHeight;

            GameObject runeObject = new GameObject(
                "Magical Blue Rune Lettering",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(BeginRuneGlowGraphic));
            runeObject.transform.SetParent(rootRect, false);

            RectTransform runeRect = runeObject.GetComponent<RectTransform>();
            StretchToParent(runeRect);

            BeginRuneGlowGraphic runeGraphic = runeObject.GetComponent<BeginRuneGlowGraphic>();
            runeGraphic.raycastTarget = false;
            runeGraphic.SetRuneText(message);
            runeGraphic.SetGlow(1f, 0f);

            return canvasGroup;
        }

        private static void RequireFrostMessage(string objectName, string expectedMessage)
        {
            GameObject messageObject = GameObject.Find(objectName);
            if (messageObject == null || messageObject.GetComponent<CanvasGroup>() == null)
            {
                throw new InvalidDataException($"Main play screen is missing frost message '{expectedMessage}'.");
            }

            BeginRuneGlowGraphic runeGraphic = messageObject.GetComponentInChildren<BeginRuneGlowGraphic>(true);
            if (runeGraphic != null && runeGraphic.RuneText == expectedMessage)
            {
                return;
            }

            throw new InvalidDataException($"Main play screen frost message '{objectName}' does not contain '{expectedMessage}'.");
        }

        private static void EnsureOpeningSceneIsFirstInBuild()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;

            for (int i = 0; i < existingScenes.Length; i++)
            {
                string path = existingScenes[i].path;
                if (path == ScenePath || path == GameplayScenePath)
                {
                    continue;
                }

                scenes.Add(existingScenes[i]);
            }

            scenes.Insert(0, new EditorBuildSettingsScene(GameplayScenePath, true));
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T RequireSceneComponent<T>() where T : Component
        {
            T component = Object.FindFirstObjectByType<T>();
            if (component == null)
            {
                throw new InvalidDataException($"Main play screen is missing required component {typeof(T).Name}.");
            }

            return component;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetProperty(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetProperty(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetProperty(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }
    }
}
