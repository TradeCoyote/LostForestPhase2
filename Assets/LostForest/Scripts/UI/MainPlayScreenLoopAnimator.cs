using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LostForest.Phase2.UI
{
    [DisallowMultipleComponent]
    public sealed class MainPlayScreenLoopAnimator : MonoBehaviour
    {
        private const float MinimumLoopDurationSeconds = 0.1f;
        private const float FullTurnDegrees = 360f;

        [Header("Loop")]
        [SerializeField] private float loopDurationSeconds = 15f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Snow")]
        [SerializeField] private RectTransform snowLayer;
        [SerializeField] private int snowClusterCount = 20;
        [SerializeField] private int snowSeed = 8122026;
        [SerializeField] private Vector2 clusterSizePixelsRange = new Vector2(34f, 86f);
        [SerializeField] private Vector2Int flakesPerClusterRange = new Vector2Int(5, 11);
        [SerializeField] private Vector2 flakeRadiusPixelsRange = new Vector2(5f, 15f);
        [SerializeField] private Vector2 snowAlphaRange = new Vector2(0.34f, 0.78f);
        [SerializeField] private Vector2 windPixelsPerLoopRange = new Vector2(130f, 265f);
        [SerializeField] private bool windDriftsRight = true;
        [SerializeField] private float snowWrapMarginPixels = 110f;

        [Header("Talisman")]
        [SerializeField] private RectTransform talismanPivot;
        [SerializeField] private RectTransform talismanVisual;
        [SerializeField] private float talismanTurnDegrees = 45f;
        [SerializeField] private float talismanOvershootDegrees = -8f;
        [SerializeField] private float talismanSwayDegrees = 1.4f;
        [SerializeField] private float talismanHorizontalSwayPixels = 3f;
        [SerializeField] private float talismanLiftPixels = 1.5f;
        [SerializeField] private float talismanNarrowestScaleX = 0.88f;

        [Header("Fresh Blood")]
        [SerializeField] private Image freshBloodAccent;
        [SerializeField] private float freshBloodPulseAlpha = 0.08f;

        private readonly List<SnowCluster> snowClusters = new List<SnowCluster>(20);

        private float loopStartTimeSeconds;
        private Vector2 lastSnowLayerSize;
        private int lastSnowClusterCount;
        private int lastSnowSeed;
        private Vector2 baseTalismanPivotPosition;
        private Quaternion baseTalismanVisualRotation = Quaternion.identity;
        private Vector3 baseTalismanVisualScale = Vector3.one;
        private Color baseFreshBloodColor = Color.white;
        private bool talismanBaseCaptured;

        private sealed class SnowCluster
        {
            public RectTransform RectTransform;
            public Sprite Sprite;
            public Texture2D Texture;
            public float StartingProgress;
            public float DriftOriginX;
            public float WindPixelsPerLoop;
        }

        private void Awake()
        {
            EnsureSnowLayer();
            RebuildSnowClusters();
        }

        private void OnEnable()
        {
            loopStartTimeSeconds = CurrentTimeSeconds();
            CaptureTalismanBasePose();
        }

        private void OnDestroy()
        {
            DestroyGeneratedSnowAssets();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                RebuildSnowClusters();
            }
        }

        private void OnValidate()
        {
            loopDurationSeconds = Mathf.Max(MinimumLoopDurationSeconds, loopDurationSeconds);
            snowClusterCount = Mathf.Max(0, snowClusterCount);
            snowWrapMarginPixels = Mathf.Max(0f, snowWrapMarginPixels);
            talismanTurnDegrees = Mathf.Clamp(talismanTurnDegrees, -FullTurnDegrees, FullTurnDegrees);
            talismanOvershootDegrees = Mathf.Clamp(talismanOvershootDegrees, -FullTurnDegrees, FullTurnDegrees);
            talismanNarrowestScaleX = Mathf.Clamp(talismanNarrowestScaleX, 0.05f, 1f);

            clusterSizePixelsRange = SortPositiveRange(clusterSizePixelsRange, 1f);
            flakeRadiusPixelsRange = SortPositiveRange(flakeRadiusPixelsRange, 1f);
            snowAlphaRange = SortRange(snowAlphaRange);
            windPixelsPerLoopRange = SortPositiveRange(windPixelsPerLoopRange, 0f);

            flakesPerClusterRange.x = Mathf.Max(1, flakesPerClusterRange.x);
            flakesPerClusterRange.y = Mathf.Max(flakesPerClusterRange.x, flakesPerClusterRange.y);
        }

        private void Update()
        {
            EnsureSnowLayer();

            Vector2 snowLayerSize = GetSnowLayerSize();
            if (snowLayerSize != lastSnowLayerSize || snowClusterCount != lastSnowClusterCount || snowSeed != lastSnowSeed)
            {
                RebuildSnowClusters();
            }

            float loopProgress = GetLoopProgress01();
            AnimateSnow(loopProgress);
            AnimateTalisman(loopProgress);
            AnimateFreshBlood(loopProgress);
        }

        private void EnsureSnowLayer()
        {
            if (snowLayer != null)
            {
                return;
            }

            GameObject snowObject = new GameObject("Looping Snow Layer", typeof(RectTransform));
            snowObject.transform.SetParent(transform, false);
            snowLayer = snowObject.GetComponent<RectTransform>();
            StretchToParent(snowLayer);
        }

        private void RebuildSnowClusters()
        {
            if (snowLayer == null)
            {
                return;
            }

            DestroyGeneratedSnowAssets();
            ClearSnowLayerChildren();

            Vector2 layerSize = GetSnowLayerSize();
            if (layerSize.x <= 0f || layerSize.y <= 0f || snowClusterCount <= 0)
            {
                lastSnowLayerSize = layerSize;
                lastSnowClusterCount = snowClusterCount;
                lastSnowSeed = snowSeed;
                return;
            }

            System.Random random = new System.Random(snowSeed);
            float left = -layerSize.x * 0.5f - snowWrapMarginPixels;
            float right = layerSize.x * 0.5f + snowWrapMarginPixels;
            float windDirection = windDriftsRight ? 1f : -1f;

            for (int i = 0; i < snowClusterCount; i++)
            {
                float initialProgress = NextFloat(random, 0f, 1f);
                float initialX = NextFloat(random, left, right);
                float windPixels = NextFloat(random, windPixelsPerLoopRange.x, windPixelsPerLoopRange.y) * windDirection;
                float sizePixels = NextFloat(random, clusterSizePixelsRange.x, clusterSizePixelsRange.y);

                GameObject clusterObject = new GameObject($"Snow Cluster {i + 1:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                clusterObject.transform.SetParent(snowLayer, false);

                RectTransform clusterRect = clusterObject.GetComponent<RectTransform>();
                clusterRect.anchorMin = new Vector2(0.5f, 0.5f);
                clusterRect.anchorMax = new Vector2(0.5f, 0.5f);
                clusterRect.pivot = new Vector2(0.5f, 0.5f);
                clusterRect.sizeDelta = new Vector2(sizePixels, sizePixels);

                Texture2D texture = CreateSnowClusterTexture(random, i);
                Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
                sprite.name = $"Snow Cluster Sprite {i + 1:00}";

                Image image = clusterObject.GetComponent<Image>();
                image.sprite = sprite;
                image.color = Color.white;
                image.raycastTarget = false;
                image.preserveAspect = true;

                snowClusters.Add(new SnowCluster
                {
                    RectTransform = clusterRect,
                    Sprite = sprite,
                    Texture = texture,
                    StartingProgress = initialProgress,
                    DriftOriginX = initialX - windPixels * initialProgress,
                    WindPixelsPerLoop = windPixels
                });
            }

            lastSnowLayerSize = layerSize;
            lastSnowClusterCount = snowClusterCount;
            lastSnowSeed = snowSeed;
        }

        private void AnimateSnow(float loopProgress)
        {
            if (snowLayer == null || snowClusters.Count == 0)
            {
                return;
            }

            Vector2 layerSize = GetSnowLayerSize();
            float top = layerSize.y * 0.5f + snowWrapMarginPixels;
            float bottom = -layerSize.y * 0.5f - snowWrapMarginPixels;
            float left = -layerSize.x * 0.5f - snowWrapMarginPixels;
            float right = layerSize.x * 0.5f + snowWrapMarginPixels;

            for (int i = 0; i < snowClusters.Count; i++)
            {
                SnowCluster cluster = snowClusters[i];
                if (cluster.RectTransform == null)
                {
                    continue;
                }

                float pathProgress = Mathf.Repeat(cluster.StartingProgress + loopProgress, 1f);
                float y = Mathf.Lerp(top, bottom, pathProgress);
                float x = Wrap(cluster.DriftOriginX + cluster.WindPixelsPerLoop * pathProgress, left, right);
                cluster.RectTransform.anchoredPosition = new Vector2(x, y);
            }
        }

        private void AnimateTalisman(float loopProgress)
        {
            if (talismanVisual == null)
            {
                return;
            }

            if (!talismanBaseCaptured)
            {
                CaptureTalismanBasePose();
            }

            float turnDegrees = EvaluateTalismanTurn(loopProgress);
            float swayDegrees = Mathf.Sin(loopProgress * Mathf.PI * 2f) * talismanSwayDegrees;
            Quaternion motionRotation = Quaternion.Euler(0f, turnDegrees, swayDegrees);
            talismanVisual.localRotation = baseTalismanVisualRotation * motionRotation;

            float turnAmount = Mathf.InverseLerp(0f, Mathf.Max(0.001f, Mathf.Abs(talismanTurnDegrees)), Mathf.Abs(turnDegrees));
            Vector3 scale = baseTalismanVisualScale;
            scale.x *= Mathf.Lerp(1f, talismanNarrowestScaleX, turnAmount);
            talismanVisual.localScale = scale;

            if (talismanPivot != null)
            {
                float swayPixels = Mathf.Sin(loopProgress * Mathf.PI * 2f) * talismanHorizontalSwayPixels;
                float liftPixels = Mathf.Sin(loopProgress * Mathf.PI * 4f + Mathf.PI * 0.3f) * talismanLiftPixels;
                talismanPivot.anchoredPosition = baseTalismanPivotPosition + new Vector2(swayPixels, liftPixels);
            }
        }

        private void AnimateFreshBlood(float loopProgress)
        {
            if (freshBloodAccent == null)
            {
                return;
            }

            float wetPulse = Mathf.Sin(loopProgress * Mathf.PI * 2f + Mathf.PI * 0.4f) * 0.5f + 0.5f;
            Color color = baseFreshBloodColor;
            color.a = Mathf.Clamp01(baseFreshBloodColor.a + wetPulse * freshBloodPulseAlpha);
            freshBloodAccent.color = color;
        }

        private float EvaluateTalismanTurn(float loopProgress)
        {
            if (loopProgress < 0.32f)
            {
                float segmentProgress = Smooth01(loopProgress / 0.32f);
                return Mathf.Lerp(0f, talismanTurnDegrees, segmentProgress);
            }

            if (loopProgress < 0.72f)
            {
                float segmentProgress = Smooth01((loopProgress - 0.32f) / 0.4f);
                return Mathf.Lerp(talismanTurnDegrees, talismanOvershootDegrees, segmentProgress);
            }

            float finalProgress = Smooth01((loopProgress - 0.72f) / 0.28f);
            return Mathf.Lerp(talismanOvershootDegrees, 0f, finalProgress);
        }

        private void CaptureTalismanBasePose()
        {
            if (talismanPivot != null)
            {
                baseTalismanPivotPosition = talismanPivot.anchoredPosition;
            }

            if (talismanVisual != null)
            {
                baseTalismanVisualRotation = talismanVisual.localRotation;
                baseTalismanVisualScale = talismanVisual.localScale;
            }

            if (freshBloodAccent != null)
            {
                baseFreshBloodColor = freshBloodAccent.color;
            }

            talismanBaseCaptured = true;
        }

        private float GetLoopProgress01()
        {
            float duration = Mathf.Max(MinimumLoopDurationSeconds, loopDurationSeconds);
            return Mathf.Repeat(CurrentTimeSeconds() - loopStartTimeSeconds, duration) / duration;
        }

        private float CurrentTimeSeconds()
        {
            return useUnscaledTime ? Time.unscaledTime : Time.time;
        }

        private Vector2 GetSnowLayerSize()
        {
            if (snowLayer == null)
            {
                return Vector2.zero;
            }

            Rect rect = snowLayer.rect;
            return new Vector2(rect.width, rect.height);
        }

        private void ClearSnowLayerChildren()
        {
            for (int i = snowLayer.childCount - 1; i >= 0; i--)
            {
                DestroyObject(snowLayer.GetChild(i).gameObject);
            }
        }

        private void DestroyGeneratedSnowAssets()
        {
            for (int i = 0; i < snowClusters.Count; i++)
            {
                SnowCluster cluster = snowClusters[i];
                if (cluster.Sprite != null)
                {
                    DestroyObject(cluster.Sprite);
                }

                if (cluster.Texture != null)
                {
                    DestroyObject(cluster.Texture);
                }
            }

            snowClusters.Clear();
        }

        private Texture2D CreateSnowClusterTexture(System.Random random, int clusterIndex)
        {
            const int textureSize = 128;
            Color32[] pixels = new Color32[textureSize * textureSize];
            int flakeCount = random.Next(flakesPerClusterRange.x, flakesPerClusterRange.y + 1);
            float alphaMultiplier = NextFloat(random, snowAlphaRange.x, snowAlphaRange.y);

            for (int flakeIndex = 0; flakeIndex < flakeCount; flakeIndex++)
            {
                float centerX = NextFloat(random, 18f, textureSize - 18f);
                float centerY = NextFloat(random, 18f, textureSize - 18f);
                float radius = NextFloat(random, flakeRadiusPixelsRange.x, flakeRadiusPixelsRange.y);
                float flakeAlpha = NextFloat(random, 0.5f, 1f) * alphaMultiplier;
                DrawSoftDisc(pixels, textureSize, centerX, centerY, radius, flakeAlpha);
            }

            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.name = $"Looping Snow Cluster Texture {clusterIndex + 1:00}";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void DrawSoftDisc(Color32[] pixels, int textureSize, float centerX, float centerY, float radius, float alpha)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - radius));
            int maxX = Mathf.Min(textureSize - 1, Mathf.CeilToInt(centerX + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(centerY - radius));
            int maxY = Mathf.Min(textureSize - 1, Mathf.CeilToInt(centerY + radius));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (distance > radius)
                    {
                        continue;
                    }

                    float normalizedDistance = distance / radius;
                    float falloff = 1f - Smooth01(normalizedDistance);
                    byte sourceAlpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha * falloff) * 255f);
                    int index = y * textureSize + x;
                    byte targetAlpha = pixels[index].a > sourceAlpha ? pixels[index].a : sourceAlpha;
                    pixels[index] = new Color32(255, 255, 255, targetAlpha);
                }
            }
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Vector2 SortRange(Vector2 range)
        {
            return range.x <= range.y ? range : new Vector2(range.y, range.x);
        }

        private static Vector2 SortPositiveRange(Vector2 range, float minimum)
        {
            range.x = Mathf.Max(minimum, range.x);
            range.y = Mathf.Max(minimum, range.y);
            return SortRange(range);
        }

        private static float NextFloat(System.Random random, float minInclusive, float maxInclusive)
        {
            return Mathf.Lerp(minInclusive, maxInclusive, (float)random.NextDouble());
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static float Wrap(float value, float minInclusive, float maxExclusive)
        {
            float range = maxExclusive - minInclusive;
            if (range <= 0f)
            {
                return minInclusive;
            }

            return minInclusive + Mathf.Repeat(value - minInclusive, range);
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
