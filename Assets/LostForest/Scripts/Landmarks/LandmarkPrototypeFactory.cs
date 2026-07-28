using LostForest.Phase2.World;
using UnityEngine;

namespace LostForest.Phase2.Landmarks
{
    public static class LandmarkPrototypeFactory
    {
        private static readonly float[] BirchBandHeights01 = { 0.14f, 0.25f, 0.37f, 0.5f, 0.62f, 0.74f, 0.86f };

        public readonly struct Materials
        {
            public Materials(
                Material whiteStone,
                Material veryLightStone,
                Material lightStone,
                Material mediumStone,
                Material darkStone,
                Material birchTrunk,
                Material birchBand,
                Material totem)
            {
                WhiteStone = whiteStone;
                VeryLightStone = veryLightStone;
                LightStone = lightStone;
                MediumStone = mediumStone;
                DarkStone = darkStone;
                BirchTrunk = birchTrunk;
                BirchBand = birchBand;
                Totem = totem;
            }

            public Material WhiteStone { get; }
            public Material VeryLightStone { get; }
            public Material LightStone { get; }
            public Material MediumStone { get; }
            public Material DarkStone { get; }
            public Material BirchTrunk { get; }
            public Material BirchBand { get; }
            public Material Totem { get; }
        }

        public static LandmarkInstance SpawnPrototype(
            Transform parent,
            LandmarkPlacementPlan plan,
            TerrainSurfaceSampler surfaceSampler,
            Materials materials)
        {
            if (parent == null || plan.Profile == null)
            {
                return null;
            }

            GameObject rootObject = new GameObject($"{plan.Profile.DisplayName} Landmark Slot {plan.SlotAddress}");
            rootObject.transform.SetParent(parent, false);
            rootObject.transform.position = plan.AnchorPosition;
            rootObject.transform.rotation = plan.Rotation;
            rootObject.transform.localScale = Vector3.one;

            LandmarkInstance instance = rootObject.AddComponent<LandmarkInstance>();
            instance.Initialize(plan);

            switch (plan.Type)
            {
                case LandmarkType.Well:
                    BuildWell(rootObject.transform, surfaceSampler, materials);
                    break;
                case LandmarkType.CairnSphere:
                    BuildCairn(rootObject.transform, LandmarkType.CairnSphere, materials);
                    break;
                case LandmarkType.CairnCube:
                    BuildCairn(rootObject.transform, LandmarkType.CairnCube, materials);
                    break;
                case LandmarkType.CairnPyramid:
                    BuildCairn(rootObject.transform, LandmarkType.CairnPyramid, materials);
                    break;
                case LandmarkType.CairnCylinder:
                    BuildCairn(rootObject.transform, LandmarkType.CairnCylinder, materials);
                    break;
                case LandmarkType.BirchTreeCircle:
                    BuildBirchTreeCircle(rootObject.transform, surfaceSampler, materials);
                    break;
                case LandmarkType.LowAltar:
                    BuildLowAltar(rootObject.transform, surfaceSampler, materials);
                    break;
                case LandmarkType.RockWhite:
                    BuildHalfBuriedRock(rootObject.transform, "Rock White", GetWhiteStone(materials), surfaceSampler);
                    break;
                case LandmarkType.RockVeryLightGray:
                    BuildHalfBuriedRock(rootObject.transform, "Rock Very Light Gray", GetVeryLightStone(materials), surfaceSampler);
                    break;
                case LandmarkType.RockLightGray:
                    BuildHalfBuriedRock(rootObject.transform, "Rock Light Gray", GetLightStone(materials), surfaceSampler);
                    break;
                case LandmarkType.RockMediumGray:
                    BuildHalfBuriedRock(rootObject.transform, "Rock Medium Gray", GetMediumStone(materials), surfaceSampler);
                    break;
                case LandmarkType.TwoFallenParallelBirches:
                    BuildTwoFallenParallelBirches(rootObject.transform, surfaceSampler, materials);
                    break;
                case LandmarkType.BirchStumpCircle:
                    BuildBirchStumpCircle(rootObject.transform, surfaceSampler, materials);
                    break;
                case LandmarkType.StoneHut:
                    BuildStoneHut(rootObject.transform, surfaceSampler, materials);
                    break;
                case LandmarkType.OneTotem:
                    BuildTotems(rootObject.transform, 1, surfaceSampler, materials);
                    break;
                case LandmarkType.TwoTotems:
                    BuildTotems(rootObject.transform, 2, surfaceSampler, materials);
                    break;
                case LandmarkType.ThreeTotems:
                    BuildTotems(rootObject.transform, 3, surfaceSampler, materials);
                    break;
                case LandmarkType.CrossedTrees:
                    BuildCrossedTrees(rootObject.transform, surfaceSampler, materials);
                    break;
                case LandmarkType.SmallRingOfStones:
                    BuildSmallRingOfStones(rootObject.transform, surfaceSampler, materials);
                    break;
                case LandmarkType.LargeRingOfStoneSpires:
                    BuildLargeRingOfStoneSpires(rootObject.transform, surfaceSampler, materials);
                    break;
            }

            return instance;
        }

