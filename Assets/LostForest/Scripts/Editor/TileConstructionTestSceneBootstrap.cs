#if UNITY_EDITOR
using System.IO;
using LostForest.Phase2.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostForest.Phase2.Editor
{
    public static class TileConstructionTestSceneBootstrap
    {
        private const string ScenePath = "Assets/LostForest/Scenes/Phase2_TileConstructionTest.unity";

        [MenuItem("Lost Forest/Bootstrap/Open Tile Construction Test Scene")]
        public static void OpenTileConstructionTestScene()
        {
            if (!File.Exists(ScenePath))
            {
                CreateOrRepairTileConstructionTestScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Selection.activeGameObject = Object.FindFirstObjectByType<TileConstructionDebugRunner>()?.gameObject;
        }

        [MenuItem("Lost Forest/Bootstrap/Create or Repair Tile Construction Test Scene")]
        public static void CreateOrRepairTileConstructionTestScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));

            Scene scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            TileConstructionDebugRunner existingRunner = Object.FindFirstObjectByType<TileConstructionDebugRunner>();
            GameObject runnerObject = existingRunner != null
                ? existingRunner.gameObject
                : new GameObject("Tile Construction Debug Runner");

            TileConstructionDebugRunner runner = existingRunner != null
                ? existingRunner
                : runnerObject.AddComponent<TileConstructionDebugRunner>();

            runner.Rebuild();
            EnsureCamera();
            EnsureLight();

            Selection.activeGameObject = runnerObject;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Lost Forest tile construction test scene is ready: {ScenePath}");
        }

        private static void EnsureCamera()
        {
            Camera camera = Object.FindFirstObjectByType<Camera>();

            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Tile Construction Test Camera");
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.transform.position = new Vector3(1125f, 270f, 1225f);
            camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 155f;
        }

        private static void EnsureLight()
        {
            Light light = Object.FindFirstObjectByType<Light>();

            if (light != null)
            {
                return;
            }

            GameObject lightObject = new GameObject("Tile Construction Test Light");
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
#endif
