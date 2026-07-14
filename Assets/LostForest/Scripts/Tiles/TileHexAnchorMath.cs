using System.Collections.Generic;
using UnityEngine;

namespace LostForest.Phase2.Tiles
{
    public static class TileHexAnchorMath
    {
        public static Vector3 RotateLocalContent(Vector3 localPosition, int orientationIndex)
        {
            int normalizedIndex = ((orientationIndex % 6) + 6) % 6;
            Quaternion rotation = Quaternion.Euler(0f, normalizedIndex * 60f, 0f);
            return rotation * localPosition;
        }

        public static Vector3 ToPlacedContentPosition(Vector3 slotWorldCenter, Vector3 localPosition, int orientationIndex, bool supportsRotation)
        {
            Vector3 orientedLocal = supportsRotation ? RotateLocalContent(localPosition, orientationIndex) : localPosition;
            return slotWorldCenter + orientedLocal;
        }

        public static Vector3[] GetFlatTopCorners(Vector3 center, float outerRadiusMeters)
        {
            Vector3[] corners = new Vector3[6];

            for (int i = 0; i < corners.Length; i++)
            {
                float radians = Mathf.Deg2Rad * (60f * i);
                corners[i] = center + new Vector3(Mathf.Cos(radians) * outerRadiusMeters, 0f, Mathf.Sin(radians) * outerRadiusMeters);
            }

            return corners;
        }

        public static Vector3[] GetEdgeMidpoints(IReadOnlyList<Vector3> corners)
        {
            Vector3[] edgeMidpoints = new Vector3[6];

            for (int i = 0; i < edgeMidpoints.Length; i++)
            {
                edgeMidpoints[i] = (corners[i] + corners[(i + 1) % 6]) * 0.5f;
            }

            return edgeMidpoints;
        }

        public static Vector3[] GetInnerPoints(Vector3 center, IReadOnlyList<Vector3> corners)
        {
            Vector3[] innerPoints = new Vector3[6];

            for (int i = 0; i < innerPoints.Length; i++)
            {
                innerPoints[i] = Vector3.Lerp(center, corners[i], 0.5f);
            }

            return innerPoints;
        }
    }
}