        private static void BuildWell(Transform root, TerrainSurfaceSampler surfaceSampler, Materials materials)
        {
            Vector3 ground = GetGroundLocal(root, Vector3.zero, surfaceSampler, 0.14f);
            CreateCylinder(root, "Well Low Wide Stone Ring", ground + Vector3.up * 0.24f, 3f, 0.78f, Quaternion.identity, GetVeryLightStone(materials));
            CreateCylinder(root, "Well Dark Center", ground + Vector3.up * 0.66f, 2.05f, 0.1f, Quaternion.identity, GetDarkStone(materials));
        }

        private static void BuildCairn(Transform root, LandmarkType type, Materials materials)
        {
            Vector3 ground = Vector3.zero;
            Material stone = GetLightStone(materials);

            CreateSphere(root, "Cairn Bottom Stone", ground + Vector3.up * 1.05f, 1.05f, stone);
            CreateSphere(root, "Cairn Middle Stone", ground + Vector3.up * 2.75f, 0.8f, GetVeryLightStone(materials));

            float topBaseY = 3.55f;

            if (type == LandmarkType.CairnSphere)
            {
                CreateSphere(root, "Cairn Top Sphere", ground + Vector3.up * 4.08f, 0.52f, GetWhiteStone(materials));
            }
            else if (type == LandmarkType.CairnCube)
            {
                CreateCube(root, "Cairn Top Cube", ground + Vector3.up * (topBaseY + 0.38f), new Vector3(0.9f, 0.76f, 0.9f), GetWhiteStone(materials));
            }
            else if (type == LandmarkType.CairnPyramid)
            {
                CreatePyramid(root, "Cairn Top Pyramid", ground + Vector3.up * topBaseY, 1.1f, 0.95f, GetWhiteStone(materials), Quaternion.identity);
            }
            else
            {
                CreateCylinder(root, "Cairn Top Cylinder", ground + Vector3.up * (topBaseY + 0.38f), 0.43f, 0.76f, Quaternion.identity, GetWhiteStone(materials));
            }
        }

        private static void BuildBirchTreeCircle(Transform root, TerrainSurfaceSampler surfaceSampler, Materials materials)
        {
            const int treeCount = 6;
            const float ringRadius = 4.45f;

            for (int i = 0; i < treeCount; i++)
            {
                Vector3 local = GetCircleLocalPosition(i, treeCount, ringRadius);
                CreateUprightBirch(root, $"Birch Circle Tree {i:00}", local, 7.5f, 0.28f, surfaceSampler, materials);
            }
        }

