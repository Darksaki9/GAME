using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutritionKitchen.Props
{
    [ExecuteInEditMode]
    public class DumpsterBuilder : MonoBehaviour
    {
        [Header("Colors (Matching Reference Photo)")]
        public Color dumpsterBlue = new Color(0.18f, 0.26f, 0.56f);       // Industrial blue body
        public Color darkMetal = new Color(0.15f, 0.16f, 0.18f);          // Steel rim, pockets, runners
        public Color lidPlastic = new Color(0.20f, 0.21f, 0.23f);         // Molded charcoal plastic lids
        public Color casterRubber = new Color(0.08f, 0.08f, 0.09f);       // Wheel rubber

        private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        [ContextMenu("Build Dumpster")]
        public void BuildDumpster()
        {
            materialCache.Clear();

            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

            Material blueMat = GetOrCreateMaterial("Mat_Dumpster_Blue", dumpsterBlue, 0.35f, 0.12f);
            Material darkMetalMat = GetOrCreateMaterial("Mat_Dumpster_DarkMetal", darkMetal, 0.45f, 0.65f);
            Material lidMat = GetOrCreateMaterial("Mat_Dumpster_LidPlastic", lidPlastic, 0.25f, 0.02f);
            Material wheelMat = GetOrCreateMaterial("Mat_Dumpster_Wheel", casterRubber, 0.15f, 0.05f);
            Material decalMat = GetOrCreateDecalMaterial("Mat_Dumpster_Decals");

            GameObject root = new GameObject("DumpsterModel");
            root.transform.SetParent(transform, false);

            // Dimensions:
            // Width: 2.15m, Depth: 1.25m, Front Height: 1.05m (total y=1.25), Back Height: 1.25m (total y=1.45)
            float halfW = 1.05f;
            float halfD = 0.60f;
            float baseY = 0.20f;
            float frontH = 1.05f; // y reaches 1.25
            float backH = 1.25f;  // y reaches 1.45

            // --- 1. MAIN HOPPER BODY ---
            BuildMainBody(root.transform, halfW, halfD, baseY, frontH, backH, blueMat);

            // --- 2. FRONT HORIZONTAL RIBS (3 ribs) ---
            BuildFrontRibs(root.transform, halfW, baseY, frontH, blueMat, darkMetalMat);

            // --- 3. PERIMETER TOP RIM ---
            BuildTopRim(root.transform, halfW, halfD, baseY, frontH, backH, darkMetalMat);

            // --- 4. SIDE FORK POCKETS (LIFT SLEEVES) ---
            BuildForkPockets(root.transform, halfW, darkMetalMat);

            // --- 5. DOUBLE CORRUGATED LIDS ---
            BuildDoubleLids(root.transform, halfW, halfD, baseY, frontH, backH, lidMat);

            // --- 6. CASTER WHEELS (4 CORNERS) ---
            BuildCasters(root.transform, halfW, halfD, baseY, darkMetalMat, wheelMat);

            // --- 7. DECALS (Signage matching reference) ---
            BuildDecalPlate(root.transform, halfW, halfD, baseY, frontH, decalMat);

            // --- 8. COLLIDER ---
            BoxCollider col = gameObject.GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, (baseY + backH) * 0.5f, 0);
            col.size = new Vector3(halfW * 2.3f, baseY + backH, halfD * 2.2f);

            Debug.Log("[DumpsterBuilder] Built commercial dumpster matching reference photo!");
        }

        private void BuildMainBody(Transform parent, float hw, float hd, float by, float fh, float bh, Material mat)
        {
            GameObject body = new GameObject("HopperBody");
            body.transform.SetParent(parent, false);

            // Slanted hopper mesh
            MeshFilter mf = body.AddComponent<MeshFilter>();
            MeshRenderer mr = body.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            Mesh mesh = new Mesh { name = "Dumpster_Hopper_Mesh" };

            // 8 outer vertices
            // Bottom 4: (-hw, by, -hd), (hw, by, -hd), (hw, by, hd), (-hw, by, hd)
            // Top 4:    (-hw, by+fh, -hd), (hw, by+fh, -hd), (hw, by+bh, hd), (-hw, by+bh, hd)
            Vector3[] verts = new Vector3[]
            {
                // Front face
                new Vector3(-hw, by, -hd), new Vector3(hw, by, -hd), new Vector3(hw, by + fh, -hd), new Vector3(-hw, by + fh, -hd),
                // Back face
                new Vector3(hw, by, hd), new Vector3(-hw, by, hd), new Vector3(-hw, by + bh, hd), new Vector3(hw, by + bh, hd),
                // Left face
                new Vector3(-hw, by, hd), new Vector3(-hw, by, -hd), new Vector3(-hw, by + fh, -hd), new Vector3(-hw, by + bh, hd),
                // Right face
                new Vector3(hw, by, -hd), new Vector3(hw, by, hd), new Vector3(hw, by + bh, hd), new Vector3(hw, by + fh, -hd),
                // Bottom face
                new Vector3(-hw, by, hd), new Vector3(hw, by, hd), new Vector3(hw, by, -hd), new Vector3(-hw, by, -hd),
                // Top face (interior floor or closed top rim base)
                new Vector3(-hw, by + fh, -hd), new Vector3(hw, by + fh, -hd), new Vector3(hw, by + bh, hd), new Vector3(-hw, by + bh, hd)
            };

            int[] tris = new int[]
            {
                0, 2, 1, 0, 3, 2,       // Front
                4, 6, 5, 4, 7, 6,       // Back
                8, 10, 9, 8, 11, 10,    // Left
                12, 14, 13, 12, 15, 14, // Right
                16, 18, 17, 16, 19, 18, // Bottom
                20, 21, 22, 20, 22, 23  // Top
            };

            Vector2[] uvs = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i += 4)
            {
                uvs[i] = new Vector2(0, 0);
                uvs[i + 1] = new Vector2(1, 0);
                uvs[i + 2] = new Vector2(1, 1);
                uvs[i + 3] = new Vector2(0, 1);
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
        }

        private void BuildFrontRibs(Transform parent, float hw, float by, float fh, Material blueMat, Material darkMat)
        {
            GameObject ribsRoot = new GameObject("FrontRibs");
            ribsRoot.transform.SetParent(parent, false);

            float[] ribHeights = new float[] { by + fh * 0.25f, by + fh * 0.50f, by + fh * 0.75f };
            float ribWidth = hw * 1.96f;
            float ribHeight = 0.055f;
            float ribDepth = 0.035f;

            for (int i = 0; i < ribHeights.Length; i++)
            {
                GameObject rib = CreateCube("Rib_" + (i + 1), ribsRoot.transform,
                    new Vector3(0, ribHeights[i], -0.60f - ribDepth * 0.5f),
                    Quaternion.identity,
                    new Vector3(ribWidth, ribHeight, ribDepth),
                    blueMat);
            }

            // Lower base bumper ledge (often dark or reinforced)
            CreateCube("BottomBumper", ribsRoot.transform,
                new Vector3(0, by + 0.03f, -0.60f - 0.025f),
                Quaternion.identity,
                new Vector3(hw * 2.02f, 0.06f, 0.05f),
                darkMat);
        }

        private void BuildTopRim(Transform parent, float hw, float hd, float by, float fh, float bh, Material mat)
        {
            GameObject rimRoot = new GameObject("TopPerimeterRim");
            rimRoot.transform.SetParent(parent, false);

            float rimSize = 0.07f;

            // Front rim bar
            CreateCube("Rim_Front", rimRoot.transform,
                new Vector3(0, by + fh + rimSize * 0.5f, -hd - rimSize * 0.3f),
                Quaternion.identity,
                new Vector3(hw * 2f + rimSize * 2f, rimSize, rimSize),
                mat);

            // Back rim bar
            CreateCube("Rim_Back", rimRoot.transform,
                new Vector3(0, by + bh + rimSize * 0.5f, hd + rimSize * 0.3f),
                Quaternion.identity,
                new Vector3(hw * 2f + rimSize * 2f, rimSize, rimSize),
                mat);

            // Left & Right slanted side rims
            float sideLen = Mathf.Sqrt(Mathf.Pow(hd * 2f, 2) + Mathf.Pow(bh - fh, 2));
            float sideAngle = Mathf.Atan2(bh - fh, hd * 2f) * Mathf.Rad2Deg;

            for (int s = -1; s <= 1; s += 2)
            {
                GameObject sideRim = CreateCube(s < 0 ? "Rim_Left" : "Rim_Right", rimRoot.transform,
                    new Vector3(s * (hw + rimSize * 0.3f), by + (fh + bh) * 0.5f + rimSize * 0.5f, 0),
                    Quaternion.Euler(-sideAngle, 0, 0),
                    new Vector3(rimSize, rimSize, sideLen),
                    mat);
            }
        }

        private void BuildForkPockets(Transform parent, float hw, Material mat)
        {
            GameObject pocketsRoot = new GameObject("ForkPockets");
            pocketsRoot.transform.SetParent(parent, false);

            float pocketLen = 1.15f;
            float pocketH = 0.18f;
            float pocketW = 0.08f;
            float pocketY = 0.82f;

            for (int s = -1; s <= 1; s += 2)
            {
                GameObject pocket = new GameObject(s < 0 ? "ForkPocket_L" : "ForkPocket_R");
                pocket.transform.SetParent(pocketsRoot.transform, false);
                pocket.transform.localPosition = new Vector3(s * (hw + pocketW * 0.5f), pocketY, 0);

                // Top & bottom plates forming the channel
                CreateCube("TopPlate", pocket.transform, new Vector3(0, pocketH * 0.5f, 0), Quaternion.identity, new Vector3(pocketW, 0.02f, pocketLen), mat);
                CreateCube("BottomPlate", pocket.transform, new Vector3(0, -pocketH * 0.5f, 0), Quaternion.identity, new Vector3(pocketW, 0.02f, pocketLen), mat);
                CreateCube("OuterWall", pocket.transform, new Vector3(s * pocketW * 0.5f, 0, 0), Quaternion.identity, new Vector3(0.02f, pocketH, pocketLen), mat);

                // Triangular angled entry gusset / skirt (flared receiver at the front)
                CreateCube("FrontFlare", pocket.transform,
                    new Vector3(s * 0.03f, -0.05f, -pocketLen * 0.5f - 0.05f),
                    Quaternion.Euler(0, s * -15f, -12f),
                    new Vector3(0.04f, pocketH * 1.2f, 0.12f),
                    mat);
            }
        }

        private void BuildDoubleLids(Transform parent, float hw, float hd, float by, float fh, float bh, Material mat)
        {
            GameObject lidsRoot = new GameObject("DoubleLids");
            lidsRoot.transform.SetParent(parent, false);

            float lidWidth = hw - 0.03f;
            float lidDepth = hd * 2f + 0.14f;
            float lidThickness = 0.045f;
            float slopeAngle = Mathf.Atan2(bh - fh, hd * 2f) * Mathf.Rad2Deg;

            // Two split lids (Left & Right)
            for (int i = 0; i < 2; i++)
            {
                float xCenter = (i == 0) ? -hw * 0.5f - 0.01f : hw * 0.5f + 0.01f;
                GameObject lid = new GameObject(i == 0 ? "Lid_Left" : "Lid_Right");
                lid.transform.SetParent(lidsRoot.transform, false);

                // Center position on slanted top
                lid.transform.localPosition = new Vector3(xCenter, by + (fh + bh) * 0.5f + 0.08f, -0.02f);
                lid.transform.localRotation = Quaternion.Euler(-slopeAngle, 0, 0);

                // Main lid flat slab
                CreateCube("LidSlab", lid.transform, Vector3.zero, Quaternion.identity, new Vector3(lidWidth, lidThickness, lidDepth), mat);

                // Longitudinal corrugation ribs (5 molded stiffening ridges per lid)
                int ribCount = 5;
                float ribSpacing = lidWidth / (ribCount + 1);
                for (int r = 1; r <= ribCount; r++)
                {
                    float rx = -lidWidth * 0.5f + r * ribSpacing;
                    CreateCube("Ridge_" + r, lid.transform,
                        new Vector3(rx, lidThickness * 0.5f + 0.02f, 0),
                        Quaternion.identity,
                        new Vector3(0.05f, 0.035f, lidDepth * 0.94f),
                        mat);
                }

                // Front overhanging handle lip
                CreateCube("HandleLip", lid.transform,
                    new Vector3(0, -lidThickness * 0.3f, -lidDepth * 0.5f - 0.02f),
                    Quaternion.identity,
                    new Vector3(lidWidth * 0.96f, 0.04f, 0.05f),
                    mat);
            }
        }

        private void BuildCasters(Transform parent, float hw, float hd, float by, float darkMat, Material wheelMat)
        {
            // Empty placeholder to be overwritten below
        }

        private void BuildCasters(Transform parent, float hw, float hd, float by, Material darkMat, Material wheelMat)
        {
            GameObject castersRoot = new GameObject("CasterWheels");
            castersRoot.transform.SetParent(parent, false);

            float cxOffset = hw * 0.82f;
            float czOffset = hd * 0.78f;
            float wheelRadius = 0.085f;
            float wheelWidth = 0.055f;

            Vector3[] positions = new Vector3[]
            {
                new Vector3(-cxOffset, 0, -czOffset), // Front Left
                new Vector3(cxOffset, 0, -czOffset),  // Front Right
                new Vector3(-cxOffset, 0, czOffset),  // Back Left
                new Vector3(cxOffset, 0, czOffset)   // Back Right
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject caster = new GameObject("Caster_" + (i + 1));
                caster.transform.SetParent(castersRoot.transform, false);
                caster.transform.localPosition = positions[i];

                // Top mount plate
                CreateCube("MountPlate", caster.transform,
                    new Vector3(0, by - 0.015f, 0),
                    Quaternion.identity,
                    new Vector3(0.18f, 0.03f, 0.18f),
                    darkMat);

                // Swivel yoke / fork
                CreateCube("SwivelStem", caster.transform,
                    new Vector3(0, by * 0.65f, 0),
                    Quaternion.identity,
                    new Vector3(0.08f, by * 0.45f, 0.08f),
                    darkMat);

                CreateCube("ForkLeft", caster.transform,
                    new Vector3(-wheelWidth * 0.6f, wheelRadius, 0),
                    Quaternion.identity,
                    new Vector3(0.015f, wheelRadius * 1.5f, 0.06f),
                    darkMat);

                CreateCube("ForkRight", caster.transform,
                    new Vector3(wheelWidth * 0.6f, wheelRadius, 0),
                    Quaternion.identity,
                    new Vector3(0.015f, wheelRadius * 1.5f, 0.06f),
                    darkMat);

                // Wheel cylinder
                GameObject wheel = CreatePrimitive(PrimitiveType.Cylinder, "Wheel", caster.transform,
                    new Vector3(0, wheelRadius, 0),
                    Quaternion.Euler(0, 0, 90f),
                    new Vector3(wheelRadius * 2f, wheelWidth * 0.5f, wheelRadius * 2f),
                    wheelMat);

                // Axle bolt cap
                CreatePrimitive(PrimitiveType.Cylinder, "AxleCap", caster.transform,
                    new Vector3(0, wheelRadius, 0),
                    Quaternion.Euler(0, 0, 90f),
                    new Vector3(0.035f, wheelWidth * 0.7f, 0.035f),
                    darkMat);
            }
        }

        private void BuildDecalPlate(Transform parent, float hw, float hd, float by, float fh, Material decalMat)
        {
            GameObject decalObj = new GameObject("FrontDecalPlate");
            decalObj.transform.SetParent(parent, false);

            // Positioned right on the upper front panel
            float decalY = by + fh * 0.82f;
            float decalZ = -hd - 0.005f;
            decalObj.transform.localPosition = new Vector3(0, decalY, decalZ);

            MeshFilter mf = decalObj.AddComponent<MeshFilter>();
            MeshRenderer mr = decalObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = decalMat;

            // Quad facing -Z
            Mesh mesh = new Mesh { name = "Decal_Quad" };
            float dw = hw * 1.85f;
            float dh = 0.28f;

            mesh.vertices = new Vector3[]
            {
                new Vector3(-dw * 0.5f, -dh * 0.5f, 0),
                new Vector3(dw * 0.5f, -dh * 0.5f, 0),
                new Vector3(dw * 0.5f, dh * 0.5f, 0),
                new Vector3(-dw * 0.5f, dh * 0.5f, 0)
            };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            mesh.normals = new Vector3[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
        }

        private GameObject CreateCube(string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 scale, Material mat)
        {
            return CreatePrimitive(PrimitiveType.Cube, name, parent, localPos, localRot, scale, mat);
        }

        private GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localRotation = localRot;
            obj.transform.localScale = scale;

            Collider c = obj.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;

            return obj;
        }

        private Material GetOrCreateMaterial(string matName, Color color, float smoothness, float metallic)
        {
            if (materialCache.TryGetValue(matName, out Material cached) && cached != null)
                return cached;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader) { name = matName };
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

#if UNITY_EDITOR
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Props");
            string matPath = "Assets/Materials/Props/" + matName + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
            {
                existing.color = color;
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
                if (existing.HasProperty("_Smoothness")) existing.SetFloat("_Smoothness", smoothness);
                if (existing.HasProperty("_Metallic")) existing.SetFloat("_Metallic", metallic);
                materialCache[matName] = existing;
                return existing;
            }
            AssetDatabase.CreateAsset(mat, matPath);
#endif
            materialCache[matName] = mat;
            return mat;
        }

        private Material GetOrCreateDecalMaterial(string matName)
        {
            if (materialCache.TryGetValue(matName, out Material cached) && cached != null)
                return cached;

            Texture2D tex = CreateDecalTexture();

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader) { name = matName };
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

