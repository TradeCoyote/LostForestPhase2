using UnityEngine;

namespace LostForest.Phase2.World
{
    public sealed class FieldGenerationDebugRunner : MonoBehaviour
    {
        [SerializeField] private FrameSettings settings = new FrameSettings();
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool logReportOnGenerate = true;

        private FieldData currentField;

        public FieldData CurrentField => currentField;

        private void Start()
        {
            if (generateOnStart)
            {
                Generate();
            }
        }

        [ContextMenu("Generate Field")]
        public void Generate()
        {
            currentField = FieldGenerator.Generate(settings);

            if (logReportOnGenerate && currentField != null)
            {
                Debug.Log(currentField.BuildFieldSlotReport());
            }
        }
    }
}
