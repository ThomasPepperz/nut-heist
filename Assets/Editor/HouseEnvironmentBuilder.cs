#if UNITY_EDITOR
using System.Collections.Generic;
using NutHeist.Core;
using NutHeist.Environment;
using NutHeist.World;
using UnityEditor;
using UnityEngine;

namespace NutHeist.EditorTools
{
    /// <summary>
    /// Procedural author for the full Nut Heist world: yard, fence, four trees, garden,
    /// pool, shed, two-story brick house with 10 exterior entry points, six rooms +
    /// basement lab, and the branching ventilation network. All measurements are in
    /// Unity units (1 unit = 1 meter); squirrel is 0.25 units tall (see ScaleManager).
    ///
    /// Idempotent: rebuilding deletes the old `HouseWorld_Root` and regenerates fresh.
    /// </summary>
    public static class HouseEnvironmentBuilder
    {
        const string RootName = "HouseWorld_Root";

        [MenuItem("Nut Heist/Build House World", priority = 10)]
        public static void BuildMenu()
        {
            BuildAll();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Nut Heist",
                "House world rebuilt. Look for HouseWorld_Root in the Hierarchy.", "OK");
        }

        public static GameObject BuildAll()
        {
            // Tear down any prior generated world AND the original blockout stubs from
            // NutHeistBootstrap so we don't double up.
            RemoveByName(RootName);
            RemoveByName("Terrain_YardPlate");
            RemoveByName("House_Mass_Box");
            RemoveByName("Oak_BlockoutCylinder");

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "HouseWorld root");

            Palette.PrimeRuntimeMaterials();

            YardAuthor.Build(root.transform);
            FenceAuthor.Build(root.transform);
            TreeAuthor.Build(root.transform);
            GardenAuthor.Build(root.transform);
            PoolAuthor.Build(root.transform);
            ShedAuthor.Build(root.transform);

            HouseShellAuthor.HouseLayout house = HouseShellAuthor.Build(root.transform);
            HouseEntryPointsAuthor.Build(root.transform, house);
            HouseInteriorAuthor.Build(root.transform, house);
            VentNetworkAuthor.Build(root.transform, house);