#if UNITY_EDITOR
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Props");
            string matPath = "Assets/Materials/Props/" + matName + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
            {
                existing.mainTexture = tex;
                if (existing.HasProperty("_BaseMap")) existing.SetTexture("_BaseMap", tex);
                materialCache[matName] = existing;
                return existing;
            }
            AssetDatabase.CreateAsset(mat, matPath);
#endif
            materialCache[matName] = mat;
            return mat;
        }

        private Texture2D CreateDecalTexture()
        {
            const int w = 1024;
            const int h = 256;

#if UNITY_EDITOR
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Props");
            string texPath = "Assets/Materials/Props/Tex_Dumpster_Decals.png";
#endif

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color bgBlue = dumpsterBlue;

            // Fill background with matching dumpster blue
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    tex.SetPixel(x, y, bgBlue);
                }
            }

            // --- DECAL 1: Left Company Placard (White box with text) ---
            DrawRect(tex, 80, 40, 190, 170, Color.white);
            DrawRectBorder(tex, 80, 40, 190, 170, 3, new Color(0.1f, 0.1f, 0.1f));
            // Small yellow caution flag in top left
            DrawRect(tex, 85, 170, 50, 32, new Color(0.95f, 0.85f, 0.10f));
            DrawTextLines(tex, 140, 175, 4, 12, new Color(0.1f, 0.1f, 0.1f)); // "CAUTION"
            DrawTextLines(tex, 95, 135, 160, 24, new Color(0.1f, 0.1f, 0.1f)); // "SPECIALTY SOLID WASTE"
            DrawTextLines(tex, 95, 95, 160, 24, new Color(0.1f, 0.1f, 0.1f)); // "& RECYCLING"
            DrawTextLines(tex, 105, 55, 140, 28, new Color(0.1f, 0.1f, 0.1f)); // "565-9900"

            // --- DECAL 2: "C425" Fleet Number (White vinyl box with blue bold letters) ---
            DrawRect(tex, 320, 95, 160, 95, Color.white);
            DrawRectBorder(tex, 320, 95, 160, 95, 2, new Color(0.2f, 0.2f, 0.3f));
            DrawC425(tex, 335, 115);

            // --- DECAL 3: "DO NOT PARK / NO HAZARDOUS WASTE" (Yellow/Red warning sign) ---
            DrawRect(tex, 530, 45, 210, 165, new Color(0.98f, 0.88f, 0.12f)); // Yellow background
            DrawRectBorder(tex, 530, 45, 210, 165, 3, new Color(0.1f, 0.1f, 0.1f));
            // Red "DO NOT PARK" header box
            DrawRect(tex, 535, 170, 200, 35, new Color(0.85f, 0.15f, 0.15f));
            DrawTextLines(tex, 550, 178, 170, 18, Color.white); // "DO NOT PARK"
            // "NO HAZARDOUS WASTES ACCEPTED" text
            DrawTextLines(tex, 545, 142, 180, 16, Color.black);
            // Pictogram icons row (flame, toxic, biohazard, etc.)
            for (int p = 0; p < 4; p++)
            {
                DrawCircle(tex, 560 + p * 45, 115, 14, Color.black);
                DrawCircle(tex, 560 + p * 45, 115, 11, new Color(0.98f, 0.88f, 0.12f));
            }
            // Caution banners
            DrawRect(tex, 540, 60, 90, 25, Color.black);
            DrawRect(tex, 640, 60, 90, 25, Color.black);

            // --- DECAL 4: Right CAUTION Placard ---
            DrawRect(tex, 780, 75, 140, 135, new Color(0.98f, 0.88f, 0.12f));
            DrawRectBorder(tex, 780, 75, 140, 135, 2, Color.black);
            DrawRect(tex, 785, 175, 130, 28, Color.black);
            DrawTextLines(tex, 790, 140, 120, 14, Color.black);
            DrawTextLines(tex, 790, 115, 120, 14, Color.black);
            DrawTextLines(tex, 790, 90, 120, 14, Color.black);

            tex.Apply();