        private static void BuildLowAltar(Transform root, TerrainSurfaceSampler surfaceSampler, Materials materials)
        {
            Vector3 ground = GetGroundLocal(root, Vector3.zero, surfaceSampler, 0.04f);
            CreateCube(root, "Low Altar Stone Slab", ground + Vector3.up * 0.35f, new Vector3(4.8f, 0.7f, 2.3f), GetMediumStone(materials));
            CreateCube(root, "Low Altar Pale Top", ground + Vector3.up * 0.74f, new Vector3(4.95f, 0.12f, 2.45f), GetVeryLightStone(materials));
        }

        private static void BuildHalfBuriedRock(Transform root, string name, Material material, TerrainSurfaceSampler surfaceSampler)
        {
            Vector3 ground = GetGroundLocal(root, Vector3.zero, surfaceSampler, 0f);
            CreateSphere(root, name, ground, 1.12f, material);
        }

        private static void BuildTwoFallenParallelBirches(Transform root, TerrainSurfaceSampler surfaceSampler, Materials materials)
        {
            CreateFallenBirch(root, "Fallen Parallel Birch 01", new Vector3(0f, 0f, -0.72f), 0f, 8.6f, 0.24f, 0f, surfaceSampler, materials);
            CreateFallenBirch(root, "Fallen Parallel Birch 02", new Vector3(0f, 0f, 0.72f), 0f, 8.6f, 0.24f, 0.08f, surfaceSampler, materials);
        }

        private static void BuildBirchStumpCircle(Transform root, TerrainSurfaceSampler surfaceSampler, Materials materials)
        {
            const int stumpCount = 6;
            const float ringRadius = 3.65f;

            for (int i = 0; i < stumpCount; i++)
            {
                Vector3 local = GetCircleLocalPosition(i, stumpCount, ringRadius);
                CreateUprightBirch(root, $"Birch Stump {i:00}", local, 1.35f, 0.34f, surfaceSampler, materials, 4);
            }
        }

        private static void BuildStoneHut(Transform root, TerrainSurfaceSampler surfaceSampler, Materials materials)
        {
            Vector3 ground = GetGroundLocal(root, Vector3.zero, surfaceSampler, 0.03f);
            CreateCube(root, "Stone Hut Body", ground + Vector3.up * 1.35f, new Vector3(4.1f, 2.7f, 3.5f), GetLightStone(materials));
            CreatePyramid(root, "Stone Hut Pyramid Roof", ground + Vector3.up * 2.72f, 4.7f, 1.9f, GetMediumStone(materials), Quaternion.identity);
            CreateCube(root, "Stone Hut Dark Door", ground + Vector3.up * 0.8f + Vector3.forward * 1.78f, new Vector3(0.85f, 1.4f, 0.1f), GetDarkStone(materials));
        }

        private static void BuildTotems(Transform root, int count, TerrainSurfaceSampler surfaceSampler, Materials materials)
        {
            float spacing = 1.3f;
            float start = (count - 1) * -0.5f * spacing;

            for (int i = 0; i < count; i++)
            {
                Vector3 local = new Vector3(start + (i * spacing), 0f, 0f);
                Vector3 ground = GetGroundLocal(root, local, surfaceSampler, 0.03f);
                CreateCylinder(root, $"Totem Post {i + 1}", ground + Vector3.up * 1.25f, 0.22f, 2.5f, Quaternion.identity, GetTotem(materials));
                CreateSphere(root, $"Totem Head {i + 1}", ground + Vector3.up * 2.85f, 0.47f, GetWhiteStone(materials));
            }
        }

        private static void BuildCrossedTrees(Transform root, TerrainSurfaceSampler surfaceSampler, Materials materials)
        {
            CreateFallenBirch(root, "Crossed Birch 01", Vector3.zero, 36f, 8.6f, 0.25f, 0f, surfaceSampler, materials);
            CreateFallenBirch(root, "Crossed Birch 02", Vector3.zero, -36f, 8.6f, 0.25f, 0.26f, surfaceSampler, materials);
        }

