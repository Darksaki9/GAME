using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NutritionKitchen.Crafting.EditorTools
{
    public static class IngredientModelGenerator
    {
        private struct Part
        {
            public Mesh mesh;
            public Matrix4x4 matrix;
            public Color color;
        }

        private static readonly List<Part> parts = new List<Part>();
        private static readonly List<Mesh> tempMeshes = new List<Mesh>();

        private static Mesh sphereMesh;
        private static Mesh capsuleMesh;
        private static Mesh cylinderMesh;
        private static Mesh cubeMesh;

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Mesh m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go);
            return m;
        }

        private static void EnsurePrimitives()
        {
            if (sphereMesh == null) sphereMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
            if (capsuleMesh == null) capsuleMesh = GetPrimitiveMesh(PrimitiveType.Capsule);
            if (cylinderMesh == null) cylinderMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);
            if (cubeMesh == null) cubeMesh = GetPrimitiveMesh(PrimitiveType.Cube);
        }

        private static void Ellipsoid(Vector3 center, Vector3 size, Color color)
        {
            parts.Add(new Part
            {
                mesh = sphereMesh,
                matrix = Matrix4x4.TRS(center, Quaternion.identity, size),
                color = color
            });
        }

        private static void Capsule(Vector3 a, Vector3 b, float radius, Color color)
        {
            Vector3 dir = b - a;
            float len = dir.magnitude;
            if (len < 0.0001f) return;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir);
            Vector3 scale = new Vector3(radius * 2f, len * 0.5f, radius * 2f);
            parts.Add(new Part
            {
                mesh = capsuleMesh,
                matrix = Matrix4x4.TRS((a + b) * 0.5f, rot, scale),
                color = color
            });
        }

        private static void Cylinder(Vector3 center, float diameter, float height, Color color, Quaternion rot)
        {
            parts.Add(new Part
            {
                mesh = cylinderMesh,
                matrix = Matrix4x4.TRS(center, rot, new Vector3(diameter, height * 0.5f, diameter)),
                color = color
            });
        }

        private static void Box(Vector3 center, Vector3 size, Color color, Quaternion rot)
        {
            parts.Add(new Part
            {
                mesh = cubeMesh,
                matrix = Matrix4x4.TRS(center, rot, size),
                color = color
            });
        }

        // ---------------------------------------------------------------- Ingredient 3D Builders

        // 1. ข้าวสารและข้าวกล้อง (Rice Sack with Grains)
        private static void BuildRiceSack()
        {
            Color sack = new Color(0.82f, 0.70f, 0.50f);
            Color rope = new Color(0.55f, 0.38f, 0.22f);
            Color whiteRice = new Color(0.96f, 0.96f, 0.94f);
            Color brownRice = new Color(0.72f, 0.54f, 0.36f);
            Color label = new Color(0.92f, 0.88f, 0.80f);

            // Sack body (rounded burlap bag)
            Ellipsoid(new Vector3(0, 0.24f, 0), new Vector3(0.48f, 0.44f, 0.44f), sack);
            Ellipsoid(new Vector3(0, 0.40f, 0), new Vector3(0.44f, 0.30f, 0.40f), sack);
            // Tied neck rim
            Cylinder(new Vector3(0, 0.46f, 0), 0.38f, 0.04f, rope, Quaternion.identity);
            // Burlap folded collar top
            Cylinder(new Vector3(0, 0.50f, 0), 0.40f, 0.05f, sack, Quaternion.identity);

            // Mounded grains: White rice on left, Brown rice on right
            Ellipsoid(new Vector3(-0.07f, 0.51f, 0), new Vector3(0.24f, 0.12f, 0.26f), whiteRice);
            Ellipsoid(new Vector3(0.07f, 0.51f, 0), new Vector3(0.24f, 0.12f, 0.26f), brownRice);

            // Front label patch
            Box(new Vector3(0, 0.24f, 0.22f), new Vector3(0.22f, 0.16f, 0.02f), label, Quaternion.identity);
        }

        // 2. เนื้อไก่ (Chicken Meat: Plump Drumstick & Fillet)
        private static void BuildChickenMeat()
        {
            Color rawMeat = new Color(0.96f, 0.75f, 0.72f);
            Color meatDeep = new Color(0.92f, 0.65f, 0.62f);
            Color bone = new Color(0.96f, 0.95f, 0.90f);
            Color cuttingBoard = new Color(0.76f, 0.58f, 0.40f);

            // Wooden plate / tray
            Cylinder(new Vector3(0, 0.03f, 0), 0.54f, 0.05f, cuttingBoard, Quaternion.identity);

            // Drumstick bone
            Capsule(new Vector3(-0.16f, 0.15f, 0), new Vector3(0.18f, 0.24f, 0), 0.035f, bone);
            Ellipsoid(new Vector3(0.20f, 0.25f, -0.025f), Vector3.one * 0.065f, bone);
            Ellipsoid(new Vector3(0.20f, 0.25f, 0.025f), Vector3.one * 0.065f, bone);

            // Plump juicy drumstick meat
            Ellipsoid(new Vector3(-0.06f, 0.18f, 0), new Vector3(0.26f, 0.20f, 0.20f), rawMeat);
            Ellipsoid(new Vector3(-0.14f, 0.16f, 0), new Vector3(0.18f, 0.16f, 0.17f), meatDeep);
        }

        // 3. เนื้อปลา (Fish Meat: Salmon Steak Fillet)
        private static void BuildFishMeat()
        {
            Color salmonMeat = new Color(0.98f, 0.52f, 0.38f);
            Color skin = new Color(0.38f, 0.46f, 0.50f);
            Color fatStripe = new Color(0.98f, 0.92f, 0.90f);
            Color plate = new Color(0.92f, 0.92f, 0.94f);

            // Ceramic plate
            Cylinder(new Vector3(0, 0.02f, 0), 0.56f, 0.04f, plate, Quaternion.identity);

            // Salmon steak: outer skin crescent + rich orange core + fat lines
            Ellipsoid(new Vector3(0, 0.12f, 0), new Vector3(0.44f, 0.14f, 0.32f), skin);
            Ellipsoid(new Vector3(0, 0.13f, 0), new Vector3(0.40f, 0.14f, 0.28f), salmonMeat);

            // Marbling white lines across steak
            for (int i = -2; i <= 2; i++)
            {
                Box(new Vector3(i * 0.07f, 0.20f, 0), new Vector3(0.015f, 0.01f, 0.20f), fatStripe, Quaternion.Euler(0, i * 8f, 0));
            }
            // Center bone spot
            Ellipsoid(new Vector3(0, 0.20f, 0), Vector3.one * 0.05f, Color.white);
        }

        // 4. ไข่ไก่ (Chicken Egg: Egg & Half-Cracked with Yolk)
        private static void BuildChickenEgg()
        {
            Color eggShell = new Color(0.92f, 0.78f, 0.64f);
            Color eggWhite = new Color(0.95f, 0.95f, 0.92f);
            Color yolk = new Color(0.98f, 0.72f, 0.12f);
            Color cup = new Color(0.42f, 0.65f, 0.75f);

            // Ceramic egg cup stand
            Cylinder(new Vector3(-0.08f, 0.04f, 0), 0.28f, 0.06f, cup, Quaternion.identity);
            Cylinder(new Vector3(-0.08f, 0.12f, 0), 0.18f, 0.12f, cup, Quaternion.identity);

            // Whole egg sitting upright in cup
            Ellipsoid(new Vector3(-0.08f, 0.28f, 0), new Vector3(0.24f, 0.32f, 0.24f), eggShell);

            // Small cracked egg shell bowl next to it with egg white & golden yolk
            Ellipsoid(new Vector3(0.16f, 0.08f, 0), new Vector3(0.24f, 0.12f, 0.24f), eggShell);
            Ellipsoid(new Vector3(0.16f, 0.10f, 0), new Vector3(0.20f, 0.08f, 0.20f), eggWhite);
            Ellipsoid(new Vector3(0.16f, 0.12f, 0), new Vector3(0.11f, 0.09f, 0.11f), yolk);
        }

        // 5. เต้าหู้ (Soybean Tofu: Soft White Block on Mat)
        private static void BuildTofuBlock()
        {
            Color tofu = new Color(0.97f, 0.97f, 0.94f);
            Color board = new Color(0.78f, 0.62f, 0.44f);
            Color leaf = new Color(0.35f, 0.68f, 0.32f);

            // Bamboo / wooden board
            Box(new Vector3(0, 0.03f, 0), new Vector3(0.48f, 0.04f, 0.44f), board, Quaternion.identity);

            // Decorative green banana leaf garnish
            Ellipsoid(new Vector3(0, 0.055f, 0), new Vector3(0.42f, 0.015f, 0.38f), leaf);

            // Silky tofu block with beveled rounded look
            Box(new Vector3(0, 0.18f, 0), new Vector3(0.28f, 0.22f, 0.28f), tofu, Quaternion.identity);
            // Soft rounded top edges
            Ellipsoid(new Vector3(0, 0.28f, 0), new Vector3(0.26f, 0.04f, 0.26f), tofu);
        }

        // 6. ผักรวม (Mixed Vegetables: Broccoli, Carrot & Tomato)
        private static void BuildMixedVegetables()
        {
            Color broccoliFloret = new Color(0.26f, 0.58f, 0.26f);
            Color broccoliStalk = new Color(0.62f, 0.82f, 0.50f);
            Color carrotOrange = new Color(0.98f, 0.50f, 0.12f);
            Color carrotGreen = new Color(0.32f, 0.68f, 0.28f);
            Color tomatoRed = new Color(0.92f, 0.22f, 0.18f);
            Color basket = new Color(0.72f, 0.55f, 0.38f);

            // Small rustic woven basket
            Cylinder(new Vector3(0, 0.05f, 0), 0.52f, 0.08f, basket, Quaternion.identity);

            // 1. Broccoli: stalk + fluffy floret spheres
            Cylinder(new Vector3(-0.12f, 0.16f, 0.02f), 0.08f, 0.16f, broccoliStalk, Quaternion.identity);
            Ellipsoid(new Vector3(-0.12f, 0.26f, 0.02f), new Vector3(0.24f, 0.18f, 0.24f), broccoliFloret);
            Ellipsoid(new Vector3(-0.18f, 0.28f, -0.02f), Vector3.one * 0.12f, broccoliFloret);
            Ellipsoid(new Vector3(-0.06f, 0.28f, 0.06f), Vector3.one * 0.12f, broccoliFloret);

            // 2. Carrot: angled tapered body + green leafy top
            Capsule(new Vector3(0.04f, 0.12f, -0.10f), new Vector3(0.16f, 0.22f, 0.10f), 0.06f, carrotOrange);
            Ellipsoid(new Vector3(0.18f, 0.24f, 0.12f), Vector3.one * 0.07f, carrotGreen);

            // 3. Cherry Tomato: glossy red sphere with star calyx
            Ellipsoid(new Vector3(0.12f, 0.15f, -0.08f), Vector3.one * 0.14f, tomatoRed);
            Ellipsoid(new Vector3(0.12f, 0.22f, -0.08f), new Vector3(0.06f, 0.02f, 0.06f), carrotGreen);
        }

        // 7. น้ำมันพืช (Vegetable Oil: Bottle with Cap & Drop Label)
        private static void BuildVegetableOil()
        {
            Color oilGolden = new Color(0.96f, 0.82f, 0.25f);
            Color capGreen = new Color(0.24f, 0.62f, 0.30f);
            Color label = new Color(0.96f, 0.94f, 0.88f);
            Color dropIcon = new Color(0.98f, 0.70f, 0.15f);

            // Main bottle body
            Cylinder(new Vector3(0, 0.18f, 0), 0.26f, 0.28f, oilGolden, Quaternion.identity);
            // Rounded bottle shoulder
            Ellipsoid(new Vector3(0, 0.32f, 0), new Vector3(0.26f, 0.12f, 0.26f), oilGolden);
            // Bottle neck
            Cylinder(new Vector3(0, 0.40f, 0), 0.12f, 0.10f, oilGolden, Quaternion.identity);
            // Cap
            Cylinder(new Vector3(0, 0.46f, 0), 0.13f, 0.06f, capGreen, Quaternion.identity);

            // Bottle label with drop icon
            Cylinder(new Vector3(0, 0.18f, 0), 0.264f, 0.14f, label, Quaternion.identity);
            Ellipsoid(new Vector3(0, 0.18f, 0.134f), new Vector3(0.05f, 0.07f, 0.02f), dropIcon);
        }

        // 8. เครื่องปรุง (Seasonings: Shaker & Sauce Bottle)
        private static void BuildSeasoningSet()
        {
            Color shakerCeramic = new Color(0.92f, 0.92f, 0.94f);
            Color shakerTop = new Color(0.35f, 0.38f, 0.42f);
            Color soySauceBottle = new Color(0.20f, 0.16f, 0.14f);
            Color soyRedCap = new Color(0.88f, 0.24f, 0.20f);
            Color soyLabel = new Color(0.95f, 0.90f, 0.80f);

            // 1. Shaker (salt/sodium adjustment)
            Cylinder(new Vector3(-0.12f, 0.14f, 0), 0.18f, 0.22f, shakerCeramic, Quaternion.identity);
            Ellipsoid(new Vector3(-0.12f, 0.25f, 0), new Vector3(0.18f, 0.08f, 0.18f), shakerTop);

            // 2. Soy sauce bottle (seasoning flavor)
            Cylinder(new Vector3(0.12f, 0.16f, 0), 0.18f, 0.24f, soySauceBottle, Quaternion.identity);
            Ellipsoid(new Vector3(0.12f, 0.28f, 0), new Vector3(0.18f, 0.10f, 0.18f), soySauceBottle);
            Cylinder(new Vector3(0.12f, 0.34f, 0), 0.09f, 0.08f, soySauceBottle, Quaternion.identity);
            Cylinder(new Vector3(0.12f, 0.39f, 0), 0.10f, 0.06f, soyRedCap, Quaternion.identity);

            // Label on soy sauce bottle
            Cylinder(new Vector3(0.12f, 0.16f, 0), 0.184f, 0.12f, soyLabel, Quaternion.identity);
        }

        // 9. น้ำซุป (Soup Stock / Broth: Soup Pot with Golden Broth)
        private static void BuildSoupBroth()
        {
            Color potCeramic = new Color(0.38f, 0.55f, 0.65f);
            Color rim = new Color(0.88f, 0.78f, 0.68f);
            Color broth = new Color(0.95f, 0.78f, 0.28f);
            Color garnishGreen = new Color(0.32f, 0.68f, 0.30f);
            Color carrotGarnish = new Color(0.98f, 0.52f, 0.14f);

            // Cute two-handled soup bowl / pot
            Cylinder(new Vector3(0, 0.12f, 0), 0.44f, 0.20f, potCeramic, Quaternion.identity);
            Ellipsoid(new Vector3(0, 0.04f, 0), new Vector3(0.42f, 0.08f, 0.42f), potCeramic);
            // Pot rim
            Cylinder(new Vector3(0, 0.22f, 0), 0.46f, 0.03f, rim, Quaternion.identity);

            // Two cute side loop handles
            for (int i = -1; i <= 1; i += 2)
            {
                Ellipsoid(new Vector3(i * 0.24f, 0.16f, 0), new Vector3(0.08f, 0.05f, 0.08f), rim);
            }

            // Shimmering golden clear soup broth
            Cylinder(new Vector3(0, 0.19f, 0), 0.42f, 0.02f, broth, Quaternion.identity);

            // Floating scallion herbs and carrot slices
            Ellipsoid(new Vector3(-0.08f, 0.205f, 0.05f), new Vector3(0.06f, 0.01f, 0.06f), garnishGreen);
            Ellipsoid(new Vector3(0.06f, 0.205f, -0.06f), new Vector3(0.05f, 0.01f, 0.05f), garnishGreen);
            Ellipsoid(new Vector3(0.04f, 0.205f, 0.08f), new Vector3(0.07f, 0.01f, 0.07f), carrotGarnish);
        }

        // ---------------------------------------------------------------- Creation & Combining

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        public struct IngredientSpec
        {
            public string id;
            public string thaiName;
            public string engName;
            public string nutritionRole;
            public NutrientCategory category;
            public CookingRole cookingRole;
            public float calories;
            public float carbs;
            public float protein;
            public float fat;
            public float fiber;
            public float sodium;
            public Color themeColor;
            public System.Action buildAction;
        }

        private static readonly IngredientSpec[] specs = new IngredientSpec[]
        {
            new IngredientSpec
            {
                id = "ingredient_rice",
                thaiName = "ข้าวสารและข้าวกล้อง",
                engName = "White & Brown Rice",
                nutritionRole = "แหล่งคาร์โบไฮเดรตหลัก ให้พลังงานแก่ร่างกาย",
                category = NutrientCategory.Carbohydrate,
                cookingRole = CookingRole.StapleBase,
                calories = 130f,
                carbs = 28f,
                protein = 2.7f,
                fat = 0.3f,
                fiber = 1.8f,
                sodium = 2f,
                themeColor = new Color(0.85f, 0.72f, 0.52f),
                buildAction = BuildRiceSack
            },
            new IngredientSpec
            {
                id = "ingredient_chicken",
                thaiName = "เนื้อไก่",
                engName = "Chicken Meat",
                nutritionRole = "แหล่งโปรตีนคุณภาพสูง เสริมสร้างกล้ามเนื้อ",
                category = NutrientCategory.Protein,
                cookingRole = CookingRole.MainProtein,
                calories = 165f,
                carbs = 0f,
                protein = 31f,
                fat = 3.6f,
                fiber = 0f,
                sodium = 74f,
                themeColor = new Color(0.95f, 0.65f, 0.60f),
                buildAction = BuildChickenMeat
            },
            new IngredientSpec
            {
                id = "ingredient_fish",
                thaiName = "เนื้อปลา",
                engName = "Fish Meat",
                nutritionRole = "โปรตีนย่อยง่ายและกรดไขมันดี (Omega-3)",
                category = NutrientCategory.Protein,
                cookingRole = CookingRole.MainProtein,
                calories = 142f,
                carbs = 0f,
                protein = 24f,
                fat = 4.5f,
                fiber = 0f,
                sodium = 60f,
                themeColor = new Color(0.98f, 0.52f, 0.40f),
                buildAction = BuildFishMeat
            },
            new IngredientSpec
            {
                id = "ingredient_egg",
                thaiName = "ไข่ไก่",
                engName = "Chicken Egg",
                nutritionRole = "โปรตีนสมบูรณ์และสารอาหารหลายชนิด",
                category = NutrientCategory.Protein,
                cookingRole = CookingRole.MainProtein,
                calories = 72f,
                carbs = 0.4f,
                protein = 6.3f,
                fat = 4.8f,
                fiber = 0f,
                sodium = 71f,
                themeColor = new Color(0.98f, 0.75f, 0.20f),
                buildAction = BuildChickenEgg
            },
            new IngredientSpec
            {
                id = "ingredient_tofu",
                thaiName = "เต้าหู้",
                engName = "Tofu",
                nutritionRole = "โปรตีนจากถั่วเหลือง ไขมันอิ่มตัวต่ำ",
                category = NutrientCategory.Protein,
                cookingRole = CookingRole.MainProtein,
                calories = 76f,
                carbs = 1.9f,
                protein = 8.1f,
                fat = 4.8f,
                fiber = 0.3f,
                sodium = 7f,
                themeColor = new Color(0.95f, 0.95f, 0.90f),
                buildAction = BuildTofuBlock
            },
            new IngredientSpec
            {
                id = "ingredient_vegetables",
                thaiName = "ผักรวม",
                engName = "Mixed Vegetables",
                nutritionRole = "ใยอาหาร วิตามิน และแร่ธาตุ ช่วยระบบขับถ่าย",
                category = NutrientCategory.Vegetable,
                cookingRole = CookingRole.VegetableAddin,
                calories = 45f,
                carbs = 9f,
                protein = 2.4f,
                fat = 0.4f,
                fiber = 3.6f,
                sodium = 35f,
                themeColor = new Color(0.32f, 0.68f, 0.30f),
                buildAction = BuildMixedVegetables
            },
            new IngredientSpec
            {
                id = "ingredient_oil",
                thaiName = "น้ำมันพืช",
                engName = "Vegetable Oil",
                nutritionRole = "ใช้ในการผัดและทอด ให้พลังงานและกรดไขมัน",
                category = NutrientCategory.CookingFat,
                cookingRole = CookingRole.CookingFat,
                calories = 120f,
                carbs = 0f,
                protein = 0f,
                fat = 14f,
                fiber = 0f,
                sodium = 0f,
                themeColor = new Color(0.96f, 0.82f, 0.25f),
                buildAction = BuildVegetableOil
            },
            new IngredientSpec
            {
                id = "ingredient_seasoning",
                thaiName = "เครื่องปรุง",
                engName = "Seasoning / Condiments",
                nutritionRole = "เพิ่มรสชาติและปรับระดับโซเดียมในอาหาร",
                category = NutrientCategory.Seasoning,
                cookingRole = CookingRole.Seasoning,
                calories = 15f,
                carbs = 2.5f,
                protein = 1.0f,
                fat = 0f,
                fiber = 0f,
                sodium = 480f,
                themeColor = new Color(0.85f, 0.35f, 0.30f),
                buildAction = BuildSeasoningSet
            },
            new IngredientSpec
            {
                id = "ingredient_broth",
                thaiName = "น้ำซุป",
                engName = "Soup Stock / Broth",
                nutritionRole = "ใช้ในเมนูต้มและแกงจืด เพิ่มความกลมกล่อม",
                category = NutrientCategory.Broth,
                cookingRole = CookingRole.SoupStock,
                calories = 20f,
                carbs = 1.2f,
                protein = 2.0f,
                fat = 0.5f,
                fiber = 0f,
                sodium = 240f,
                themeColor = new Color(0.35f, 0.60f, 0.70f),
                buildAction = BuildSoupBroth
            }
        };

        private static GameObject CreateIngredient(IngredientSpec spec, Transform parent, Vector3 localPos)
        {
            EnsurePrimitives();
            parts.Clear();
            tempMeshes.Clear();

            spec.buildAction();

            // Group parts by color -> one submesh/material per color
            List<Color32> keys = new List<Color32>();
            Dictionary<Color32, List<CombineInstance>> groups = new Dictionary<Color32, List<CombineInstance>>();
            foreach (Part p in parts)
            {
                Color32 key = p.color;
                if (!groups.ContainsKey(key))
                {
                    groups[key] = new List<CombineInstance>();
                    keys.Add(key);
                }
                groups[key].Add(new CombineInstance { mesh = p.mesh, transform = p.matrix });
            }

            List<Mesh> groupMeshes = new List<Mesh>();
            List<CombineInstance> finalInstances = new List<CombineInstance>();
            foreach (Color32 key in keys)
            {
                Mesh gm = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                gm.CombineMeshes(groups[key].ToArray(), true, true);
                groupMeshes.Add(gm);
                finalInstances.Add(new CombineInstance { mesh = gm, transform = Matrix4x4.identity });
            }

            Mesh final = new Mesh { name = spec.id + "_Mesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            final.CombineMeshes(finalInstances.ToArray(), false, false);
            final.RecalculateBounds();
            foreach (Mesh gm in groupMeshes) Object.DestroyImmediate(gm);
            foreach (Mesh tm in tempMeshes) Object.DestroyImmediate(tm);

            EnsureFolder("Assets", "Meshes");
            EnsureFolder("Assets/Meshes", "Ingredients");
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Ingredients");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Ingredients");
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "Ingredients");

            string meshPath = "Assets/Meshes/Ingredients/" + spec.id + ".asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(final, meshPath);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material[] mats = new Material[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                Material m = new Material(shader) { name = spec.id + "_Mat" + i, color = (Color)keys[i] };
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.35f);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                string matPath = "Assets/Materials/Ingredients/" + spec.id + "_Mat" + i + ".mat";
                AssetDatabase.DeleteAsset(matPath);
                AssetDatabase.CreateAsset(m, matPath);
                mats[i] = m;
            }

            GameObject root = new GameObject(spec.id);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.AddComponent<MeshFilter>().sharedMesh = final;
            root.AddComponent<MeshRenderer>().sharedMaterials = mats;
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = final.bounds.center;
            box.size = final.bounds.size;

            string prefabPath = "Assets/Prefabs/Ingredients/" + spec.id + ".prefab";
            AssetDatabase.DeleteAsset(prefabPath);
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);

            // Create ScriptableObject Data Asset
            string dataPath = "Assets/Data/Ingredients/" + spec.id + "_Data.asset";
            IngredientData data = AssetDatabase.LoadAssetAtPath<IngredientData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<IngredientData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }
            data.ingredientId = spec.id;
            data.displayNameThai = spec.thaiName;
            data.displayNameEng = spec.engName;
            data.nutritionRoleThai = spec.nutritionRole;
            data.category = spec.category;
            data.cookingRole = spec.cookingRole;
            data.calories = spec.calories;
            data.carbohydrates = spec.carbs;
            data.protein = spec.protein;
            data.fat = spec.fat;
            data.fiber = spec.fiber;
            data.sodium = spec.sodium;
            data.themeColor = spec.themeColor;
            data.prefab = prefabAsset;

            EditorUtility.SetDirty(data);
            return root;
        }

        [MenuItem("Tools/Nutrition Kitchen/Generate All Ingredients", priority = 30)]
        public static void GenerateAllIngredients()
        {
            GameObject old;
            while ((old = GameObject.Find("Ingredient_Collection")) != null)
                Object.DestroyImmediate(old);

            GameObject group = new GameObject("Ingredient_Collection");
            Undo.RegisterCreatedObjectUndo(group, "Create Ingredient Collection");
            // Place neatly in front of the NPCs (z = -1.2m)
            group.transform.position = new Vector3(0, 0, -1.2f);

            for (int i = 0; i < specs.Length; i++)
            {
                // Line up horizontally centered
                Vector3 pos = new Vector3((i - (specs.Length - 1) * 0.5f) * 0.8f, 0, 0);
                CreateIngredient(specs[i], group.transform, pos);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = group;
            Debug.Log("[IngredientModelGenerator] Generated 9 3D ingredient models and ScriptableObjects in Assets/Prefabs/Ingredients and Assets/Data/Ingredients");
        }
    }
}
