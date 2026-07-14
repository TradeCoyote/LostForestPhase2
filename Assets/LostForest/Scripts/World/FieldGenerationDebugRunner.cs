using UnityEngine;

namespace LostForest.Phase2.World
{
    public sealed class FieldGenerationDebugRunner : MonoBehaviour
    {
        [SerializeField] private FrameSettings settings = new FrameSettings();
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private bool logSummaryOnGenerate = true;
        [SerializeField] private bool logReportOnGenerate = true;

        private FieldData currentField;

        public FieldData CurrentField => currentField;

        private void Awake()
        {
            Debug.Log("Lost Forest Phase 2 FieldGenerationDebugRunner is active.", this);
        }

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

            if (currentField == null)
            {
                Debug.LogError("Lost Forest Phase 2 Field generation failed: no FieldData was returned.", this);
                return;
            }

            if (logSummaryOnGenerate)
            {
                Debug.Log(
                    $"Lost Forest Phase 2 Field generated. Rows={currentField.Rows}, Columns={currentField.Columns}, Slots={currentField.SlotsFilled}, Seed={currentField.Seed}",
                    this);
            }

            if (logReportOnGenerate)
            {
                Debug.Log(currentField.BuildFieldSlotReport());
            }
        }
    }
}