            return root;
        }

        static void RemoveByName(string n)
        {
            GameObject found = GameObject.Find(n);
            if (found)
            {
                Object.DestroyImmediate(found);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Shared primitive + tagging helpers. Every authored object passes through
        // these so behavior (colliders, materials, tiers) stays consistent.
        // ─────────────────────────────────────────────────────────────────────────
        internal static class Prim
        {
            public static GameObject Box(Transform parent, string name, Vector3 center,
                Vector3 size, Color color, bool climbable = false)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = center;
                go.transform.localScale = size;
                Palette.Paint(go, color);
                if (climbable)
                {
                    TagClimbable(go);
                    go.AddComponent<ClimbableSurface>();
                }

                return go;
            }

            public static GameObject Cylinder(Transform parent, string name, Vector3 center,
                float diameter, float height, Color color, bool climbable = false)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = center;
                // Unity cylinder is 2 units tall by default → scale y by half the desired height
                go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
                Palette.Paint(go, color);
                if (climbable)
                {
                    TagClimbable(go);
                    go.AddComponent<ClimbableSurface>();
                }

                return go;
            }

            public static GameObject Sphere(Transform parent, string name, Vector3 center,
                float diameter, Color color)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = center;
                go.transform.localScale = Vector3.one * diameter;
                Palette.Paint(go, color);
                return go;
            }

            public static GameObject TriggerBox(Transform parent, string name,
                Vector3 center, Vector3 size)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = center;
                BoxCollider bc = go.AddComponent<BoxCollider>();
                bc.size = size;
                bc.isTrigger = true;
                return go;
            }

            public static void TagClimbable(GameObject go)
            {
                try
                {
                    go.tag = GameplayTags.Climbable;
                }
                catch
                {
                    // Tag asset not yet created; runtime ClimbableSurface enforces lazily.
                }
            }
        }

        internal static class Tier
        {
            public static Interactable Assign(GameObject go, params InteractionTier[] tiers)
            {
                Interactable inter = go.GetComponent<Interactable>();
                if (!inter)
                {
                    inter = go.AddComponent<Interactable>();
                }

                SerializedObject so = new SerializedObject(inter);
                SerializedProperty arr = so.FindProperty("tiers");
                arr.arraySize = tiers.Length;
                for (int i = 0; i < tiers.Length; i++)
                {
                    arr.GetArrayElementAtIndex(i).enumValueIndex = (int)tiers[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                return inter;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Material palette. Creates a small set of named, color-tinted URP materials
        // as project assets so primitives don't look grey and so they batch nicely.
        // ─────────────────────────────────────────────────────────────────────────
        internal static class Palette
        {
            const string FolderPath = "Assets/Materials/Generated";

            static readonly Dictionary<Color, Material> _cache = new Dictionary<Color, Material>();

            public static readonly Color SoilBrown = new Color(0.36f, 0.27f, 0.18f);
            public static readonly Color GrassGreen = new Color(0.32f, 0.55f, 0.20f);
            public static readonly Color BarkBrown = new Color(0.30f, 0.20f, 0.13f);
            public static readonly Color LeafGreen = new Color(0.18f, 0.42f, 0.18f);
            public static readonly Color WoodLight = new Color(0.68f, 0.50f, 0.30f);
            public static readonly Color WoodWeathered = new Color(0.55f, 0.42f, 0.30f);
            public static readonly Color BrickRed = new Color(0.55f, 0.25f, 0.20f);
            public static readonly Color MortarGrey = new Color(0.70f, 0.66f, 0.60f);
            public static readonly Color ConcreteGrey = new Color(0.55f, 0.55f, 0.55f);
            public static readonly Color SteelGrey = new Color(0.62f, 0.65f, 0.68f);
            public static readonly Color CopperHot = new Color(0.75f, 0.45f, 0.25f);
            public static readonly Color PoolBlue = new Color(0.18f, 0.55f, 0.78f);
            public static readonly Color GlassPale = new Color(0.78f, 0.86f, 0.92f);
            public static readonly Color RustOrange = new Color(0.62f, 0.32f, 0.20f);
            public static readonly Color LabPurple = new Color(0.42f, 0.30f, 0.55f);
            public static readonly Color SunflowerYellow = new Color(0.95f, 0.78f, 0.20f);
            public static readonly Color TomatoRed = new Color(0.78f, 0.20f, 0.15f);
            public static readonly Color CarrotOrange = new Color(0.92f, 0.50f, 0.15f);
            public static readonly Color RubberDuckYellow = new Color(0.98f, 0.85f, 0.20f);
            public static readonly Color WebGrey = new Color(0.85f, 0.85f, 0.85f);

            public static void PrimeRuntimeMaterials()
            {
                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }

                if (!AssetDatabase.IsValidFolder(FolderPath))
                {
                    AssetDatabase.CreateFolder("Assets/Materials", "Generated");
                }

                _cache.Clear();
            }

            public static void Paint(GameObject target, Color color)
            {
                MeshRenderer mr = target.GetComponent<MeshRenderer>();
                if (!mr)
                {
                    return;
                }

                mr.sharedMaterial = ResolveOrCreate(color);
            }

            static Material ResolveOrCreate(Color color)
            {
                if (_cache.TryGetValue(color, out Material cached) && cached)
                {
                    return cached;
                }

                string key = $"NutHeistMat_{Mathf.RoundToInt(color.r * 255f)}_" +
                             $"{Mathf.RoundToInt(color.g * 255f)}_" +
                             $"{Mathf.RoundToInt(color.b * 255f)}";
                string fullPath = $"{FolderPath}/{key}.mat";
                Material existing = AssetDatabase.LoadAssetAtPath<Material>(fullPath);
                if (existing)
                {
                    _cache[color] = existing;
                    return existing;
                }

                Shader litShader = Shader.Find("Universal Render Pipeline/Lit")
                                   ?? Shader.Find("Standard");
                Material mat = new Material(litShader);
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }

                if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", color);
                }

                AssetDatabase.CreateAsset(mat, fullPath);
                _cache[color] = mat;
                return mat;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section B.1: YARD plate (120 × 120) — replaces the original Terrain_YardPlate.
        // ─────────────────────────────────────────────────────────────────────────
        static class YardAuthor
        {
            public static void Build(Transform root)
            {
                GameObject yard = Prim.Box(root, "Terrain_YardPlate",
                    new Vector3(0f, -0.15f, 0f),
                    new Vector3(120f, 0.3f, 120f),
                    Palette.GrassGreen);
                Tier.Assign(yard, InteractionTier.PassivePhysics);

                // A subtle dirt patch under each tree-quadrant so navigation reads.
                Prim.Box(root, "Dirt_FrontLeft", new Vector3(-30f, 0.001f, 30f),
                    new Vector3(8f, 0.02f, 8f), Palette.SoilBrown);
                Prim.Box(root, "Dirt_BackRight", new Vector3(30f, 0.001f, -30f),
                    new Vector3(8f, 0.02f, 8f), Palette.SoilBrown);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section B.5: FENCE perimeter with squeeze-under gap on each side.
        // ─────────────────────────────────────────────────────────────────────────
        static class FenceAuthor
        {
            const float Half = 60f;
            const float PicketWidth = 0.6f;
            const float PicketSpacing = 0.8f;
            const float Height = ScaleManager.FENCE_HEIGHT; // 1.8

            public static void Build(Transform root)
            {
                GameObject fenceRoot = new GameObject("Fence_Perimeter");
                fenceRoot.transform.SetParent(root, false);

                BuildSide(fenceRoot.transform, "Fence_North", new Vector3(0f, 0f,  Half), Vector3.right);
                BuildSide(fenceRoot.transform, "Fence_South", new Vector3(0f, 0f, -Half), Vector3.right);
                BuildSide(fenceRoot.transform, "Fence_East",  new Vector3( Half, 0f, 0f), Vector3.forward);
                BuildSide(fenceRoot.transform, "Fence_West",  new Vector3(-Half, 0f, 0f), Vector3.forward);

                // Four corner posts — climbable, taller than the pickets.
                foreach (Vector3 corner in new[]
                         {
                             new Vector3( Half, 0f,  Half),
                             new Vector3( Half, 0f, -Half),
                             new Vector3(-Half, 0f,  Half),
                             new Vector3(-Half, 0f, -Half),
                         })
                {
                    GameObject post = Prim.Box(fenceRoot.transform, "FenceCornerPost",
                        new Vector3(corner.x, Height * 0.6f, corner.z),
                        new Vector3(1.2f, Height * 1.2f, 1.2f),
                        Palette.WoodWeathered, climbable: true);
                    Tier.Assign(post, InteractionTier.ClimbableTier);
                }
            }

            static void BuildSide(Transform parent, string name, Vector3 midpoint, Vector3 along)
            {
                GameObject side = new GameObject(name);
                side.transform.SetParent(parent, false);
                side.transform.localPosition = midpoint;

                int picketCount = Mathf.RoundToInt((Half * 2f - 2f) / PicketSpacing);
                int gapIndex = picketCount / 2; // squeeze-under gap roughly mid-span

                for (int i = 0; i < picketCount; i++)
                {
                    float t = (i - picketCount * 0.5f) * PicketSpacing;
                    Vector3 local = along * t;
                    float picketHeight = Height + (Mathf.PerlinNoise(i * 0.2f, 0f) - 0.5f) * 0.4f;
                    bool isGap = i >= gapIndex - 1 && i <= gapIndex + 1;
                    float yOffset = isGap ? 0.55f : picketHeight * 0.5f;
                    float scaleY = isGap ? picketHeight - 0.55f : picketHeight;

                    GameObject picket = Prim.Box(side.transform, $"Picket_{i:000}",
                        new Vector3(local.x, yOffset, local.z),
                        new Vector3(PicketWidth, scaleY, 0.08f),
                        Palette.WoodWeathered, climbable: true);

                    // Rotate east/west sides so pickets face the right way
                    if (along == Vector3.forward)
                    {
                        picket.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    }

                    Tier.Assign(picket, InteractionTier.ClimbableTier);
                }

                // Cap rail along the top: balance-beam traversal path.
                GameObject rail = Prim.Box(side.transform, "Fence_TopRail",
                    new Vector3(0f, Height + 0.05f, 0f),
                    along == Vector3.right
                        ? new Vector3(Half * 2f, 0.1f, 0.12f)
                        : new Vector3(0.12f, 0.1f, Half * 2f),
                    Palette.WoodLight, climbable: true);
                Tier.Assign(rail, InteractionTier.ClimbableTier);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section B.2: Four trees, each with trunk + branches + (optional) hollow.
        // ─────────────────────────────────────────────────────────────────────────
        static class TreeAuthor
        {
            public static void Build(Transform root)
            {
                GameObject treesRoot = new GameObject("Trees_Root");
                treesRoot.transform.SetParent(root, false);

                BuildClassicTree(treesRoot.transform, "OldOak_FrontLeft",
                    new Vector3(-28f, 0f, 28f),
                    trunkHeight: ScaleManager.TREE_HEIGHT_OAK, trunkDiameter: 4f,
                    branchHeights: new[] { 4f, 8f, 12f }, hasHollow: false);

                BuildClassicTree(treesRoot.transform, "Elm_BackRight",
                    new Vector3(28f, 0f, -28f),
                    trunkHeight: 12f, trunkDiameter: 3.2f,
                    branchHeights: new[] { 6f, 9f }, hasHollow: false);

                BuildClassicTree(treesRoot.transform, "GardenPine_BackLeft",
                    new Vector3(-30f, 0f, -25f),
                    trunkHeight: 10f, trunkDiameter: 2.6f,
                    branchHeights: new[] { 3f, 5f, 7f, 9f }, hasHollow: false,
                    crownColor: new Color(0.15f, 0.38f, 0.18f));

                BuildClassicTree(treesRoot.transform, "DeadOak_NearFence",
                    new Vector3(45f, 0f, 40f),
                    trunkHeight: 8f, trunkDiameter: 3f,
                    branchHeights: new[] { 4f, 6f }, hasHollow: true,
                    crownColor: Palette.BarkBrown);
            }

            static void BuildClassicTree(Transform parent, string name, Vector3 basePos,
                float trunkHeight, float trunkDiameter, float[] branchHeights,
                bool hasHollow, Color? crownColor = null)
            {
                GameObject tree = new GameObject(name);
                tree.transform.SetParent(parent, false);
                tree.transform.localPosition = basePos;

                GameObject trunk = Prim.Cylinder(tree.transform, "Trunk",
                    new Vector3(0f, trunkHeight * 0.5f, 0f),
                    trunkDiameter, trunkHeight, Palette.BarkBrown, climbable: true);
                Tier.Assign(trunk, InteractionTier.ClimbableTier);

                if (hasHollow)
                {
                    // Knothole entry — a darker recess at the base that VentTraversalVolume
                    // treats like a tight crawl tunnel. Squirrel actually fits inside.
                    GameObject knot = Prim.Cylinder(tree.transform, "HollowKnothole",
                        new Vector3(trunkDiameter * 0.45f, 0.6f, 0f),
                        0.5f, 1.2f, new Color(0.10f, 0.06f, 0.04f));
                    knot.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    knot.AddComponent<VentTraversalVolume>();
                    GameObject interior = Prim.TriggerBox(tree.transform, "HollowInterior",
                        new Vector3(0f, trunkHeight * 0.4f, 0f),
                        new Vector3(trunkDiameter * 0.5f, trunkHeight * 0.6f, trunkDiameter * 0.5f));
                    interior.AddComponent<VentTraversalVolume>();
                }

                for (int i = 0; i < branchHeights.Length; i++)
                {
                    float bh = branchHeights[i];
                    float length = trunkDiameter * 2.0f + 1.5f;
                    float yaw = (i * 90f + 30f) % 360f;
                    GameObject branch = Prim.Cylinder(tree.transform, $"Branch_{i}",
                        new Vector3(0f, bh, 0f),
                        0.45f, length, Palette.BarkBrown, climbable: true);
                    branch.transform.localRotation = Quaternion.Euler(0f, yaw, 80f);
                    branch.transform.localPosition += branch.transform.right * (length * 0.5f);
                    Tier.Assign(branch, InteractionTier.ClimbableTier);

                    if (crownColor.HasValue)
                    {
                        Prim.Sphere(tree.transform, $"Crown_{i}",
                            new Vector3(0f, bh + 0.6f, 0f) +
                            Quaternion.Euler(0f, yaw, 0f) * Vector3.right * length,
                            length * 0.9f, crownColor.Value);
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section B.3: Two raised garden beds with sunflowers, tomato lattice, carrots.
        // ─────────────────────────────────────────────────────────────────────────
        static class GardenAuthor
        {
            public static void Build(Transform root)
            {
                GameObject gardens = new GameObject("Gardens_Root");
                gardens.transform.SetParent(root, false);

                BuildBed(gardens.transform, "GardenBed_North", new Vector3(-15f, 0f, 25f));
                BuildBed(gardens.transform, "GardenBed_South", new Vector3(-15f, 0f, -25f));
            }

            static void BuildBed(Transform parent, string name, Vector3 origin)
            {
                GameObject bed = new GameObject(name);
                bed.transform.SetParent(parent, false);
                bed.transform.localPosition = origin;

                // Wooden border frame (4 planks), climbable.
                const float bedX = 6f, bedZ = 4f, wallH = 0.6f, wallT = 0.15f;
                foreach ((string n, Vector3 c, Vector3 s) in new[]
                         {
                             ("Border_N", new Vector3(0f, wallH * 0.5f,  bedZ * 0.5f), new Vector3(bedX, wallH, wallT)),
                             ("Border_S", new Vector3(0f, wallH * 0.5f, -bedZ * 0.5f), new Vector3(bedX, wallH, wallT)),
                             ("Border_E", new Vector3( bedX * 0.5f, wallH * 0.5f, 0f), new Vector3(wallT, wallH, bedZ)),
                             ("Border_W", new Vector3(-bedX * 0.5f, wallH * 0.5f, 0f), new Vector3(wallT, wallH, bedZ)),
                         })
                {
                    GameObject border = Prim.Box(bed.transform, n, c, s, Palette.WoodLight, climbable: true);
                    Tier.Assign(border, InteractionTier.ClimbableTier);
                }

                // Soil interior.
                Prim.Box(bed.transform, "Soil",
                    new Vector3(0f, 0.05f, 0f),
                    new Vector3(bedX - wallT * 2f, 0.1f, bedZ - wallT * 2f),
                    Palette.SoilBrown);

                // Sunflowers — 20-unit climbable towers per spec.
                for (int i = 0; i < 3; i++)
                {
                    float x = (i - 1) * 1.8f;
                    GameObject stalk = Prim.Cylinder(bed.transform, $"Sunflower_Stalk_{i}",
                        new Vector3(x, 10f, 1f), 0.18f, 20f, Palette.LeafGreen, climbable: true);
                    Tier.Assign(stalk, InteractionTier.ClimbableTier);
                    Prim.Sphere(bed.transform, $"Sunflower_Head_{i}",
                        new Vector3(x, 20.2f, 1f), 1.6f, Palette.SunflowerYellow);
                }

                // Tomato lattice — 3D climbable web (cross-braced cube frame).
                BuildTomatoLattice(bed.transform, new Vector3(0f, 0f, -1f), 2.5f, 3f, 1.5f);

                // Carrot tops sticking out of soil. Pushable physics objects.
                for (int i = 0; i < 4; i++)
                {
                    GameObject carrot = Prim.Cylinder(bed.transform, $"Carrot_{i}",
                        new Vector3(-2f + i * 1.2f, 0.3f, -1.5f),
                        0.18f, 0.5f, Palette.CarrotOrange);
                    Rigidbody rb = carrot.AddComponent<Rigidbody>();
                    rb.mass = 0.4f;
                    carrot.AddComponent<Pushable>();
                    Tier.Assign(carrot, InteractionTier.Pushable);
                }
            }

            static void BuildTomatoLattice(Transform parent, Vector3 center,
                float w, float h, float d)
            {
                GameObject lat = new GameObject("TomatoLattice");
                lat.transform.SetParent(parent, false);
                lat.transform.localPosition = center;

                Vector3[] corners =
                {
                    new Vector3(-w * 0.5f, 0f, -d * 0.5f),
                    new Vector3( w * 0.5f, 0f, -d * 0.5f),
                    new Vector3(-w * 0.5f, 0f,  d * 0.5f),
                    new Vector3( w * 0.5f, 0f,  d * 0.5f),
                };

                foreach (Vector3 corner in corners)
                {
                    GameObject post = Prim.Cylinder(lat.transform, "LatticePost",
                        corner + Vector3.up * h * 0.5f, 0.08f, h, Palette.WoodLight, climbable: true);
                    Tier.Assign(post, InteractionTier.ClimbableTier);
                }

                // Horizontal rungs at quarter heights.
                for (int row = 1; row <= 3; row++)
                {
                    float y = h * row / 4f;
                    GameObject rungX = Prim.Box(lat.transform, $"Rung_X_{row}",
                        new Vector3(0f, y, -d * 0.5f), new Vector3(w, 0.06f, 0.06f),
                        Palette.WoodLight, climbable: true);
                    Tier.Assign(rungX, InteractionTier.ClimbableTier);
                    GameObject rungZ = Prim.Box(lat.transform, $"Rung_Z_{row}",
                        new Vector3(w * 0.5f, y, 0f), new Vector3(0.06f, 0.06f, d),
                        Palette.WoodLight, climbable: true);
                    Tier.Assign(rungZ, InteractionTier.ClimbableTier);
                }

                // Decorative tomatoes (destructible — knock & shatter).
                for (int i = 0; i < 4; i++)
                {
                    GameObject tomato = Prim.Sphere(lat.transform, $"Tomato_{i}",
                        new Vector3(Random.Range(-w * 0.4f, w * 0.4f),
                            Random.Range(0.4f, h - 0.4f),
                            Random.Range(-d * 0.4f, d * 0.4f)),
                        0.35f, Palette.TomatoRed);
                    Rigidbody rb = tomato.AddComponent<Rigidbody>();
                    rb.mass = 0.3f;
                    tomato.AddComponent<Destructible>();
                    Tier.Assign(tomato, InteractionTier.Destructible, InteractionTier.PassivePhysics);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section B.4: POOL — swimmable volume + rim + drain (tunnel to basement).
        // ─────────────────────────────────────────────────────────────────────────
        static class PoolAuthor
        {
            public static void Build(Transform root)
            {
                GameObject pool = new GameObject("Pool_Root");
                pool.transform.SetParent(root, false);
                pool.transform.localPosition = new Vector3(8f, 0f, -4f);

                const float poolX = 16f, poolZ = 10f, poolDepth = 2f, rimW = 0.6f;

                // Pool basin — bottom slab + 4 inner walls (so squirrel can fall in).
                Prim.Box(pool.transform, "Pool_Bottom",
                    new Vector3(0f, -poolDepth - 0.1f, 0f),
                    new Vector3(poolX, 0.2f, poolZ),
                    Palette.PoolBlue);

                foreach ((string n, Vector3 c, Vector3 s) in new[]
                         {
                             ("Wall_N", new Vector3(0f, -poolDepth * 0.5f,  poolZ * 0.5f), new Vector3(poolX, poolDepth, 0.15f)),
                             ("Wall_S", new Vector3(0f, -poolDepth * 0.5f, -poolZ * 0.5f), new Vector3(poolX, poolDepth, 0.15f)),
                             ("Wall_E", new Vector3( poolX * 0.5f, -poolDepth * 0.5f, 0f), new Vector3(0.15f, poolDepth, poolZ)),
                             ("Wall_W", new Vector3(-poolX * 0.5f, -poolDepth * 0.5f, 0f), new Vector3(0.15f, poolDepth, poolZ)),
                         })
                {
                    Prim.Box(pool.transform, n, c, s, Palette.GlassPale);
                }

                // Rim walkway around the pool — climbable narrow ledge.
                foreach ((string n, Vector3 c, Vector3 s) in new[]
                         {
                             ("Rim_N", new Vector3(0f, 0.1f,  poolZ * 0.5f + rimW * 0.5f), new Vector3(poolX + rimW * 2f, 0.2f, rimW)),
                             ("Rim_S", new Vector3(0f, 0.1f, -poolZ * 0.5f - rimW * 0.5f), new Vector3(poolX + rimW * 2f, 0.2f, rimW)),
                             ("Rim_E", new Vector3( poolX * 0.5f + rimW * 0.5f, 0.1f, 0f), new Vector3(rimW, 0.2f, poolZ)),
                             ("Rim_W", new Vector3(-poolX * 0.5f - rimW * 0.5f, 0.1f, 0f), new Vector3(rimW, 0.2f, poolZ)),
                         })
                {
                    GameObject rim = Prim.Box(pool.transform, n, c, s, Palette.ConcreteGrey, climbable: true);
                    Tier.Assign(rim, InteractionTier.ClimbableTier);
                }

                // SwimmingVolume trigger that fills the basin.
                GameObject water = Prim.TriggerBox(pool.transform, "PoolWaterVolume",
                    new Vector3(0f, -poolDepth * 0.5f + 0.1f, 0f),
                    new Vector3(poolX - 0.3f, poolDepth + 0.2f, poolZ - 0.3f));
                water.AddComponent<SwimmingVolume>();

                // Floating pool toys — physics objects on the water plane.
                GameObject duck = Prim.Sphere(pool.transform, "RubberDuck",
                    new Vector3(2f, -0.2f, 1f), 0.7f, Palette.RubberDuckYellow);
                Rigidbody duckRb = duck.AddComponent<Rigidbody>();
                duckRb.mass = 0.6f;
                duck.AddComponent<Pushable>();
                Tier.Assign(duck, InteractionTier.Pushable, InteractionTier.ClimbableTier);

                GameObject noodle = Prim.Cylinder(pool.transform, "PoolNoodle",
                    new Vector3(-3f, -0.1f, -1f), 0.25f, 3.5f, new Color(0.95f, 0.55f, 0.85f), climbable: true);
                noodle.transform.localRotation = Quaternion.Euler(0f, 30f, 90f);
                Rigidbody noodleRb = noodle.AddComponent<Rigidbody>();
                noodleRb.mass = 0.4f;
                noodle.AddComponent<Pushable>();
                Tier.Assign(noodle, InteractionTier.Pushable, InteractionTier.ClimbableTier);

                // Pool drain in corner — entry to basement drainage pipe (tagged as vent).
                GameObject drain = Prim.Cylinder(pool.transform, "PoolDrain_BasementEntry",
                    new Vector3(poolX * 0.5f - 0.8f, -poolDepth - 0.05f, poolZ * 0.5f - 0.8f),
                    0.4f, 0.1f, Palette.SteelGrey);
                GameObject drainTrigger = Prim.TriggerBox(pool.transform, "DrainTunnelTrigger",
                    drain.transform.localPosition + Vector3.down * 0.5f,
                    new Vector3(0.45f, 1f, 0.45f));
                drainTrigger.AddComponent<VentTraversalVolume>();
                drainTrigger.AddComponent<VentAirflowVolume>();

                // Pool pump (decorative) — triggerable mechanical object.
                GameObject pump = Prim.Box(pool.transform, "PoolPump",
                    new Vector3(poolX * 0.5f + 1.5f, 0.5f, -poolZ * 0.5f - 1.5f),
                    new Vector3(1f, 1f, 1f),
                    Palette.SteelGrey);
                Tier.Assign(pump, InteractionTier.Triggerable, InteractionTier.PassivePhysics);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section B.6: GARDEN SHED with door gap + interior + tunnel to basement.
        // ─────────────────────────────────────────────────────────────────────────
        static class ShedAuthor
        {
            public static void Build(Transform root)
            {
                GameObject shed = new GameObject("Shed_Root");
                shed.transform.SetParent(root, false);
                shed.transform.localPosition = new Vector3(35f, 0f, -38f);

                const float w = 5f, d = 4f, h = 3f, wallT = 0.12f;

                // Walls — front wall has a door gap (a split with two pieces).
                Prim.Box(shed.transform, "ShedWall_Back",
                    new Vector3(0f, h * 0.5f, -d * 0.5f),
                    new Vector3(w, h, wallT), Palette.WoodWeathered, climbable: true);
                Prim.Box(shed.transform, "ShedWall_Left",
                    new Vector3(-w * 0.5f, h * 0.5f, 0f),
                    new Vector3(wallT, h, d), Palette.WoodWeathered, climbable: true);
                Prim.Box(shed.transform, "ShedWall_Right",
                    new Vector3(w * 0.5f, h * 0.5f, 0f),
                    new Vector3(wallT, h, d), Palette.WoodWeathered, climbable: true);

                // Front wall = two strips, with a 0.6-wide gap at the base for squirrel entry.
                Prim.Box(shed.transform, "ShedWall_Front_Lintel",
                    new Vector3(0f, h - 0.5f, d * 0.5f),
                    new Vector3(w, 1f, wallT), Palette.WoodWeathered, climbable: true);
                Prim.Box(shed.transform, "ShedWall_Front_Left",
                    new Vector3(-w * 0.25f - 0.3f, (h - 1f) * 0.5f, d * 0.5f),
                    new Vector3(w * 0.5f - 0.6f, h - 1f, wallT), Palette.WoodWeathered, climbable: true);
                Prim.Box(shed.transform, "ShedWall_Front_Right",
                    new Vector3(w * 0.25f + 0.3f, (h - 1f) * 0.5f, d * 0.5f),
                    new Vector3(w * 0.5f - 0.6f, h - 1f, wallT), Palette.WoodWeathered, climbable: true);

                Prim.Box(shed.transform, "ShedRoof",
                    new Vector3(0f, h + 0.1f, 0f),
                    new Vector3(w + 0.4f, 0.2f, d + 0.4f),
                    Palette.RustOrange);

                // Lawn mower obstacle — huge pushable physics box at squirrel scale.
                GameObject mower = Prim.Box(shed.transform, "LawnMower",
                    new Vector3(0f, 0.5f, 0f),
                    new Vector3(1.6f, 1f, 2.2f), Palette.SteelGrey);
                Rigidbody mowerRb = mower.AddComponent<Rigidbody>();
                mowerRb.mass = 80f; // immovable to squirrel
                mowerRb.constraints = RigidbodyConstraints.FreezeAll;
                Tier.Assign(mower, InteractionTier.PassivePhysics, InteractionTier.ClimbableTier);

                // Shelves with seed bags — climbable platforms.
                for (int i = 0; i < 3; i++)
                {
                    GameObject shelf = Prim.Box(shed.transform, $"ShedShelf_{i}",
                        new Vector3(-w * 0.4f, 0.8f + i * 0.7f, -d * 0.3f),
                        new Vector3(1.2f, 0.05f, 0.6f),
                        Palette.WoodLight, climbable: true);
                    Tier.Assign(shelf, InteractionTier.ClimbableTier);

                    GameObject seedBag = Prim.Box(shed.transform, $"SeedBag_{i}",
                        new Vector3(-w * 0.4f, 0.95f + i * 0.7f, -d * 0.3f),
                        new Vector3(0.5f, 0.25f, 0.35f),
                        new Color(0.75f, 0.66f, 0.45f));
                    Rigidbody bagRb = seedBag.AddComponent<Rigidbody>();
                    bagRb.mass = 1.2f;
                    seedBag.AddComponent<Pushable>();
                    Tier.Assign(seedBag, InteractionTier.Pushable);
                }

                // Hidden tunnel in shed floor → basement (long vent trigger).
                GameObject tunnel = Prim.TriggerBox(shed.transform, "ShedFloorTunnel_ToBasement",
                    new Vector3(w * 0.35f, -0.3f, -d * 0.35f),
                    new Vector3(0.5f, 0.6f, 0.5f));
                tunnel.AddComponent<VentTraversalVolume>();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section C + D: HOUSE shell. Two-story (16 unit tall) brick box,
        // 20×30 footprint, with basement -3 units deep. Returns the bounds so
        // entry-point + interior + vent builders can place children precisely.
        // ─────────────────────────────────────────────────────────────────────────
        internal static class HouseShellAuthor
        {
            public const float HouseW = 20f;
            public const float HouseD = 30f;
            public const float StoryH = 4f;
            public const float Stories = 2f;
            public const float BasementDepth = 3f;
            public const float WallT = 0.4f;
            public static readonly Vector3 HouseOrigin = new Vector3(20f, 0f, 10f);

            public struct HouseLayout
            {
                public Transform shell;
                public Vector3 origin;
                public float w, d, storyH, basementDepth, wallT;
            }

            public static HouseLayout Build(Transform root)
            {
                GameObject house = new GameObject("House_Shell");
                house.transform.SetParent(root, false);
                house.transform.localPosition = HouseOrigin;

                float totalH = StoryH * Stories;

                // Foundation slab + basement walls (sunken).
                Prim.Box(house.transform, "FoundationSlab",
                    new Vector3(0f, -BasementDepth - 0.15f, 0f),
                    new Vector3(HouseW + 0.4f, 0.3f, HouseD + 0.4f),
                    Palette.ConcreteGrey);

                BuildPerimeterWalls(house.transform, "Basement",
                    yMid: -BasementDepth * 0.5f, height: BasementDepth,
                    color: Palette.ConcreteGrey, brick: false);

                // First floor slab (with a stair cutout: omit a 2×3 hole near west wall).
                BuildFloorSlabWithCutout(house.transform, "Floor1_Slab",
                    yLevel: 0f,
                    cutoutLocal: new Vector3(-HouseW * 0.5f + 2.5f, 0f, -HouseD * 0.5f + 4f),
                    cutoutSize: new Vector2(3f, 2.5f),
                    color: Palette.WoodLight);

                BuildPerimeterWalls(house.transform, "Story1",
                    yMid: StoryH * 0.5f, height: StoryH,
                    color: Palette.BrickRed, brick: true);

                // Second floor slab (cutout for upper stair landing).
                BuildFloorSlabWithCutout(house.transform, "Floor2_Slab",
                    yLevel: StoryH,
                    cutoutLocal: new Vector3(-HouseW * 0.5f + 2.5f, 0f, -HouseD * 0.5f + 4f),
                    cutoutSize: new Vector2(3f, 2.5f),
                    color: Palette.WoodLight);

                BuildPerimeterWalls(house.transform, "Story2",
                    yMid: StoryH * 1.5f, height: StoryH,
                    color: Palette.BrickRed, brick: true);

                // Roof — pitched-ish single slab.
                Prim.Box(house.transform, "Roof",
                    new Vector3(0f, totalH + 0.5f, 0f),
                    new Vector3(HouseW + 1f, 1f, HouseD + 1f),
                    Palette.RustOrange);

                // Chimney on the roof (Section C.2 entry #6).
                GameObject chimney = Prim.Box(house.transform, "Chimney",
                    new Vector3(HouseW * 0.3f, totalH + 1.5f, HouseD * 0.2f),
                    new Vector3(1.2f, 2f, 1.2f),
                    Palette.BrickRed, climbable: true);
                Tier.Assign(chimney, InteractionTier.ClimbableTier);

                return new HouseLayout
                {
                    shell = house.transform,
                    origin = HouseOrigin,
                    w = HouseW,
                    d = HouseD,
                    storyH = StoryH,
                    basementDepth = BasementDepth,
                    wallT = WallT,
                };
            }

            static void BuildPerimeterWalls(Transform parent, string tag,
                float yMid, float height, Color color, bool brick)
            {
                Color tintMortar = brick ? Palette.MortarGrey : color;
                _ = tintMortar; // mortar lines hinted via spec; readable enough at primitive res.

                foreach ((string n, Vector3 c, Vector3 s) in new[]
                         {
                             ($"{tag}_Wall_N", new Vector3(0f, yMid,  HouseD * 0.5f), new Vector3(HouseW, height, WallT)),
                             ($"{tag}_Wall_S", new Vector3(0f, yMid, -HouseD * 0.5f), new Vector3(HouseW, height, WallT)),
                             ($"{tag}_Wall_E", new Vector3( HouseW * 0.5f, yMid, 0f), new Vector3(WallT, height, HouseD)),
                             ($"{tag}_Wall_W", new Vector3(-HouseW * 0.5f, yMid, 0f), new Vector3(WallT, height, HouseD)),
                         })
                {
                    GameObject w = Prim.Box(parent, n, c, s, color, climbable: brick);
                    if (brick)
                    {
                        Tier.Assign(w, InteractionTier.ClimbableTier);
                    }
                    else
                    {
                        Tier.Assign(w, InteractionTier.PassivePhysics);
                    }
                }
            }

            static void BuildFloorSlabWithCutout(Transform parent, string name, float yLevel,
                Vector3 cutoutLocal, Vector2 cutoutSize, Color color)
            {
                // Decompose the slab into 4 rectangles around the cutout.
                float halfW = HouseW * 0.5f;
                float halfD = HouseD * 0.5f;
                float cx = cutoutLocal.x;
                float cz = cutoutLocal.z;
                float cw = cutoutSize.x;
                float cd = cutoutSize.y;

                // West strip (left of cutout)
                Prim.Box(parent, $"{name}_W",
                    new Vector3((-halfW + (cx - cw * 0.5f)) * 0.5f, yLevel, 0f),
                    new Vector3((cx - cw * 0.5f) - (-halfW), 0.2f, HouseD),
                    color);
                // East strip (right of cutout)
                Prim.Box(parent, $"{name}_E",
                    new Vector3(((cx + cw * 0.5f) + halfW) * 0.5f, yLevel, 0f),
                    new Vector3(halfW - (cx + cw * 0.5f), 0.2f, HouseD),
                    color);
                // North strip (above cutout in z)
                Prim.Box(parent, $"{name}_N",
                    new Vector3(cx, yLevel, ((cz + cd * 0.5f) + halfD) * 0.5f),
                    new Vector3(cw, 0.2f, halfD - (cz + cd * 0.5f)),
                    color);
                // South strip (below cutout in z)
                Prim.Box(parent, $"{name}_S",
                    new Vector3(cx, yLevel, (-halfD + (cz - cd * 0.5f)) * 0.5f),
                    new Vector3(cw, 0.2f, (cz - cd * 0.5f) - (-halfD)),
                    color);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section C.2: ten unique exterior entry points. Each is a tagged interactive
        // child that funnels the squirrel into an interior VentTraversalVolume.
        // ─────────────────────────────────────────────────────────────────────────
        static class HouseEntryPointsAuthor
        {
            public static void Build(Transform root, HouseShellAuthor.HouseLayout house)
            {
                GameObject entries = new GameObject("HouseEntryPoints");
                entries.transform.SetParent(house.shell, false);

                // 1. Brick hole at foundation (back/south wall, ground level)
                MakeBrickHole(entries.transform, "Entry_BrickHole_Basement",
                    new Vector3(2f, 0.3f, -house.d * 0.5f - 0.05f));

                // 2. Dryer vent (east wall, 2 units up)
                MakeDryerVent(entries.transform, "Entry_DryerVent",
                    new Vector3(house.w * 0.5f + 0.05f, 2f, -4f));

                // 3. Basement window well (sunken below grade)
                MakeBasementWindow(entries.transform, "Entry_BasementWindow",
                    new Vector3(-house.w * 0.5f - 0.05f, -1.5f, 8f));

                // 4. Garage door gap (south wall, ground)
                MakeGarageGap(entries.transform, "Entry_GarageGap",
                    new Vector3(-5f, 0.15f, -house.d * 0.5f - 0.05f));

                // 5. Attic vent (high north wall, 14u up)
                MakeAtticVent(entries.transform, "Entry_AtticVent",
                    new Vector3(0f, house.storyH * 2f - 0.5f, house.d * 0.5f + 0.05f));

                // 6. Chimney drop already authored in the shell — add a trigger.
                GameObject chimneyDrop = Prim.TriggerBox(entries.transform, "Entry_ChimneyDrop",
                    new Vector3(house.w * 0.3f, house.storyH * 2f + 1.5f, house.d * 0.2f),
                    new Vector3(0.9f, 1f, 0.9f));
                chimneyDrop.AddComponent<VentTraversalVolume>();

                // 7. Cracked siding (second floor east wall)
                MakeCrackedSiding(entries.transform, "Entry_CrackedSiding",
                    new Vector3(house.w * 0.5f + 0.05f, house.storyH + 2f, 6f));

                // 8. Pet door (back/north wall, ground)
                MakePetDoor(entries.transform, "Entry_PetDoor",
                    new Vector3(4f, 0.4f, house.d * 0.5f + 0.05f));

                // 9. Broken screen window (first floor, west wall)
                MakeBrokenScreen(entries.transform, "Entry_BrokenScreen",
                    new Vector3(-house.w * 0.5f - 0.05f, 2.2f, -8f));

                // 10. Drain pipe on corner (climb up to roof gutter)
                MakeDrainPipe(entries.transform, "Entry_DrainPipe",
                    new Vector3(house.w * 0.5f + 0.3f, house.storyH, -house.d * 0.5f + 0.3f),
                    house.storyH * 2f);
            }

            static void MakeBrickHole(Transform parent, string n, Vector3 worldOffset)
            {
                GameObject hole = Prim.Box(parent, n, worldOffset,
                    new Vector3(0.6f, 0.4f, 0.5f), new Color(0.10f, 0.06f, 0.05f));
                Object.DestroyImmediate(hole.GetComponent<Collider>());
                BoxCollider trig = hole.AddComponent<BoxCollider>();
                trig.isTrigger = true;
                hole.AddComponent<VentTraversalVolume>();
                Tier.Assign(hole, InteractionTier.Triggerable);

                // A few loose brick props nearby — pushable physics.
                for (int i = 0; i < 3; i++)
                {
                    GameObject brick = Prim.Box(parent, $"LooseBrick_{i}",
                        worldOffset + new Vector3(0.5f + i * 0.25f, 0.05f, 0f),
                        new Vector3(0.19f, 0.057f, 0.09f),
                        Palette.BrickRed);
                    Rigidbody rb = brick.AddComponent<Rigidbody>();
                    rb.mass = 2f;
                    brick.AddComponent<Pushable>();
                    Tier.Assign(brick, InteractionTier.Pushable);
                }
            }

            static void MakeDryerVent(Transform parent, string n, Vector3 pos)
            {
                GameObject housing = Prim.Cylinder(parent, n + "_Housing", pos,
                    0.5f, 0.4f, Palette.SteelGrey);
                housing.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                GameObject grate = Prim.Cylinder(parent, n + "_Grate", pos,
                    0.45f, 0.05f, Palette.SteelGrey);
                grate.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Rigidbody grRb = grate.AddComponent<Rigidbody>();
                grRb.mass = 0.3f;
                HingeJoint hj = grate.AddComponent<HingeJoint>();
                hj.axis = Vector3.up;
                grate.AddComponent<Openable>();
                Tier.Assign(grate, InteractionTier.Openable);

                GameObject duct = Prim.TriggerBox(parent, n + "_Tube",
                    pos + new Vector3(-0.6f, 0f, 0f),
                    new Vector3(1.2f, 0.4f, 0.4f));
                duct.AddComponent<VentTraversalVolume>();
            }

            static void MakeBasementWindow(Transform parent, string n, Vector3 pos)
            {
                // Window well — sunken concrete trough.
                Prim.Box(parent, n + "_Well",
                    pos + new Vector3(-0.3f, -0.3f, 0f),
                    new Vector3(1f, 0.6f, 1.2f), Palette.ConcreteGrey, climbable: true);
                GameObject pane = Prim.Box(parent, n + "_Pane", pos,
                    new Vector3(0.05f, 0.8f, 1f), Palette.GlassPale);
                Rigidbody paneRb = pane.AddComponent<Rigidbody>();
                paneRb.mass = 0.5f;
                pane.AddComponent<Destructible>();
                Tier.Assign(pane, InteractionTier.Destructible);
                GameObject trigger = Prim.TriggerBox(parent, n + "_Tube",
                    pos + new Vector3(0.4f, 0f, 0f),
                    new Vector3(0.8f, 0.7f, 0.9f));
                trigger.AddComponent<VentTraversalVolume>();
            }

            static void MakeGarageGap(Transform parent, string n, Vector3 pos)
            {
                Prim.Box(parent, n + "_Lintel",
                    pos + new Vector3(0f, 1.5f, 0f),
                    new Vector3(4f, 3f, 0.1f), Palette.SteelGrey);
                GameObject gap = Prim.TriggerBox(parent, n + "_Tube",
                    pos + new Vector3(0f, 0f, 0.1f),
                    new Vector3(4f, 0.3f, 0.5f));
                gap.AddComponent<VentTraversalVolume>();
                gap.AddComponent<VentSpiderWebZone>(); // tight squeeze = slows squirrel
            }

            static void MakeAtticVent(Transform parent, string n, Vector3 pos)
            {
                GameObject louvers = Prim.Box(parent, n + "_Louvers", pos,
                    new Vector3(1.4f, 0.9f, 0.1f), Palette.SteelGrey);
                Rigidbody rb = louvers.AddComponent<Rigidbody>();
                rb.mass = 1f;
                louvers.AddComponent<Destructible>();
                Tier.Assign(louvers, InteractionTier.Destructible, InteractionTier.Openable);

                GameObject trigger = Prim.TriggerBox(parent, n + "_Tube",
                    pos + new Vector3(0f, 0f, -0.4f),
                    new Vector3(1.2f, 0.7f, 0.6f));
                trigger.AddComponent<VentTraversalVolume>();
                trigger.AddComponent<VentAirflowVolume>(); // attic gets pulled in by airflow
            }

            static void MakeCrackedSiding(Transform parent, string n, Vector3 pos)
            {
                for (int i = 0; i < 2; i++)
                {
                    GameObject plank = Prim.Box(parent, n + $"_Plank_{i}",
                        pos + new Vector3(0f, i * 0.3f - 0.15f, 0f),
                        new Vector3(0.05f, 0.25f, 0.6f), Palette.WoodLight);
                    Rigidbody rb = plank.AddComponent<Rigidbody>();
                    rb.mass = 1.5f;
                    plank.AddComponent<Pushable>();
                    Tier.Assign(plank, InteractionTier.Pushable, InteractionTier.Destructible);
                }

                GameObject trigger = Prim.TriggerBox(parent, n + "_Tube",
                    pos + new Vector3(-0.4f, 0f, 0f),
                    new Vector3(0.8f, 0.6f, 0.6f));
                trigger.AddComponent<VentTraversalVolume>();
            }

            static void MakePetDoor(Transform parent, string n, Vector3 pos)
            {
                GameObject frame = Prim.Box(parent, n + "_Frame", pos,
                    new Vector3(0.8f, 0.8f, 0.05f), Palette.WoodLight);
                _ = frame;
                GameObject flap = Prim.Box(parent, n + "_Flap",
                    pos + new Vector3(0f, 0.1f, 0f),
                    new Vector3(0.65f, 0.7f, 0.02f),
                    new Color(0.25f, 0.20f, 0.18f));
                Rigidbody flapRb = flap.AddComponent<Rigidbody>();
                flapRb.mass = 0.2f;
                HingeJoint hinge = flap.AddComponent<HingeJoint>();
                hinge.axis = Vector3.right;
                hinge.anchor = new Vector3(0f, 0.35f, 0f);
                flap.AddComponent<Openable>();
                Tier.Assign(flap, InteractionTier.Openable);

                GameObject trigger = Prim.TriggerBox(parent, n + "_Tube",
                    pos + new Vector3(0f, 0f, -0.4f),
                    new Vector3(0.6f, 0.6f, 0.6f));
                trigger.AddComponent<VentTraversalVolume>();
            }

            static void MakeBrokenScreen(Transform parent, string n, Vector3 pos)
            {
                Prim.Box(parent, n + "_Frame", pos,
                    new Vector3(0.05f, 1.2f, 1.4f), Palette.WoodLight);
                GameObject screen = Prim.Box(parent, n + "_TornMesh", pos,
                    new Vector3(0.02f, 1.0f, 1.2f), Palette.WebGrey);
                Rigidbody rb = screen.AddComponent<Rigidbody>();
                rb.mass = 0.1f;
                rb.isKinematic = true;
                screen.AddComponent<Destructible>();
                Tier.Assign(screen, InteractionTier.Destructible);

                GameObject trigger = Prim.TriggerBox(parent, n + "_Tube",
                    pos + new Vector3(0.4f, 0f, 0f),
                    new Vector3(0.7f, 0.9f, 0.9f));
                trigger.AddComponent<VentTraversalVolume>();
            }

            static void MakeDrainPipe(Transform parent, string n, Vector3 basePos, float length)
            {
                GameObject pipe = Prim.Cylinder(parent, n,
                    basePos + Vector3.up * length * 0.5f,
                    0.24f, length, Palette.SteelGrey, climbable: true);
                Tier.Assign(pipe, InteractionTier.ClimbableTier);

                // Gutter at top — short horizontal pipe segment leading inward to the attic.
                GameObject gutter = Prim.Cylinder(parent, n + "_Gutter",
                    basePos + Vector3.up * length + new Vector3(-0.6f, 0f, 0f),
                    0.24f, 1.5f, Palette.SteelGrey);
                gutter.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                GameObject trigger = Prim.TriggerBox(parent, n + "_Tube",
                    basePos + Vector3.up * length + new Vector3(-1.3f, 0f, 0f),
                    new Vector3(1f, 0.4f, 0.4f));
                trigger.AddComponent<VentTraversalVolume>();
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section D: HOUSE INTERIOR. Furniture / fixtures per room, scaled so a
        // kitchen counter is a 1m cliff to a 0.25m squirrel. Mad scientist lab on
        // floor 2 and primary lab in the basement.
        // ─────────────────────────────────────────────────────────────────────────
        static class HouseInteriorAuthor
        {
            public static void Build(Transform root, HouseShellAuthor.HouseLayout house)
            {
                GameObject inside = new GameObject("House_Interior");
                inside.transform.SetParent(house.shell, false);

                BuildKitchen(inside.transform, new Vector3(-6f, 0f, 10f));
                BuildLivingRoom(inside.transform, new Vector3(6f, 0f, 10f));
                BuildDiningRoom(inside.transform, new Vector3(0f, 0f, 2f));
                BuildLaundry(inside.transform, new Vector3(-6f, 0f, -6f));
                BuildGarage(inside.transform, new Vector3(6f, 0f, -10f));

                BuildMasterBedroom(inside.transform, new Vector3(-6f, house.storyH, 10f));
                BuildMadScientistStudy(inside.transform, new Vector3(0f, house.storyH, 2f));
                BuildBedroom2(inside.transform, new Vector3(6f, house.storyH, -8f));
                BuildBathroom2(inside.transform, new Vector3(6f, house.storyH, 4f));

                BuildBasementLab(inside.transform, new Vector3(0f, -house.basementDepth, 0f));

                BuildStairs(inside.transform, house);
            }

            static void BuildKitchen(Transform parent, Vector3 origin)
            {
                GameObject room = NewRoom(parent, "Kitchen", origin);

                // Counter cliff face (climb teaches grout grip).
                GameObject counter = Prim.Box(room.transform, "Counter",
                    new Vector3(0f, 0.5f, -1.5f),
                    new Vector3(4f, 1f, 1.5f), Palette.WoodLight, climbable: true);
                Tier.Assign(counter, InteractionTier.ClimbableTier);

                // Fridge — monolith.
                GameObject fridge = Prim.Box(room.transform, "Refrigerator",
                    new Vector3(-2f, 1.1f, 1.5f),
                    new Vector3(1f, 2.2f, 0.8f), new Color(0.85f, 0.85f, 0.88f), climbable: true);
                Tier.Assign(fridge, InteractionTier.ClimbableTier);

                // Trash can — open top trap pit with nut at the bottom.
                GameObject can = Prim.Cylinder(room.transform, "TrashCan",
                    new Vector3(2.5f, 0.4f, 1f), 0.6f, 0.8f, new Color(0.2f, 0.2f, 0.22f), climbable: true);
                Tier.Assign(can, InteractionTier.ClimbableTier);
                GameObject canInside = Prim.TriggerBox(room.transform, "TrashCan_Interior",
                    new Vector3(2.5f, 0.3f, 1f), new Vector3(0.45f, 0.5f, 0.45f));
                _ = canInside; // hook later for inside-trigger nut spawn

                // Knife block — hazard.
                GameObject knife = Prim.Box(room.transform, "KnifeBlock",
                    new Vector3(1f, 1.1f, -1.5f),
                    new Vector3(0.4f, 0.4f, 0.4f), new Color(0.15f, 0.15f, 0.15f));
                BoxCollider hazard = knife.AddComponent<BoxCollider>();
                hazard.size = new Vector3(0.5f, 0.6f, 0.5f);
                hazard.isTrigger = true;
                knife.AddComponent<SpikeHazard>();

                // Coffee maker (warm/steam top).
                Prim.Box(room.transform, "CoffeeMaker",
                    new Vector3(1.6f, 1.2f, -1.5f),
                    new Vector3(0.5f, 0.6f, 0.5f), Palette.SteelGrey);
            }

            static void BuildLivingRoom(Transform parent, Vector3 origin)
            {
                GameObject room = NewRoom(parent, "LivingRoom", origin);
                GameObject couch = Prim.Box(room.transform, "Couch",
                    new Vector3(0f, 0.5f, -2f),
                    new Vector3(4f, 1f, 1.5f), new Color(0.45f, 0.32f, 0.28f), climbable: true);
                Tier.Assign(couch, InteractionTier.ClimbableTier);

                // Bookshelf tower.
                for (int i = 0; i < 5; i++)
                {
                    GameObject shelf = Prim.Box(room.transform, $"Bookshelf_Shelf_{i}",
                        new Vector3(-3f, 0.4f + i * 0.7f, 2f),
                        new Vector3(2f, 0.05f, 0.7f), Palette.WoodLight, climbable: true);
                    Tier.Assign(shelf, InteractionTier.ClimbableTier);

                    for (int b = 0; b < 4; b++)
                    {
                        GameObject book = Prim.Box(room.transform, $"Book_{i}_{b}",
                            new Vector3(-3.7f + b * 0.45f, 0.55f + i * 0.7f, 2f),
                            new Vector3(0.2f, 0.25f, 0.5f),
                            new Color(Random.value, Random.value, Random.value));
                        Rigidbody rb = book.AddComponent<Rigidbody>();
                        rb.mass = 0.5f;
                        book.AddComponent<Pushable>();
                        Tier.Assign(book, InteractionTier.Pushable);
                    }
                }
            }

            static void BuildDiningRoom(Transform parent, Vector3 origin)
            {
                GameObject room = NewRoom(parent, "DiningRoom", origin);
                // Dining table with 4 legs (pillars at squirrel scale).
                Prim.Box(room.transform, "TableTop",
                    new Vector3(0f, 1.0f, 0f),
                    new Vector3(3f, 0.1f, 2f), Palette.WoodLight, climbable: true);
                foreach (Vector3 corner in new[]
                         {
                             new Vector3(-1.3f, 0.5f, -0.85f),
                             new Vector3( 1.3f, 0.5f, -0.85f),
                             new Vector3(-1.3f, 0.5f,  0.85f),
                             new Vector3( 1.3f, 0.5f,  0.85f),
                         })
                {
                    GameObject leg = Prim.Box(room.transform, "TableLeg",
                        corner, new Vector3(0.15f, 1f, 0.15f),
                        Palette.WoodLight, climbable: true);
                    Tier.Assign(leg, InteractionTier.ClimbableTier);
                }
            }

            static void BuildLaundry(Transform parent, Vector3 origin)
            {
                GameObject room = NewRoom(parent, "Laundry", origin);
                Prim.Box(room.transform, "WashingMachine",
                    new Vector3(0f, 0.5f, -1.5f),
                    new Vector3(0.9f, 1f, 0.8f), Palette.SteelGrey, climbable: true);
                Prim.Box(room.transform, "Dryer",
                    new Vector3(1f, 0.5f, -1.5f),
                    new Vector3(0.9f, 1f, 0.8f), Palette.SteelGrey, climbable: true);
            }

            static void BuildGarage(Transform parent, Vector3 origin)
            {
                GameObject room = NewRoom(parent, "Garage", origin);
                Prim.Box(room.transform, "ParkedCar",
                    new Vector3(0f, 0.6f, 0f),
                    new Vector3(2f, 1.2f, 4f), new Color(0.18f, 0.18f, 0.32f), climbable: true);

                // Toolbox — pushable.
                GameObject toolbox = Prim.Box(room.transform, "Toolbox",
                    new Vector3(-3f, 0.3f, 2f),
                    new Vector3(0.6f, 0.4f, 0.4f), Palette.SteelGrey);
                Rigidbody rb = toolbox.AddComponent<Rigidbody>();
                rb.mass = 3f;
                toolbox.AddComponent<Pushable>();
                Tier.Assign(toolbox, InteractionTier.Pushable);
            }

            static void BuildMasterBedroom(Transform parent, Vector3 origin)
            {
                GameObject room = NewRoom(parent, "MasterBedroom", origin);
                Prim.Box(room.transform, "Bed",
                    new Vector3(0f, 0.4f, -1.5f),
                    new Vector3(2.5f, 0.8f, 3f), new Color(0.55f, 0.45f, 0.55f), climbable: true);
                Prim.Box(room.transform, "Dresser",
                    new Vector3(-2.5f, 0.5f, 1f),
                    new Vector3(0.6f, 1f, 1.5f), Palette.WoodWeathered, climbable: true);
            }

            static void BuildMadScientistStudy(Transform parent, Vector3 origin)
            {
                GameObject room = NewRoom(parent, "MadScientistStudy", origin);

                // Tesla coil — glass column on a base, climbable.
                GameObject teslaBase = Prim.Cylinder(room.transform, "TeslaCoil_Base",
                    new Vector3(-3f, 0.3f, 1f), 0.8f, 0.6f, Palette.SteelGrey);
                _ = teslaBase;
                GameObject teslaColumn = Prim.Cylinder(room.transform, "TeslaCoil_Column",
                    new Vector3(-3f, 1.6f, 1f), 0.3f, 2f, Palette.GlassPale, climbable: true);
                Tier.Assign(teslaColumn, InteractionTier.ClimbableTier, InteractionTier.Triggerable);
                Prim.Sphere(room.transform, "TeslaCoil_Orb",
                    new Vector3(-3f, 2.8f, 1f), 0.6f, Palette.CopperHot);

                // Centrifuge — spinning platform challenge (kinematic rotation).
                GameObject centrifuge = Prim.Cylinder(room.transform, "Centrifuge",
                    new Vector3(-1f, 1.2f, 1f), 1.2f, 0.3f, Palette.LabPurple, climbable: true);
                Tier.Assign(centrifuge, InteractionTier.Triggerable, InteractionTier.ClimbableTier);

                // Computer bank — three monitors + keyboard.
                for (int i = 0; i < 3; i++)
                {
                    Prim.Box(room.transform, $"Monitor_{i}",
                        new Vector3(1f + i * 0.7f, 1.3f, -1.5f),
                        new Vector3(0.5f, 0.4f, 0.05f), Palette.GlassPale);
                }

                Prim.Box(room.transform, "Keyboard",
                    new Vector3(2f, 1.05f, -1.2f),
                    new Vector3(1.2f, 0.05f, 0.4f), new Color(0.15f, 0.15f, 0.15f), climbable: true);

                // Whiteboard.
                Prim.Box(room.transform, "Whiteboard",
                    new Vector3(0f, 1.5f, 1.9f),
                    new Vector3(2f, 1.2f, 0.05f), Color.white);

                // Filing cabinets — stacked drawer rungs.
                GameObject filing = new GameObject("FilingCabinet");
                filing.transform.SetParent(room.transform, false);
                filing.transform.localPosition = new Vector3(3f, 0f, 1.5f);
                for (int i = 0; i < 4; i++)
                {
                    GameObject drawer = Prim.Box(filing.transform, $"Drawer_{i}",
                        new Vector3((i % 2) * 0.1f, 0.3f + i * 0.4f, 0f),
                        new Vector3(0.6f, 0.35f, 0.7f), Palette.SteelGrey, climbable: true);
                    Tier.Assign(drawer, InteractionTier.ClimbableTier, InteractionTier.Pushable);
                }

                // Specimen jars — destructible.
                for (int i = 0; i < 5; i++)
                {
                    GameObject jar = Prim.Cylinder(room.transform, $"SpecimenJar_{i}",
                        new Vector3(-2.5f + i * 0.5f, 1.6f, -1.8f),
                        0.18f, 0.4f, Palette.GlassPale);
                    Rigidbody rb = jar.AddComponent<Rigidbody>();
                    rb.mass = 0.5f;
                    jar.AddComponent<Destructible>();
                    Tier.Assign(jar, InteractionTier.Destructible);
                }

                // Blueprint roll tubes — laid on the floor, squirrel-fittable.
                for (int i = 0; i < 3; i++)
                {
                    GameObject tube = Prim.Cylinder(room.transform, $"BlueprintTube_{i}",
                        new Vector3(0f, 0.18f, -2f + i * 0.4f), 0.35f, 1.8f, new Color(0.86f, 0.78f, 0.55f));
                    tube.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Tier.Assign(tube, InteractionTier.Triggerable);
                    GameObject inside = Prim.TriggerBox(room.transform, $"BlueprintTubeInside_{i}",
                        new Vector3(0f, 0.18f, -2f + i * 0.4f),
                        new Vector3(1.7f, 0.2f, 0.2f));
                    inside.AddComponent<VentTraversalVolume>();
                }

                // Pressure gauges (triggerable steam release → spring pad on activation).
                for (int i = 0; i < 2; i++)
                {
                    GameObject gauge = Prim.Sphere(room.transform, $"PressureGauge_{i}",
                        new Vector3(-3.5f, 1f + i * 0.6f, -1.5f), 0.25f, Palette.CopperHot);
                    BoxCollider trig = gauge.AddComponent<BoxCollider>();
                    trig.isTrigger = true;
                    trig.size = Vector3.one * 0.4f;
                    gauge.AddComponent<SpringJumpPad>();
                    Tier.Assign(gauge, InteractionTier.Triggerable);
                }
            }

            static void BuildBedroom2(Transform parent, Vector3 origin)
            {
                GameObject room = NewRoom(parent, "Bedroom2", origin);
                Prim.Box(room.transform, "Bed2",
                    new Vector3(0f, 0.3f, 0f),
                    new Vector3(2f, 0.6f, 2.8f), new Color(0.3f, 0.45f, 0.6f), climbable: true);
            }

            static void BuildBathroom2(Transform parent, Vector3 origin)
            {
                GameObject room = NewRoom(parent, "Bathroom2", origin);
                Prim.Box(room.transform, "Bathtub",
                    new Vector3(0f, 0.3f, 0f),
                    new Vector3(1.5f, 0.6f, 2.5f), Palette.GlassPale, climbable: true);
                GameObject water = Prim.TriggerBox(room.transform, "TubWater",
                    new Vector3(0f, 0.35f, 0f), new Vector3(1.3f, 0.5f, 2.3f));
                water.AddComponent<SwimmingVolume>();
            }

            static void BuildBasementLab(Transform parent, Vector3 origin)
            {
                GameObject lab = NewRoom(parent, "BasementLab", origin);

                // Workbenches.
                for (int i = 0; i < 3; i++)
                {
                    GameObject bench = Prim.Box(lab.transform, $"Workbench_{i}",
                        new Vector3(-5f + i * 3f, 0.5f, -3f),
                        new Vector3(2.5f, 1f, 1f), Palette.WoodWeathered, climbable: true);
                    Tier.Assign(bench, InteractionTier.ClimbableTier);
                }

                // Pegboard wall — entire wall is climbable.
                GameObject pegboard = Prim.Box(lab.transform, "PegboardWall",
                    new Vector3(0f, 1.5f, 3f),
                    new Vector3(8f, 2.5f, 0.05f),
                    new Color(0.65f, 0.50f, 0.35f), climbable: true);
                Tier.Assign(pegboard, InteractionTier.ClimbableTier);

                // Central contraption — scaffolding cube made of crossing beams.
                GameObject contraption = new GameObject("CentralContraption");
                contraption.transform.SetParent(lab.transform, false);
                contraption.transform.localPosition = new Vector3(0f, 0f, 0f);
                for (int row = 0; row < 4; row++)
                {
                    float y = 0.5f + row * 0.7f;
                    Prim.Box(contraption.transform, $"Scaffold_X_{row}",
                        new Vector3(0f, y, 0f),
                        new Vector3(2.5f, 0.08f, 0.08f), Palette.SteelGrey, climbable: true);
                    Prim.Box(contraption.transform, $"Scaffold_Z_{row}",
                        new Vector3(0f, y, 0f),
                        new Vector3(0.08f, 0.08f, 2.5f), Palette.SteelGrey, climbable: true);
                }

                // Trap prototype shelf — preview of future hazards (spike rows).
                for (int i = 0; i < 4; i++)
                {
                    GameObject trap = Prim.Box(lab.transform, $"TrapPrototype_{i}",
                        new Vector3(4f, 0.3f + i * 0.5f, 2f),
                        new Vector3(0.7f, 0.1f, 0.7f), Palette.RustOrange);
                    Tier.Assign(trap, InteractionTier.Triggerable, InteractionTier.PassivePhysics);
                }

                // Hidden room false-wall — pushable thick brick.
                GameObject falseWall = Prim.Box(lab.transform, "FalseWall_SecretRoom",
                    new Vector3(-7f, 1f, 0f),
                    new Vector3(0.3f, 2f, 2f), Palette.ConcreteGrey);
                Rigidbody rb = falseWall.AddComponent<Rigidbody>();
                rb.mass = 5f;
                falseWall.AddComponent<Pushable>();
                Tier.Assign(falseWall, InteractionTier.Pushable);
            }

            static void BuildStairs(Transform parent, HouseShellAuthor.HouseLayout house)
            {
                // Two flights of stairs in the stairwell cutout.
                GameObject stairs = new GameObject("Stairs");
                stairs.transform.SetParent(parent, false);
                stairs.transform.localPosition = new Vector3(-house.w * 0.5f + 2.5f, 0f, -house.d * 0.5f + 4f);

                BuildStairFlight(stairs.transform, "Flight_Basement", -house.basementDepth, 0f);
                BuildStairFlight(stairs.transform, "Flight_Floor1to2", 0f, house.storyH);
            }

            static void BuildStairFlight(Transform parent, string n, float yStart, float yEnd)
            {
                GameObject flight = new GameObject(n);
                flight.transform.SetParent(parent, false);

                const int steps = 8;
                float totalRise = yEnd - yStart;
                float stepRise = totalRise / steps;
                float runPerStep = 0.35f; // tiny, squirrel-friendly

                for (int i = 0; i < steps; i++)
                {
                    GameObject step = Prim.Box(flight.transform, $"Step_{i}",
                        new Vector3(0f, yStart + (i + 0.5f) * stepRise, i * runPerStep - 1f),
                        new Vector3(2.4f, stepRise, runPerStep),
                        Palette.WoodLight, climbable: true);
                    Tier.Assign(step, InteractionTier.ClimbableTier);
                }
            }

            static GameObject NewRoom(Transform parent, string name, Vector3 origin)
            {
                GameObject room = new GameObject($"Room_{name}");
                room.transform.SetParent(parent, false);
                room.transform.localPosition = origin;
                return room;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Section E: VENTILATION NETWORK. One attic trunk + vertical shaft +
        // first-floor + basement junctions, with airflow/slide/web zones along
        // the way and 18+ grates at ends.
        // ─────────────────────────────────────────────────────────────────────────
        static class VentNetworkAuthor
        {
            const float DuctDiameter = ScaleManager.VENT_DIAMETER; // 0.35

            public static void Build(Transform root, HouseShellAuthor.HouseLayout house)
            {
                GameObject vents = new GameObject("Vents_Network");
                vents.transform.SetParent(house.shell, false);

                float atticY = house.storyH * 2f - 0.2f;
                float floor1Y = house.storyH * 0.5f;
                float basementY = -house.basementDepth * 0.5f;

                // Attic main trunk — runs full length of house along z, beneath roof.
                Duct(vents.transform, "AtticTrunk",
                    new Vector3(0f, atticY, -house.d * 0.5f + 1f),
                    new Vector3(0f, atticY, house.d * 0.5f - 1f),
                    DuctDiameter);

                // Three branches off the trunk.
                Duct(vents.transform, "AtticBranch_Study",
                    new Vector3(0f, atticY, 2f),
                    new Vector3(0f, house.storyH + 1f, 2f),
                    DuctDiameter, addAirflow: true);
                AddGrate(vents.transform, "Grate_Study", new Vector3(0f, house.storyH + 0.4f, 2f));

                Duct(vents.transform, "AtticBranch_Master",
                    new Vector3(0f, atticY, 10f),
                    new Vector3(-6f, house.storyH + 1f, 10f),
                    DuctDiameter);
                AddGrate(vents.transform, "Grate_MasterBedroom",
                    new Vector3(-6f, house.storyH + 0.4f, 10f));

                Duct(vents.transform, "AtticBranch_Hallway",
                    new Vector3(0f, atticY, -4f),
                    new Vector3(0f, house.storyH + 1f, -4f),
                    DuctDiameter);
                AddGrate(vents.transform, "Grate_Hallway",
                    new Vector3(0f, house.storyH + 0.4f, -4f));

                // Vertical drop shaft from attic to basement.
                Duct(vents.transform, "VerticalShaft",
                    new Vector3(8f, atticY, -8f),
                    new Vector3(8f, basementY, -8f),
                    DuctDiameter, addSlide: true);

                // First floor junction.
                AddGrate(vents.transform, "Grate_Kitchen", new Vector3(-6f, 0.4f, 10f));
                AddGrate(vents.transform, "Grate_DiningRoom", new Vector3(0f, 0.4f, 2f));
                AddGrate(vents.transform, "Grate_LivingRoom", new Vector3(6f, 0.4f, 10f));
                AddGrate(vents.transform, "Grate_Garage", new Vector3(6f, 0.4f, -10f));
                AddGrate(vents.transform, "Grate_Laundry", new Vector3(-6f, 0.4f, -6f));

                Duct(vents.transform, "Floor1_Trunk",
                    new Vector3(-7f, floor1Y, -8f),
                    new Vector3(8f, floor1Y, -8f),
                    DuctDiameter, addWeb: true);

                // Basement junction — large industrial duct.
                Duct(vents.transform, "Basement_Trunk",
                    new Vector3(-8f, basementY, 0f),
                    new Vector3(8f, basementY, 0f),
                    DuctDiameter * 1.4f);
                AddGrate(vents.transform, "Grate_Lab",
                    new Vector3(0f, basementY + 0.4f, -3f));
                AddGrate(vents.transform, "Grate_Utility",
                    new Vector3(-7f, basementY + 0.4f, 0f));

                AddGrate(vents.transform, "Grate_Bathroom2",
                    new Vector3(6f, house.storyH + 0.4f, 4f));
                AddGrate(vents.transform, "Grate_Bedroom2",
                    new Vector3(6f, house.storyH + 0.4f, -8f));
                AddGrate(vents.transform, "Grate_Study2",
                    new Vector3(2f, house.storyH + 0.4f, 2f));
            }

            static void Duct(Transform parent, string name, Vector3 start, Vector3 end,
                float diameter, bool addAirflow = false, bool addSlide = false, bool addWeb = false)
            {
                Vector3 mid = (start + end) * 0.5f;
                Vector3 axis = end - start;
                float length = axis.magnitude;

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = name + "_Tube";
                visual.transform.SetParent(parent, false);
                visual.transform.localPosition = mid;
                visual.transform.up = axis.normalized;
                visual.transform.localScale = new Vector3(diameter, length * 0.5f, diameter);
                Palette.Paint(visual, Palette.SteelGrey);

                // Disable solid collider — squirrel needs to be *inside* the duct.
                Object.DestroyImmediate(visual.GetComponent<Collider>());

                // Inner traversal trigger covers the cylinder volume.
                GameObject trig = new GameObject(name + "_Inside");
                trig.transform.SetParent(parent, false);
                trig.transform.localPosition = mid;
                trig.transform.up = axis.normalized;
                BoxCollider bc = trig.AddComponent<BoxCollider>();
                bc.size = new Vector3(diameter * 0.85f, length, diameter * 0.85f);
                bc.isTrigger = true;
                trig.AddComponent<VentTraversalVolume>();

                if (addAirflow)
                {
                    trig.AddComponent<VentAirflowVolume>();
                }

                if (addSlide)
                {
                    trig.AddComponent<VentSlideZone>();
                }

                if (addWeb)
                {
                    trig.AddComponent<VentSpiderWebZone>();
                }
            }

            static void AddGrate(Transform parent, string name, Vector3 pos)
            {
                GameObject grate = Prim.Box(parent, name,
                    pos, new Vector3(0.4f, 0.05f, 0.4f), Palette.SteelGrey);
                Rigidbody rb = grate.AddComponent<Rigidbody>();
                rb.mass = 0.3f;
                rb.isKinematic = true;
                HingeJoint hj = grate.AddComponent<HingeJoint>();
                hj.axis = Vector3.forward;
                grate.AddComponent<Openable>();
                Tier.Assign(grate, InteractionTier.Openable);
            }
        }
    }
}
#endif