#if UNITY_EDITOR
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(texPath, bytes);
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
#endif
            return tex;
        }

        private void DrawRect(Texture2D tex, int x0, int y0, int w, int h, Color col)
        {
            for (int y = y0; y < y0 + h && y < tex.height; y++)
            {
                for (int x = x0; x < x0 + w && x < tex.width; x++)
                {
                    if (x >= 0 && y >= 0) tex.SetPixel(x, y, col);
                }
            }
        }

        private void DrawRectBorder(Texture2D tex, int x0, int y0, int w, int h, int thickness, Color col)
        {
            DrawRect(tex, x0, y0, w, thickness, col);
            DrawRect(tex, x0, y0 + h - thickness, w, thickness, col);
            DrawRect(tex, x0, y0, thickness, h, col);
            DrawRect(tex, x0 + w - thickness, y0, thickness, h, col);
        }

        private void DrawCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            int r2 = r * r;
            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy <= r2)
                    {
                        int px = cx + dx;
                        int py = cy + dy;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                            tex.SetPixel(px, py, col);
                    }
                }
            }
        }

        private void DrawTextLines(Texture2D tex, int x0, int y0, int w, int h, Color col)
        {
            // Clean bar simulation of text lines for billboard decals
            DrawRect(tex, x0, y0, w, h, col);
        }

        private void DrawC425(Texture2D tex, int x0, int y0)
        {
            Color blue = new Color(0.12f, 0.22f, 0.65f); // Deep stencil blue
            int charW = 22;
            int charH = 55;
            int t = 6;

            // 'C'
            DrawRect(tex, x0, y0, charW, t, blue);
            DrawRect(tex, x0, y0 + charH - t, charW, t, blue);
            DrawRect(tex, x0, y0, t, charH, blue);

            // '4'
            int x1 = x0 + 32;
            DrawRect(tex, x1, y0 + 20, charW, t, blue);
            DrawRect(tex, x1, y0 + 20, t, charH - 20, blue);
            DrawRect(tex, x1 + charW - t, y0, t, charH, blue);

            // '2'
            int x2 = x1 + 32;
            DrawRect(tex, x2, y0, charW, t, blue);
            DrawRect(tex, x2, (int)(y0 + charH * 0.5f - t * 0.5f), charW, t, blue);
            DrawRect(tex, x2, y0 + charH - t, charW, t, blue);
            DrawRect(tex, x2, y0, t, (int)(charH * 0.5f), blue);
            DrawRect(tex, x2 + charW - t, y0 + (int)(charH * 0.5f), t, (int)(charH * 0.5f), blue);

            // '5'
            int x3 = x2 + 32;
            DrawRect(tex, x3, y0, charW, t, blue);
            DrawRect(tex, x3, (int)(y0 + charH * 0.5f - t * 0.5f), charW, t, blue);
            DrawRect(tex, x3, y0 + charH - t, charW, t, blue);
            DrawRect(tex, x3, y0 + (int)(charH * 0.5f), t, (int)(charH * 0.5f), blue);
            DrawRect(tex, x3 + charW - t, y0, t, (int)(charH * 0.5f), blue);
        }

