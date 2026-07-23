using LostForest.Phase2.Runes;
using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Landmarks
{
    public static class HomeStoneLandmarkRenderer
    {
        private const float DefaultStoneEmbedMeters = 0.08f;

        public static bool SpawnHomeStones(
            Transform parent,
            TerrainSlotData homeSlot,
            TerrainSurfaceSampler surfaceSampler,
            Material stoneMaterial,
            float stoneEmbedMeters,
            out int groundedStoneCount,
            out int skippedStoneCount,
            RuneManager runeManager = null)
        {
            groundedStoneCount = 0;
            skippedStoneCount = 0;

            if (parent == null || homeSlot == null)
            {
                return false;
            }

            Vector3 anchorWorldPosition = GetGroundedSlotCenter(homeSlot, surfaceSampler);
            GameObject landmarkRoot = new GameObject("Home Standing Stone Landmark");
            landmarkRoot.transform.position = anchorWorldPosition;
            landmarkRoot.transform.rotation = Quaternion.identity;
            landmarkRoot.transform.localScale = Vector3.one;
            landmarkRoot.transform.SetParent(parent, true);

            float embedMeters = Mathf.Clamp(stoneEmbedMeters, 0f, 0.25f);
            if (embedMeters <= 0f)
            {
                embedMeters = DefaultStoneEmbedMeters;
            }

            CreateStone(
                landmarkRoot.transform,
                "Home Black Cylinder 01 Tall",
                anchorWorldPosition,
                new Vector3(0f, 0f, 3.33f),
                0.63f,
                6f,
                new Vector3(-1f, 4f, 0f),
                0,
                runeManager,
                stoneMaterial,
                surfaceSampler,
                embedMeters,
                ref groundedStoneCount,
                ref skippedStoneCount);

            CreateStone(
                landmarkRoot.transform,
                "Home Black Cylinder 02 West",
                anchorWorldPosition,
                new Vector3(-1.83f, 0f, 2.33f),
                0.52f,
                4.67f,
                new Vector3(0.75f, -10f, -1f),
                1,
                runeManager,
                stoneMaterial,
                surfaceSampler,
                embedMeters,
                ref groundedStoneCount,
                ref skippedStoneCount);

            CreateStone(
                landmarkRoot.transform,
                "Home Black Cylinder 03 East",
                anchorWorldPosition,
                new Vector3(1.83f, 0f, 2.5f),
                0.55f,
                5.17f,
                new Vector3(-0.75f, 12f, 1f),
                2,
                runeManager,
                stoneMaterial,
                surfaceSampler,
                embedMeters,
                ref groundedStoneCount,
                ref skippedStoneCount);

            return groundedStoneCount > 0;
        }

        private static void CreateStone(
            Transform parent,
            string name,
            Vector3 anchorWorldPosition,
            Vector3 localGroundPosition,
            float radius,
            float height,
            Vector3 localEulerAngles,
            int runeIndex,
            RuneManager runeManager,
            Material stoneMaterial,
            TerrainSurfaceSampler surfaceSampler,
            float embedMeters,
            ref int groundedStoneCount,
            ref int skippedStoneCount)
        {
            Vector3 samplePosition = anchorWorldPosition + new Vector3(localGroundPosition.x, 0f, localGroundPosition.z);

            if (surfaceSampler == null || !surfaceSampler.TrySample(samplePosition, out TerrainSurfaceSample surfaceSample))
            {
                skippedStoneCount++;
                return;
            }

            GameObject stoneRoot = new GameObject($"{name} Grounded Root");
            stoneRoot.transform.position = surfaceSample.Position - Vector3.up * embedMeters;
            stoneRoot.transform.rotation = Quaternion.identity;
            stoneRoot.transform.localScale = Vector3.one;
            stoneRoot.transform.SetParent(parent, true);

            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stone.name = name;
            stone.transform.SetParent(stoneRoot.transform, false);
            Quaternion localRotation = Quaternion.Euler(localEulerAngles);
            stone.transform.localPosition = localRotation * (Vector3.up * (height * 0.5f));
            stone.transform.localRotation = localRotation;
            stone.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

            Renderer renderer = stone.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = stoneMaterial;
            }

            CreateRuneSocketIfNeeded(
                stoneRoot.transform,
                name,
                anchorWorldPosition,
                samplePosition,
                radius,
                height,
                localRotation,
                runeIndex,
                runeManager);

            groundedStoneCount++;
        }

        private static void CreateRuneSocketIfNeeded(
            Transform stoneRoot,
            string stoneName,
            Vector3 anchorWorldPosition,
            Vector3 samplePosition,
            float stoneRadius,
            float stoneHeight,
            Quaternion stoneLocalRotation,
            int runeIndex,
            RuneManager runeManager)
        {
            if (runeManager == null)
            {
                return;
            }

            char runeLetter = runeManager.GetNeededRuneAt(runeIndex);

            if (!RuneId.IsValidRune(runeLetter))
            {
                return;
            }

            Vector3 stoneAxis = stoneLocalRotation * Vector3.up;
            Vector3 outwardDirection = samplePosition - anchorWorldPosition;
            outwardDirection.y = 0f;

            if (outwardDirection.sqrMagnitude <= 0.0001f)
            {
                outwardDirection = Vector3.forward;
            }

            outwardDirection = Vector3.ProjectOnPlane(outwardDirection.normalized, stoneAxis);

            if (outwardDirection.sqrMagnitude <= 0.0001f)
            {
                outwardDirection = Vector3.ProjectOnPlane(Vector3.forward, stoneAxis);
            }

            outwardDirection = outwardDirection.sqrMagnitude <= 0.0001f ? Vector3.right : outwardDirection.normalized;

            float socketLengthMeters = 1.16f;
            float socketHeightOnStone = Mathf.Clamp(stoneHeight * 0.42f, 1.7f, 2.65f);
            Vector3 socketWorldPosition = stoneRoot.position
                + stoneAxis.normalized * socketHeightOnStone
                + outwardDirection * ((stoneRadius * 0.52f) + (socketLengthMeters * 0.5f));

            HomeRuneSocket.CreatePrototypeSocket(
                stoneRoot,
                $"{stoneName} Rune Socket {runeLetter}",
                runeManager,
                runeLetter,
                runeIndex,
                socketWorldPosition,
                outwardDirection,
                runeManager.EmptySocketMaterial,
                runeManager.DepositedSocketMaterial,
                runeManager.HomeNeededLetterColor,
                runeManager.PlayerCamera);
        }

        private static Vector3 GetGroundedSlotCenter(TerrainSlotData slot, TerrainSurfaceSampler surfaceSampler)
        {
            Vector3 slotCenter = slot.CenterPoint == null ? slot.WorldCenter : slot.CenterPoint.Position;

            if (surfaceSampler != null && surfaceSampler.TrySample(slotCenter, out TerrainSurfaceSample surfaceSample))
            {
                return surfaceSample.Position;
            }

            return slotCenter;
        }
    }
}
