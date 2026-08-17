using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LostForest.Phase2.UI
{
    [DisallowMultipleComponent]
    public sealed class BeginRuneGlowGraphic : MaskableGraphic
    {
        private const float GlyphWidth = 0.72f;
        private const float GlyphGap = 0.18f;
        private const float WordWidth = 0.55f;
        private const float DesignPixelsPerGlyphUnit = 100f;

        private static readonly Dictionary<char, RuneStroke[]> Glyphs = new Dictionary<char, RuneStroke[]>
        {
            ['A'] = Strokes(0f, -0.5f, 0.36f, 0.5f, 0.36f, 0.5f, 0.72f, -0.5f, 0.15f, -0.08f, 0.57f, -0.08f),
            ['B'] = Strokes(0f, 0.5f, 0f, -0.5f, 0f, 0.5f, 0.62f, 0.24f, 0.62f, 0.24f, 0f, 0.02f, 0f, 0.02f, 0.62f, -0.24f, 0.62f, -0.24f, 0f, -0.5f),
            ['D'] = Strokes(0f, 0.5f, 0f, -0.5f, 0f, 0.5f, 0.68f, 0f, 0.68f, 0f, 0f, -0.5f),
            ['E'] = Strokes(0.68f, 0.5f, 0f, 0f, 0f, 0f, 0.68f, -0.5f, 0f, 0f, 0.56f, 0f),
            ['F'] = Strokes(0f, -0.5f, 0f, 0.5f, 0f, 0.5f, 0.68f, 0.5f, 0f, 0f, 0.56f, 0f),
            ['G'] = Strokes(0.68f, 0.5f, 0f, 0f, 0f, 0f, 0.68f, -0.5f, 0.68f, -0.5f, 0.68f, -0.06f, 0.68f, -0.06f, 0.38f, -0.06f),
            ['H'] = Strokes(0f, 0.5f, 0f, -0.5f, 0.72f, 0.5f, 0.72f, -0.5f, 0f, 0f, 0.72f, 0f),
            ['I'] = Strokes(0.36f, 0.5f, 0.36f, -0.5f),
            ['N'] = Strokes(0f, -0.5f, 0f, 0.5f, 0f, 0.5f, 0.72f, -0.5f, 0.72f, -0.5f, 0.72f, 0.5f),
            ['O'] = Strokes(0.36f, 0.5f, 0f, 0f, 0f, 0f, 0.36f, -0.5f, 0.36f, -0.5f, 0.72f, 0f, 0.72f, 0f, 0.36f, 0.5f),
            ['P'] = Strokes(0f, -0.5f, 0f, 0.5f, 0f, 0.5f, 0.65f, 0.22f, 0.65f, 0.22f, 0f, 0f),
            ['R'] = Strokes(0f, -0.5f, 0f, 0.5f, 0f, 0.5f, 0.65f, 0.22f, 0.65f, 0.22f, 0f, 0f, 0.28f, 0f, 0.72f, -0.5f),
            ['S'] = Strokes(0.7f, 0.5f, 0f, 0.18f, 0f, 0.18f, 0.7f, -0.18f, 0.7f, -0.18f, 0f, -0.5f),
            ['T'] = Strokes(0f, 0.5f, 0.72f, 0.5f, 0.36f, 0.5f, 0.36f, -0.5f),
            ['U'] = Strokes(0f, 0.5f, 0f, -0.3f, 0f, -0.3f, 0.36f, -0.5f, 0.36f, -0.5f, 0.72f, -0.3f, 0.72f, -0.3f, 0.72f, 0.5f),
            ['W'] = Strokes(0f, 0.5f, 0.15f, -0.5f, 0.15f, -0.5f, 0.36f, 0.05f, 0.36f, 0.05f, 0.57f, -0.5f, 0.57f, -0.5f, 0.72f, 0.5f),
            ['Y'] = Strokes(0f, 0.5f, 0.36f, 0f, 0.72f, 0.5f, 0.36f, 0f, 0.36f, 0f, 0.36f, -0.5f)
        };

        [SerializeField] private string runeText = "BEGIN";
        [SerializeField] private Color magicalBlue = new Color(0.08f, 0.68f, 1f, 1f);
        [SerializeField] private float outerAuraWidth = 28f;
        [SerializeField] private float innerAuraWidth = 14f;
        [SerializeField] private float carvingWidth = 6f;
        [SerializeField, Range(0f, 1f)] private float glowIntensity = 1f;

        private float magicPhase;

        private readonly struct RuneStroke
        {
            public RuneStroke(float startX, float startY, float endX, float endY)
            {
                Start = new Vector2(startX, startY);
                End = new Vector2(endX, endY);
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
        }

        public string RuneText => runeText;

        public void SetRuneText(string text)
        {
            runeText = string.IsNullOrWhiteSpace(text) ? string.Empty : text.ToUpperInvariant();
            SetVerticesDirty();
        }

        public void SetGlow(float intensity, float phase)
        {
            glowIntensity = Mathf.Clamp01(intensity);
            magicPhase = phase;
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            runeText = string.IsNullOrWhiteSpace(runeText) ? string.Empty : runeText.ToUpperInvariant();
            outerAuraWidth = Mathf.Max(1f, outerAuraWidth);
            innerAuraWidth = Mathf.Clamp(innerAuraWidth, 1f, outerAuraWidth);
            carvingWidth = Mathf.Clamp(carvingWidth, 1f, innerAuraWidth);
            glowIntensity = Mathf.Clamp01(glowIntensity);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            if (glowIntensity <= 0.001f || string.IsNullOrWhiteSpace(runeText))
            {
                return;
            }

            string displayText = runeText.ToUpperInvariant();
            float textWidthUnits = CalculateTextWidth(displayText);
            if (textWidthUnits <= 0.001f)
            {
                return;
            }

            Rect rect = rectTransform.rect;
            float scale = Mathf.Min(rect.width / textWidthUnits, rect.height / 1.12f);
            float strokeScale = Mathf.Max(0.45f, scale / DesignPixelsPerGlyphUnit);
            float cursorX = rect.center.x - textWidthUnits * scale * 0.5f;

            for (int characterIndex = 0; characterIndex < displayText.Length; characterIndex++)
            {
                char character = displayText[characterIndex];
                if (character == ' ')
                {
                    cursorX += WordWidth * scale;
                    continue;
                }

                if (Glyphs.TryGetValue(character, out RuneStroke[] strokes))
                {
                    DrawGlyph(vertexHelper, strokes, new Vector2(cursorX, rect.center.y), scale, strokeScale, characterIndex);
                }

                cursorX += (GlyphWidth + GlyphGap) * scale;
            }
        }

        private void DrawGlyph(VertexHelper vertexHelper, RuneStroke[] strokes, Vector2 origin, float scale, float strokeScale, int characterIndex)
        {
            DrawLayer(vertexHelper, strokes, origin, scale, outerAuraWidth * strokeScale, 0.1f, 0f, characterIndex);
            DrawLayer(vertexHelper, strokes, origin, scale, innerAuraWidth * strokeScale, 0.24f, 0.7f, characterIndex);
            DrawLayer(vertexHelper, strokes, origin, scale, carvingWidth * strokeScale, 0.95f, 1.4f, characterIndex);
            DrawLayer(vertexHelper, strokes, origin, scale, Mathf.Max(1.5f, carvingWidth * 0.32f) * strokeScale, 0.82f, 2.1f, characterIndex, true);
        }

        private void DrawLayer(
            VertexHelper vertexHelper,
            RuneStroke[] strokes,
            Vector2 origin,
            float scale,
            float width,
            float layerAlpha,
            float phaseOffset,
            int characterIndex,
            bool paleCore = false)
        {
            for (int strokeIndex = 0; strokeIndex < strokes.Length; strokeIndex++)
            {
                RuneStroke stroke = strokes[strokeIndex];
                Vector2 start = origin + stroke.Start * scale;
                Vector2 end = origin + stroke.End * scale;
                float shimmer = 0.78f + Mathf.Sin(magicPhase + phaseOffset + characterIndex * 0.91f + strokeIndex * 1.37f) * 0.22f;
                float alpha = Mathf.Clamp01(glowIntensity * layerAlpha * shimmer);
                Color layerColor = paleCore ? Color.Lerp(magicalBlue, Color.white, 0.72f) : magicalBlue;
                layerColor.a = alpha;

                AddLine(vertexHelper, start, end, width, layerColor);
                AddDisc(vertexHelper, start, width * 0.5f, layerColor);
                AddDisc(vertexHelper, end, width * 0.5f, layerColor);
            }
        }

        private static float CalculateTextWidth(string text)
        {
            float width = 0f;
            bool hasPreviousGlyph = false;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    width += WordWidth;
                    hasPreviousGlyph = false;
                    continue;
                }

                if (hasPreviousGlyph)
                {
                    width += GlyphGap;
                }

                width += GlyphWidth;
                hasPreviousGlyph = true;
            }

            return width;
        }

        private static RuneStroke[] Strokes(params float[] coordinates)
        {
            RuneStroke[] strokes = new RuneStroke[coordinates.Length / 4];
            for (int i = 0; i < strokes.Length; i++)
            {
                int coordinateIndex = i * 4;
                strokes[i] = new RuneStroke(
                    coordinates[coordinateIndex],
                    coordinates[coordinateIndex + 1],
                    coordinates[coordinateIndex + 2],
                    coordinates[coordinateIndex + 3]);
            }

            return strokes;
        }

        private static void AddLine(VertexHelper vertexHelper, Vector2 start, Vector2 end, float width, Color color)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * (width * 0.5f);
            int firstVertex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(start - normal, color, Vector2.zero);
            vertexHelper.AddVert(start + normal, color, Vector2.up);
            vertexHelper.AddVert(end + normal, color, Vector2.one);
            vertexHelper.AddVert(end - normal, color, Vector2.right);
            vertexHelper.AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
            vertexHelper.AddTriangle(firstVertex, firstVertex + 2, firstVertex + 3);
        }

        private static void AddDisc(VertexHelper vertexHelper, Vector2 center, float radius, Color color)
        {
            const int SideCount = 10;
            int centerVertex = vertexHelper.currentVertCount;
            vertexHelper.AddVert(center, color, new Vector2(0.5f, 0.5f));

            for (int side = 0; side <= SideCount; side++)
            {
                float angle = side * Mathf.PI * 2f / SideCount;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vertexHelper.AddVert(center + direction * radius, color, direction * 0.5f + Vector2.one * 0.5f);

                if (side > 0)
                {
                    vertexHelper.AddTriangle(centerVertex, centerVertex + side, centerVertex + side + 1);
                }
            }
        }
    }
}