#if UNITY_EDITOR
        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        [MenuItem("Tools/Props/Generate Dumpster", priority = 10)]
        public static GameObject GenerateDumpsterAtTarget()
        {
            Vector3 targetPos = new Vector3(4.0f, 0.10f, 31.0f);
            Quaternion targetRot = Quaternion.Euler(0, 180f, 0); // Front faces outward towards +Z

            GameObject dumpster = GameObject.Find("Dumpster");
            if (dumpster == null)
            {
                dumpster = new GameObject("Dumpster");
                Undo.RegisterCreatedObjectUndo(dumpster, "Create Dumpster");
            }

            dumpster.transform.position = targetPos;
            dumpster.transform.rotation = targetRot;

            DumpsterBuilder builder = dumpster.GetComponent<DumpsterBuilder>();
            if (builder == null) builder = dumpster.AddComponent<DumpsterBuilder>();

            builder.BuildDumpster();

            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Props");
            string prefabPath = "Assets/Prefabs/Props/Dumpster_Blue.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(dumpster, prefabPath, InteractionMode.AutomatedAction);

            Selection.activeGameObject = dumpster;
            EditorUtility.SetDirty(dumpster);
            AssetDatabase.SaveAssets();

            Debug.Log("[DumpsterBuilder] Dumpster placed at " + targetPos + " and saved prefab at: " + prefabPath);
            return dumpster;
        }
#endif
    }
}