        private static void BuildSmallRingOfStones(Transform root, TerrainSurfaceSampler surfaceSampler, Materials materials)
        {
            const int stoneCount = 6;
            const float ringRadius = 2.85f;

            for (int i = 0; i < stoneCount; i++)
            {
                Vector3 local = GetCircleLocalPosition(i, stoneCount, ringRadius);
                Vector3 ground = GetGroundLocal(root, local, surfaceSampler, 0f);
                CreateSphere(root, $"Small Ring Stone {i:00}", ground, 0.48f, GetLightStone(materials));
            }
        }

        private static void BuildLargeRingOfStoneSpires(Transform root, TerrainSurfaceSampler surfaceSampler, Materials materials)
        {
            const int spireCount = 18;
            const float ringRadius = 6.35f;

            for (int i = 0; i < spireCount; i++)
            {
                Vector3 local = GetCircleLocalPosition(i, spireCount, ringRadius);
                Vector3 ground = GetGroundLocal(root, local, surfaceSampler, 0.02f);
                Quaternion localRotation = Quaternion.Euler(0f, (360f / spireCount) * i, 0f);
                CreatePyramid(root, $"Large Ring Stone Spire {i:00}", ground, 0.68f, 1.45f, GetLightStone(materials), localRotation);
            }
        }

        private static void CreateUprightBirch(
            Transform root,
            string name,
            Vector3 localPlanar,
            float height,
            float radius,
            TerrainSurfaceSampler surfaceSampler,
            Materials materials,
            int bandCount = -1)
        {
            Vector3 ground = GetGroundLocal(root, localPlanar, surfaceSampler, 0.03f);
            CreateCylinder(root, $"{name} Trunk", ground + Vector3.up * (height * 0.5f), radius, height, Quaternion.identity, GetBirchTrunk(materials));

            int resolvedBandCount = bandCount < 0 ? BirchBandHeights01.Length : Mathf.Clamp(bandCount, 0, BirchBandHeights01.Length);

            for (int i = 0; i < resolvedBandCount; i++)
            {
                float height01 = BirchBandHeights01[i];
                CreateCylinder(root, $"{name} Bark Band {i:00}", ground + Vector3.up * (height * height01), radius * 1.04f, 0.055f, Quaternion.identity, GetBirchBand(materials));
            }
        }

        private static void CreateFallenBirch(
            Transform root,
            string name,
            Vector3 localPlanar,
            float yawDegrees,
            float length,
            float radius,
            float verticalLift,
            TerrainSurfaceSampler surfaceSampler,
            Materials materials)
        {
            Vector3 ground = GetGroundLocal(root, localPlanar, surfaceSampler, 0.02f);
            Transform trunkRoot = new GameObject($"{name} Root").transform;
            trunkRoot.SetParent(root, false);
            trunkRoot.localPosition = ground + Vector3.up * (radius + verticalLift);
            trunkRoot.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            trunkRoot.localScale = Vector3.one;

            CreateCylinder(trunkRoot, $"{name} Trunk", Vector3.zero, radius, length, Quaternion.Euler(0f, 0f, 90f), GetBirchTrunk(materials));

            for (int i = 0; i < BirchBandHeights01.Length; i++)
            {
                float x = Mathf.Lerp(length * -0.42f, length * 0.42f, BirchBandHeights01[i]);
                CreateCylinder(trunkRoot, $"{name} Bark Band {i:00}", Vector3.right * x, radius * 1.04f, 0.06f, Quaternion.Euler(0f, 0f, 90f), GetBirchBand(materials));
            }
        }

        private static GameObject CreateCylinder(
            Transform parent,
            string name,
            Vector3 localPosition,
            float radius,
            float height,
            Quaternion localRotation,
            Material material)
        {
            GameObject cylinder = CreatePrimitive(PrimitiveType.Cylinder, name, parent, material);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localRotation = localRotation;
            cylinder.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            return cylinder;
        }

