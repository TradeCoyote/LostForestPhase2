using UnityEngine;

namespace LostForest.Phase2.Player
{
    public sealed class EarlyWalkThruPositionLogger : MonoBehaviour
    {
        [SerializeField] private float logIntervalSeconds = 2f;
        [SerializeField] private bool logOnlyWhilePlaying = true;

        private float nextLogTime;

        private void Update()
        {
            if (logOnlyWhilePlaying && !Application.isPlaying)
            {
                return;
            }

            if (Time.time < nextLogTime)
            {
                return;
            }

            nextLogTime = Time.time + Mathf.Max(0.25f, logIntervalSeconds);
            Vector3 position = transform.position;
            Debug.Log($"Lost Forest Early WalkThru player XYZ=({position.x:0.00}, {position.y:0.00}, {position.z:0.00})");
        }
    }
}
