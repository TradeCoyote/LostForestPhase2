using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.Debugging
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class GridDebugMeshText : MonoBehaviour
    {
        private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['0'] = new[] { "111", "101", "101", "101", "101", "101", "111" },
            ['1'] = new[] { "010", "110", "010", "010", "010", "010", "111" },
            ['2'] = new[] { "111", "001", "001", "111", "100", "100", "111" },
            ['3'] = new[] { "111", "001", "001", "111", "001", "001", "111" },
            ['4'] = new[] { "101", "101", "101", "111", "001", "001", "001" },
            ['5'] = new[] { "111", "100", "100", "111", "001", "001", "111" },
            ['6'] = new[] { "111", "100", "100", "111", "101", "101", "111" },
            ['7'] = new[] { "111", "001", "001", "010", "010", "100", "100" },
            ['8'] = new[] { "111", "101", "101", "111", "101", "101", "111" },
            ['9'] = new[] { "111", "101", "101", "111", "001", "001", "111" },
            ['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['B'] = new[] { "11110", "10001", "10001", "11110", "10001", "10001", "11110" },
            ['C'] = new[] { "01111", "10000", "10000", "10000", "10000", "10000", "01111" },
            ['D'] = new[] { "11110", "10001", "10001", "10001", "10001", "10001", "11110" },
            ['E'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" },
            ['F'] = new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" },
            ['G'] = new[] { "01111", "10000", "10000", "10011", "10001", "10001", "01111" },
            ['H'] = new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['I'] = new[] { "111", "010", "010", "010", "010", "010", "111" },
            ['J'] = new[] { "00111", "00010", "00010", "00010", "10010", "10010", "01100" },
            ['K'] = new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" },
            ['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
            ['M'] = new[] { "10001", "11011", "10101", "10101", "10001", "10001", "10001" },
            ['N'] = new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" },
            ['O'] = new[] { "01110", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['P'] = new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" },
            ['Q'] = new[] { "01110", "10001", "10001", "10001", "10101", "10010", "01101" },
            ['R'] = new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" },
            ['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
            ['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
            ['U'] = new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" },
            ['V'] = new[] { "10001", "10001", "10001", "10001", "10001", "01010", "00100" },
            ['W'] = new[] { "10001", "10001", "10001", "10101", "10101", "10101", "01010" },
            ['X'] = new[] { "10001", "10001", "01010", "00100", "01010", "10001", "10001" },
            ['Y'] = new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" },
            ['Z'] = new[] { "11111", "00001", "00010", "00100", "01000", "10000", "11111" },
            ['-'] = new[] { "000", "000", "000", "111", "000", "000", "000" },
            ['_'] = new[] { "000", "000", "000", "000", "000", "000", "111" },
            ['.'] = new[] { "0", "0", "0", "0", "0", "0", "1" },
            [':'] = new[] { "0", "1", "0", "0", "0", "1", "0" },
            ['%'] = new[] { "10001", "00010", "00100", "00100", "01000", "10001", "00000" },
            ['/'] = new[] { "00001", "00010", "00100", "00100", "01000", "10000", "00000" },
            ['('] = new[] { "01", "10", "10", "10", "10", "10", "01" },
            [')'] = new[] { "10", "01", "01", "01", "01", "01", "10" },
            ['='] = new[] { "000", "111", "000", "111", "000", "000", "000" },
            ['+'] = new[] { "000", "010", "010", "111", "010", "010", "000" }
        };

        private readonly List<Vector3> vertices = new List<Vector3>(4096);
        private readonly List<int> triangles = new List<int>(6144);
        private readonly List<Color> colors = new List<Color>(4096);

        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Material material;
        private Color textColor = Color.black;

        public void Configure(Color color)
        {
            textColor = color;
            EnsureMesh();
            meshRenderer.enabled = true;
            meshRenderer.sortingOrder = 100;
        }

        public void SetText(string value, Vector2 bounds)
        {
            EnsureMesh();
            BuildMesh(string.IsNullOrEmpty(value) ? string.Empty : value.ToUpperInvariant(), bounds);
        }

        private void EnsureMesh()
        {
            meshFilter = meshFilter == null ? GetComponent<MeshFilter>() : meshFilter;
            meshRenderer = meshRenderer == null ? GetComponent<MeshRenderer>() : meshRenderer;

            if (mesh == null)
            {
                mesh = new Mesh { name = "Grid Debug HUD Text Mesh" };
                mesh.MarkDynamic();
                meshFilter.sharedMesh = mesh;
            }

            if (material == null)
            {
                Shader shader = Shader.Find("Sprites/Default");

                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader)
                {
                    name = "Grid Debug HUD Text Material",
                    color = textColor
                };

                if (material.HasProperty("_Cull"))
                {
                    material.SetFloat("_Cull", 0f);
                }
            }

            material.color = textColor;
            meshRenderer.sharedMaterial = material;
        }

        private void BuildMesh(string value, Vector2 bounds)
        {
            vertices.Clear();
            triangles.Clear();
            colors.Clear();

            string[] lines = value.Replace('\r', '\n').Split('\n');
            int maxColumns = 1;

            for (int i = 0; i < lines.Length; i++)
            {
                maxColumns = Mathf.Max(maxColumns, MeasureColumns(lines[i]));
            }

            float boundedWidth = Mathf.Max(0.05f, bounds.x);
            float boundedHeight = Mathf.Max(0.05f, bounds.y);
            float pixelByWidth = boundedWidth / Mathf.Max(1f, maxColumns);
            float pixelByHeight = boundedHeight / Mathf.Max(1f, lines.Length * 9f);
            float pixelSize = Mathf.Min(pixelByWidth, pixelByHeight);
            float lineAdvance = pixelSize * 9f;
            float cursorY = 0f;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                float cursorX = 0f;
                string line = lines[lineIndex];

                for (int charIndex = 0; charIndex < line.Length; charIndex++)
                {
                    char character = line[charIndex];

                    if (character == ' ')
                    {
                        cursorX += pixelSize * 4f;
                        continue;
                    }

                    if (!Glyphs.TryGetValue(character, out string[] glyph))
                    {
                        cursorX += pixelSize * 4f;
                        continue;
                    }

                    AddGlyph(glyph, cursorX, cursorY, pixelSize);
                    cursorX += (GetGlyphWidth(glyph) + 1) * pixelSize;
                }

                cursorY -= lineAdvance;
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();
        }

        private void AddGlyph(IReadOnlyList<string> glyph, float originX, float originY, float pixelSize)
        {
            for (int row = 0; row < glyph.Count; row++)
            {
                string rowPattern = glyph[row];

                for (int column = 0; column < rowPattern.Length; column++)
                {
                    if (rowPattern[column] != '1')
                    {
                        continue;
                    }

                    AddQuad(originX + column * pixelSize, originY - row * pixelSize, pixelSize);
                }
            }
        }

        private void AddQuad(float x, float y, float size)
        {
            int vertexStart = vertices.Count;
            vertices.Add(new Vector3(x, y, 0f));
            vertices.Add(new Vector3(x + size, y, 0f));
            vertices.Add(new Vector3(x + size, y - size, 0f));
            vertices.Add(new Vector3(x, y - size, 0f));
            colors.Add(textColor);
            colors.Add(textColor);
            colors.Add(textColor);
            colors.Add(textColor);
            triangles.Add(vertexStart);
            triangles.Add(vertexStart + 1);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart + 3);
        }

        private static int MeasureColumns(string line)
        {
            int columns = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ' ')
                {
                    columns += 4;
                    continue;
                }

                columns += Glyphs.TryGetValue(line[i], out string[] glyph) ? GetGlyphWidth(glyph) + 1 : 4;
            }

            return Mathf.Max(1, columns);
        }

        private static int GetGlyphWidth(IReadOnlyList<string> glyph)
        {
            int width = 0;

            for (int i = 0; i < glyph.Count; i++)
            {
                width = Mathf.Max(width, glyph[i].Length);
            }

            return width;
        }
    }
}
