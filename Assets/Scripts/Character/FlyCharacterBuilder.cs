using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutritionKitchen.Character
{
    [ExecuteInEditMode]
    public class FlyCharacterBuilder : MonoBehaviour
    {
        [Header("Fly Palette")]
        public Color bodyColor = new Color(0.14f, 0.28f, 0.22f);         // ลำตัวสีเขียวปีกแมลงทับเหลือบดำ
        public Color abdomenColor = new Color(0.12f, 0.22f, 0.18f);      // ท้องแมลงวันสีเข้ม
        public Color eyeColor = new Color(0.72f, 0.14f, 0.18f);          // ตาประกอบขนาดใหญ่สีทับทิม/แดงเข้ม
        public Color wingColor = new Color(0.90f, 0.95f, 1.0f, 0.65f);    // ปีกโปร่งแสงสีฟ้าประกายมุก
        public Color legColor = new Color(0.10f, 0.10f, 0.12f);          // ขาและหนวดสีดำชาร์โคล

        private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        [ContextMenu("Build Fly Model")]
        public void BuildCharacter()
        {
            materialCache.Clear();

            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

            Material bodyMat = GetOrCreateMaterial("Mat_Fly_Body", bodyColor, 0.85f, 0.2f);
            Material abdomenMat = GetOrCreateMaterial("Mat_Fly_Abdomen", abdomenColor, 0.70f, 0.1f);
            Material eyeMat = GetOrCreateMaterial("Mat_Fly_Eye", eyeColor, 0.90f, 0.0f);
            Material wingMat = GetOrCreateTransparentMaterial("Mat_Fly_Wing", wingColor);
            Material legMat = GetOrCreateMaterial("Mat_Fly_Leg", legColor, 0.40f, 0.0f);
            Material highlightMat = GetOrCreateMaterial("Mat_Fly_Highlight", Color.white, 0.95f, 0.0f);

            // 1. BodyRoot (ลอยตัวอยู่ที่ระดับความสูงพอเหมาะ)
            GameObject bodyRoot = new GameObject("BodyRoot");
            bodyRoot.transform.SetParent(transform, false);
            bodyRoot.transform.localPosition = new Vector3(0, 0.85f, 0);

            // 2. ส่วนอก (Thorax)
            GameObject thorax = CreatePrimitive(PrimitiveType.Sphere, "Thorax", bodyRoot.transform,
                Vector3.zero, Quaternion.identity, new Vector3(0.38f, 0.36f, 0.42f), bodyMat);

            // 3. ส่วนหัว (Head & Giant Compound Eyes)
            GameObject head = new GameObject("Head");
            head.transform.SetParent(bodyRoot.transform, false);
            head.transform.localPosition = new Vector3(0, 0.06f, 0.22f);

            CreatePrimitive(PrimitiveType.Sphere, "HeadBase", head.transform,
                Vector3.zero, Quaternion.identity, new Vector3(0.32f, 0.30f, 0.26f), bodyMat);

            // ตาประกอบขนาดใหญ่สีแดงทับทิม 2 ข้าง (Giant Ruby Compound Eyes)
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                Vector3 eyePos = new Vector3(i * 0.13f, 0.04f, 0.08f);

                GameObject eye = CreatePrimitive(PrimitiveType.Sphere, side + "CompoundEye", head.transform,
                    eyePos, Quaternion.Euler(0, i * 20f, i * -15f), new Vector3(0.18f, 0.24f, 0.20f), eyeMat);

                // แววตาเงาวาวด้านบน
                CreatePrimitive(PrimitiveType.Sphere, side + "Highlight", eye.transform,
                    new Vector3(0.04f, 0.06f, 0.06f), Quaternion.identity, new Vector3(0.05f, 0.07f, 0.04f), highlightMat);
            }

            // หนวด 2 เส้น (Antennae)
            Transform leftAntennaTr = null, rightAntennaTr = null;
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                GameObject antObj = new GameObject(side + "Antenna");
                antObj.transform.SetParent(head.transform, false);
                antObj.transform.localPosition = new Vector3(i * 0.04f, 0.15f, 0.10f);
                antObj.transform.localRotation = Quaternion.Euler(20f, i * 15f, 0);

                if (i < 0) leftAntennaTr = antObj.transform;
                else rightAntennaTr = antObj.transform;

                CreatePrimitive(PrimitiveType.Cylinder, "Stem", antObj.transform,
                    new Vector3(0, 0.06f, 0), Quaternion.identity, new Vector3(0.015f, 0.06f, 0.015f), legMat);

                CreatePrimitive(PrimitiveType.Sphere, "Tip", antObj.transform,
                    new Vector3(0, 0.12f, 0), Quaternion.identity, new Vector3(0.04f, 0.04f, 0.04f), bodyMat);
            }

            // ปากดูดแมลงวัน (Proboscis)
            CreatePrimitive(PrimitiveType.Cylinder, "Proboscis", head.transform,
                new Vector3(0, -0.16f, 0.10f), Quaternion.Euler(30f, 0, 0), new Vector3(0.05f, 0.08f, 0.05f), legMat);
            CreatePrimitive(PrimitiveType.Sphere, "ProboscisPad", head.transform,
                new Vector3(0, -0.22f, 0.14f), Quaternion.identity, new Vector3(0.08f, 0.04f, 0.08f), legMat);

            // 4. ส่วนท้องปล้องหลัง (Abdomen)
            GameObject abdomen = CreatePrimitive(PrimitiveType.Sphere, "Abdomen", bodyRoot.transform,
                new Vector3(0, -0.06f, -0.32f), Quaternion.Euler(-18f, 0, 0), new Vector3(0.44f, 0.40f, 0.55f), abdomenMat);

            // แถบลายปล้องท้อง (Abdominal Stripes)
            CreatePrimitive(PrimitiveType.Cylinder, "Stripe1", abdomen.transform,
                new Vector3(0, 0, 0.08f), Quaternion.Euler(90f, 0, 0), new Vector3(0.45f, 0.02f, 0.41f), bodyMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Stripe2", abdomen.transform,
                new Vector3(0, 0, -0.06f), Quaternion.Euler(90f, 0, 0), new Vector3(0.40f, 0.02f, 0.36f), bodyMat);

            // 5. ปีกโปร่งแสง 2 ข้าง (Translucent Glassy Wings)
            Transform leftWingTr = null, rightWingTr = null;
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                GameObject wingObj = new GameObject(side + "Wing");
                wingObj.transform.SetParent(bodyRoot.transform, false);
                wingObj.transform.localPosition = new Vector3(i * 0.10f, 0.18f, -0.05f);
                wingObj.transform.localRotation = Quaternion.Euler(15f, i * 25f, i * -12f);

                if (i < 0) leftWingTr = wingObj.transform;
                else rightWingTr = wingObj.transform;

                // แผ่นปีกทรงหยดน้ำโปร่งแสง
                CreatePrimitive(PrimitiveType.Capsule, "WingBlade", wingObj.transform,
                    new Vector3(i * 0.18f, 0, -0.35f), Quaternion.Euler(0, i * 25f, 90f), new Vector3(0.015f, 0.45f, 0.28f), wingMat);

                // เส้นขอบปีกดาร์กคมชัด
                CreatePrimitive(PrimitiveType.Cylinder, "WingLeadingEdge", wingObj.transform,
                    new Vector3(i * 0.22f, 0, -0.35f), Quaternion.Euler(90f, 0, i * -25f), new Vector3(0.012f, 0.44f, 0.012f), legMat);
            }

            // 6. ขาทั้ง 6 ข้าง (6 Legs)
            // ขาหน้า 2 ข้าง (ยกขึ้นมาด้านหน้าเพื่อถูมือ)
            GameObject leftFrontLeg = BuildLeg(bodyRoot.transform, isLeft: true, "Front",
                new Vector3(-0.12f, -0.08f, 0.14f), Quaternion.Euler(30f, 25f, -15f), legMat);
            GameObject rightFrontLeg = BuildLeg(bodyRoot.transform, isLeft: false, "Front",
                new Vector3(0.12f, -0.08f, 0.14f), Quaternion.Euler(30f, -25f, 15f), legMat);

            // ขากลาง 2 ข้าง
            GameObject leftMidLeg = BuildLeg(bodyRoot.transform, isLeft: true, "Mid",
                new Vector3(-0.16f, -0.12f, 0.00f), Quaternion.Euler(-10f, 10f, -20f), legMat);
            GameObject rightMidLeg = BuildLeg(bodyRoot.transform, isLeft: false, "Mid",
                new Vector3(0.16f, -0.12f, 0.00f), Quaternion.Euler(-10f, -10f, 20f), legMat);

            // ขาหลัง 2 ข้าง
            GameObject leftHindLeg = BuildLeg(bodyRoot.transform, isLeft: true, "Hind",
                new Vector3(-0.14f, -0.12f, -0.14f), Quaternion.Euler(-25f, 0f, -25f), legMat);
            GameObject rightHindLeg = BuildLeg(bodyRoot.transform, isLeft: false, "Hind",
                new Vector3(0.14f, -0.12f, -0.14f), Quaternion.Euler(-25f, 0f, 25f), legMat);

            // 7. ต่อ Animator
            FlyCharacterAnimator animator = gameObject.GetComponent<FlyCharacterAnimator>();
            if (animator == null) animator = gameObject.AddComponent<FlyCharacterAnimator>();

            animator.bodyRoot = bodyRoot.transform;
            animator.head = head.transform;
            animator.leftWing = leftWingTr;
            animator.rightWing = rightWingTr;
            animator.leftFrontLeg = leftFrontLeg.transform;
            animator.rightFrontLeg = rightFrontLeg.transform;
            animator.leftMidLeg = leftMidLeg.transform;
            animator.rightMidLeg = rightMidLeg.transform;
            animator.leftHindLeg = leftHindLeg.transform;
            animator.rightHindLeg = rightHindLeg.transform;
            animator.leftAntenna = leftAntennaTr;
            animator.rightAntenna = rightAntennaTr;

            animator.RecordInitialTransforms();

            Debug.Log("[FlyCharacterBuilder] สร้างโมเดล Fly NPC สำเร็จเรียบร้อย!");
        }

        private GameObject BuildLeg(Transform parent, bool isLeft, string legType, Vector3 localPos, Quaternion localRot, Material legMat)
        {
            float sideSign = isLeft ? -1f : 1f;
            string legName = (isLeft ? "Left" : "Right") + legType + "Leg";

            GameObject legObj = new GameObject(legName);
            legObj.transform.SetParent(parent, false);
            legObj.transform.localPosition = localPos;
            legObj.transform.localRotation = localRot;

            // ท่อนขาบน (Femur)
            CreatePrimitive(PrimitiveType.Cylinder, "Femur", legObj.transform,
                new Vector3(0, -0.08f, 0.04f), Quaternion.Euler(30f, 0, 0), new Vector3(0.025f, 0.09f, 0.025f), legMat);

            // ท่อนขาล่าง (Tibia)
            CreatePrimitive(PrimitiveType.Cylinder, "Tibia", legObj.transform,
                new Vector3(0, -0.18f, 0.08f), Quaternion.Euler(-20f, 0, 0), new Vector3(0.02f, 0.10f, 0.02f), legMat);

            // อุ้งเท้าหน้าสำหรับถู (Tarsus)
            CreatePrimitive(PrimitiveType.Sphere, "Tarsus", legObj.transform,
                new Vector3(0, -0.26f, 0.10f), Quaternion.identity, new Vector3(0.035f, 0.05f, 0.035f), legMat);

            return legObj;
        }

        private GameObject CreatePrimitive(PrimitiveType type, string partName, Transform parent,
                                           Vector3 localPos, Quaternion localRot, Vector3 localScale, Material mat)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localRotation = localRot;
            part.transform.localScale = localScale;

            Collider col = part.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            if (renderer != null && mat != null) renderer.sharedMaterial = mat;

            return part;
        }

        private Material GetOrCreateMaterial(string matName, Color color, float smoothness, float metallic)
        {
            if (materialCache.TryGetValue(matName, out Material cached) && cached != null)
                return cached;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader) { name = matName, color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

#if UNITY_EDITOR
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Fly");
            string matPath = "Assets/Materials/Fly/" + matName + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
            {
                existing.color = color;
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
                materialCache[matName] = existing;
                return existing;
            }
            AssetDatabase.CreateAsset(mat, matPath);
#endif
            materialCache[matName] = mat;
            return mat;
        }

        private Material GetOrCreateTransparentMaterial(string matName, Color color)
        {
            if (materialCache.TryGetValue(matName, out Material cached) && cached != null)
                return cached;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader) { name = matName, color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.95f);

            // Transparent mode settings
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // 1 = Transparent in URP
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0);     // 0 = Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

