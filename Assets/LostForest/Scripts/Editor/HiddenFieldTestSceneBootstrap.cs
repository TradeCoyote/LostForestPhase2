#if UNITY_EDITOR
using System.IO;
using LostForest.Phase2.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostForest.Phase2.Editor
{
    public static class HiddenFieldTestSceneBootstrap
    {
        private const string ScenePath = "Assets/LostForest/Scenes/Phase2_HiddenFieldTest.unity";

        [MenuItem("Lost Forest/Bootstrap/Open Hidden Field Test Scene")]
        public static void OpenHiddenFieldTestScene()
        {
            if (!File.Exists(ScenePath))
            {
                CreateOrRepairHiddenFieldTestScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Selection.activeGameObject = Object.FindFirstObjectByType<FieldGenerationDebugRunner>()?.gameObject;
        }

        [MenuItem("Lost Forest/Bootstrap/Create or Repair Hidden Field Test Scene")]
        public static void CreateOrRepairHiddenFieldTestScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            FieldGenerationDebugRunner existingRunner = Object.FindFirstObjectByType<FieldGenerationDebugRunner>();
            GameObject runnerObject = existingRunner != null
                ? existingRunner.gameObject
                : new GameObject("Field Generation Debug Runner");

            if (existingRunner == null)
            {
                runnerObject.AddComponent<FieldGenerationDebugRunner>();
            }

            Selection.activeGameObject = runnerObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Lost Forest hidden Field test scene is ready: {ScenePath}");
        }
    }
}
#endif

