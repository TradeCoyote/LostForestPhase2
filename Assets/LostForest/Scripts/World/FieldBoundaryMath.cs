using UnityEngine;

namespace LostForest.Phase2.World
{
    public static class FieldBoundaryMath
    {
        private const float SqrtThree = 1.7320508f;

        public static Vector2Int WorldToNearestAxial(Vector3 worldPosition, float hexOuterRadiusMeters)
        {
            float radius = Mathf.Max(1f, hexOuterRadiusMeters);
            float q = ((2f / 3f) * worldPosition.x) / radius;
            float r = (((-1f / 3f) * worldPosition.x) + ((SqrtThree / 3f) * worldPosition.z)) / radius;
            return RoundAxial(q, r);
        }

        public static bool TryResolvePlayableSlot(
            FieldData fieldData,
            float hexOuterRadiusMeters,
            Vector3 worldPosition,
            out FieldSlotData slot)
        {
            slot = null;

            if (fieldData == null || fieldData.SlotsFilled == 0)
            {
                return false;
            }

            Vector2Int axial = WorldToNearestAxial(worldPosition, hexOuterRadiusMeters);
            Vector2Int offset = HexFrameMath.AxialToOffset(axial);
            slot = fieldData.GetSlot(offset.x, offset.y);

            if (slot == null)
            {
                return false;
            }

            return IsWorldPositionInsideFlatTopHex(worldPosition, slot.WorldCenter, hexOuterRadiusMeters, 0.25f);
        }

        public static int GetRingDepthAtWorldPosition(
            FieldData fieldData,
            float hexOuterRadiusMeters,
            Vector3 worldPosition,
            out Vector2Int nearestAxial,
            out FieldSlotData nearestPlayableSlot)
        {
            nearestAxial = WorldToNearestAxial(worldPosition, hexOuterRadiusMeters);
            return GetRingDepthFromPlayableField(fieldData, nearestAxial, out nearestPlayableSlot);
        }

        public static int GetRingDepthFromPlayableField(
            FieldData fieldData,
            Vector2Int axial,
            out FieldSlotData nearestPlayableSlot)
        {
            nearestPlayableSlot = null;

            if (fieldData == null || fieldData.SlotsFilled == 0)
            {
                return -1;
            }

            Vector2Int offset = HexFrameMath.AxialToOffset(axial);
            FieldSlotData exactSlot = fieldData.GetSlot(offset.x, offset.y);

            if (exactSlot != null)
            {
                nearestPlayableSlot = exactSlot;
                return 0;
            }

            int nearestDistance = int.MaxValue;

            for (int i = 0; i < fieldData.Slots.Count; i++)
            {
                FieldSlotData fieldSlot = fieldData.Slots[i];

                if (fieldSlot == null)
                {
                    continue;
                }

                int distance = HexFrameMath.GetHexDistance(axial, fieldSlot.AxialCoordinate);

                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearestPlayableSlot = fieldSlot;
            }

            return nearestDistance == int.MaxValue ? -1 : nearestDistance;
        }

        public static bool IsWorldPositionInsideFlatTopHex(
            Vector3 worldPosition,
            Vector3 hexCenter,
            float hexOuterRadiusMeters,
            float toleranceMeters = 0f)
        {
            float outerRadius = Mathf.Max(1f, hexOuterRadiusMeters);
            float innerRadius = outerRadius * 0.8660254f;
            float tolerance = Mathf.Max(0f, toleranceMeters);
            float qx = Mathf.Abs(worldPosition.x - hexCenter.x);
            float qz = Mathf.Abs(worldPosition.z - hexCenter.z);

            if (qx > outerRadius + tolerance || qz > innerRadius + tolerance)
            {
                return false;
            }

            float hexTest = (innerRadius * outerRadius) - (innerRadius * qx) - (outerRadius * 0.5f * qz);
            return hexTest >= -(tolerance * innerRadius);
        }

        private static Vector2Int RoundAxial(float q, float r)
        {
            float x = q;
            float z = r;
            float y = -x - z;
            int roundedX = Mathf.RoundToInt(x);
            int roundedY = Mathf.RoundToInt(y);
            int roundedZ = Mathf.RoundToInt(z);
            float xDiff = Mathf.Abs(roundedX - x);
            float yDiff = Mathf.Abs(roundedY - y);
            float zDiff = Mathf.Abs(roundedZ - z);

            if (xDiff > yDiff && xDiff > zDiff)
            {
                roundedX = -roundedY - roundedZ;
            }
            else if (yDiff > zDiff)
            {
                roundedY = -roundedX - roundedZ;
            }
            else
            {
                roundedZ = -roundedX - roundedY;
            }

            return new Vector2Int(roundedX, roundedZ);
        }
    }
}