#if UNITY_EDITOR
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Fly");
            string matPath = "Assets/Materials/Fly/" + matName + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
            {
                existing.color = color;
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
                materialCache[matName] = existing;
                return existing;
            }
            AssetDatabase.CreateAsset(mat, matPath);
#endif
            materialCache[matName] = mat;
            return mat;
        }

#if UNITY_EDITOR
        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        [MenuItem("Tools/Character/Generate Fly NPC", priority = 7)]
        public static GameObject GenerateFromMenu()
        {
            GameObject flyObj = GameObject.Find("FlyNPC");
            if (flyObj == null)
            {
                flyObj = new GameObject("FlyNPC");
                Undo.RegisterCreatedObjectUndo(flyObj, "Create Fly NPC");
            }

            FlyCharacterBuilder builder = flyObj.GetComponent<FlyCharacterBuilder>();
            if (builder == null) builder = flyObj.AddComponent<FlyCharacterBuilder>();

            builder.BuildCharacter();

            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "NPCs");
            string prefabPath = "Assets/Prefabs/NPCs/NPC_Fly.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(flyObj, prefabPath, InteractionMode.AutomatedAction);

            Selection.activeGameObject = flyObj;
            EditorUtility.SetDirty(flyObj);
            AssetDatabase.SaveAssets();

            Debug.Log("[FlyCharacterBuilder] Saved Prefab at: " + prefabPath);
            return flyObj;
        }
#endif
    }
}