        private static GameObject CreateSphere(
            Transform parent,
            string name,
            Vector3 localPosition,
            float radius,
            Material material)
        {
            GameObject sphere = CreatePrimitive(PrimitiveType.Sphere, name, parent, material);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = Vector3.one * (radius * 2f);
            return sphere;
        }

        private static GameObject CreateCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 size,
            Material material)
        {
            GameObject cube = CreatePrimitive(PrimitiveType.Cube, name, parent, material);
            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = size;
            return cube;
        }

        private static GameObject CreatePyramid(
            Transform parent,
            string name,
            Vector3 localBasePosition,
            float baseSize,
            float height,
            Material material,
            Quaternion localRotation)
        {
            GameObject pyramid = new GameObject(name);
            pyramid.transform.SetParent(parent, false);
            pyramid.transform.localPosition = localBasePosition;
            pyramid.transform.localRotation = localRotation;
            pyramid.transform.localScale = Vector3.one;

            float halfBase = baseSize * 0.5f;
            Mesh mesh = new Mesh
            {
                name = $"{name} Mesh",
                vertices = new[]
                {
                    new Vector3(-halfBase, 0f, -halfBase),
                    new Vector3(halfBase, 0f, -halfBase),
                    new Vector3(halfBase, 0f, halfBase),
                    new Vector3(-halfBase, 0f, halfBase),
                    new Vector3(0f, height, 0f)
                },
                triangles = new[]
                {
                    0, 2, 1,
                    0, 3, 2,
                    0, 1, 4,
                    1, 2, 4,
                    2, 3, 4,
                    3, 0, 4
                }
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter meshFilter = pyramid.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = pyramid.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            return pyramid;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);

            Renderer renderer = primitive.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            RemoveCollider(primitive);
            return primitive;
        }

        private static Vector3 GetGroundLocal(Transform root, Vector3 localPlanar, TerrainSurfaceSampler surfaceSampler, float embedMeters)
        {
            Vector3 fallback = new Vector3(localPlanar.x, 0f, localPlanar.z);

            if (root == null || surfaceSampler == null)
            {
                return fallback;
            }

            Vector3 worldPlanar = root.TransformPoint(fallback);

            if (!surfaceSampler.TrySample(worldPlanar, out TerrainSurfaceSample sample))
            {
                return fallback;
            }

            return root.InverseTransformPoint(sample.Position - Vector3.up * Mathf.Max(0f, embedMeters));
        }

        private static Vector3 GetCircleLocalPosition(int index, int count, float radius)
        {
            float angle = Mathf.Deg2Rad * ((360f / Mathf.Max(1, count)) * index);
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private static Material GetWhiteStone(Materials materials)
        {
            return materials.WhiteStone != null ? materials.WhiteStone : GetLightStone(materials);
        }

        private static Material GetVeryLightStone(Materials materials)
        {
            return materials.VeryLightStone != null ? materials.VeryLightStone : GetLightStone(materials);
        }

        private static Material GetLightStone(Materials materials)
        {
            if (materials.LightStone != null)
            {
                return materials.LightStone;
            }

            return materials.MediumStone != null ? materials.MediumStone : materials.WhiteStone;
        }

        private static Material GetMediumStone(Materials materials)
        {
            return materials.MediumStone != null ? materials.MediumStone : GetLightStone(materials);
        }

        private static Material GetDarkStone(Materials materials)
        {
            return materials.DarkStone != null ? materials.DarkStone : GetMediumStone(materials);
        }

        private static Material GetBirchTrunk(Materials materials)
        {
            return materials.BirchTrunk != null ? materials.BirchTrunk : GetWhiteStone(materials);
        }

        private static Material GetBirchBand(Materials materials)
        {
            return materials.BirchBand != null ? materials.BirchBand : GetDarkStone(materials);
        }

        private static Material GetTotem(Materials materials)
        {
            return materials.Totem != null ? materials.Totem : GetMediumStone(materials);
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject == null ? null : gameObject.GetComponent<Collider>();

            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }
    }
}
