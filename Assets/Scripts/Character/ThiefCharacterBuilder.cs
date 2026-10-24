using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutritionKitchen.Character
{
    [ExecuteInEditMode]
    public class ThiefCharacterBuilder : MonoBehaviour
    {
        [Header("Thief Color Palette (Based on Reference)")]
        public Color skinColor = new Color(0.96f, 0.80f, 0.70f);
        public Color beanieColor = new Color(0.12f, 0.12f, 0.14f);
        public Color maskColor = new Color(0.08f, 0.08f, 0.09f);
        public Color hairColor = new Color(0.28f, 0.17f, 0.12f);
        public Color eyeWhiteColor = new Color(0.98f, 0.98f, 0.98f);
        public Color irisColor = new Color(0.55f, 0.35f, 0.18f);
        public Color pupilColor = new Color(0.05f, 0.05f, 0.05f);
        public Color highlightColor = Color.white;
        public Color eyebrowColor = new Color(0.22f, 0.14f, 0.10f);
        public Color mouthColor = new Color(0.52f, 0.22f, 0.24f);
        public Color pantsColor = new Color(0.13f, 0.13f, 0.15f);
        public Color bootsColor = new Color(0.09f, 0.09f, 0.10f);

        [Header("Striped Shirt Settings")]
        public int stripeCount = 6;

        private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        [ContextMenu("Build Thief Model")]
        public void BuildCharacter()
        {
            materialCache.Clear();

            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

            Material skinMat = GetOrCreateMaterial("Mat_Thief_Skin", skinColor, 0.32f);
            Material beanieMat = GetOrCreateMaterial("Mat_Thief_Beanie", beanieColor, 0.20f);
            Material maskMat = GetOrCreateMaterial("Mat_Thief_Mask", maskColor, 0.60f);
            Material hairMat = GetOrCreateMaterial("Mat_Thief_Hair", hairColor, 0.30f);
            Material eyeWhiteMat = GetOrCreateMaterial("Mat_Thief_EyeWhite", eyeWhiteColor, 0.85f);
            Material irisMat = GetOrCreateMaterial("Mat_Thief_Iris", irisColor, 0.88f);
            Material pupilMat = GetOrCreateMaterial("Mat_Thief_Pupil", pupilColor, 0.90f);
            Material highlightMat = GetOrCreateMaterial("Mat_Thief_Highlight", highlightColor, 0.95f);
            Material eyebrowMat = GetOrCreateMaterial("Mat_Thief_Eyebrow", eyebrowColor, 0.25f);
            Material mouthMat = GetOrCreateMaterial("Mat_Thief_Mouth", mouthColor, 0.35f);
            Material pantsMat = GetOrCreateMaterial("Mat_Thief_Pants", pantsColor, 0.22f);
            Material bootsMat = GetOrCreateMaterial("Mat_Thief_Boots", bootsColor, 0.40f);
            Material stripesMat = GetOrCreateStripedMaterial("Mat_Thief_Stripes", stripeCount);

            GameObject bodyRoot = new GameObject("BodyRoot");
            bodyRoot.transform.SetParent(transform, false);

            GameObject body = new GameObject("Body");
            body.transform.SetParent(bodyRoot.transform, false);
            body.transform.localPosition = new Vector3(0, 0.92f, 0);

            CreatePrimitive(PrimitiveType.Capsule, "TorsoMesh", body.transform,
                new Vector3(0, 0.14f, 0), Quaternion.identity, new Vector3(0.52f, 0.40f, 0.42f), stripesMat);

            CreatePrimitive(PrimitiveType.Cylinder, "NeckCollar", body.transform,
                new Vector3(0, 0.38f, 0), Quaternion.identity, new Vector3(0.28f, 0.04f, 0.28f), beanieMat);

            CreatePrimitive(PrimitiveType.Cylinder, "Neck", body.transform,
                new Vector3(0, 0.42f, 0), Quaternion.identity, new Vector3(0.22f, 0.10f, 0.22f), skinMat);

            CreatePrimitive(PrimitiveType.Cylinder, "ShirtHem", body.transform,
                new Vector3(0, -0.06f, 0), Quaternion.identity, new Vector3(0.48f, 0.04f, 0.40f), beanieMat);

            CreatePrimitive(PrimitiveType.Sphere, "Pelvis", body.transform,
                new Vector3(0, -0.14f, 0), Quaternion.identity, new Vector3(0.48f, 0.22f, 0.40f), pantsMat);

            GameObject head = new GameObject("Head");
            head.transform.SetParent(bodyRoot.transform, false);
            head.transform.localPosition = new Vector3(0, 1.54f, 0);

            CreatePrimitive(PrimitiveType.Sphere, "HeadMesh", head.transform,
                new Vector3(0, 0, 0), Quaternion.identity, new Vector3(0.86f, 0.80f, 0.82f), skinMat);

            for (int i = -1; i <= 1; i += 2)
            {
                CreatePrimitive(PrimitiveType.Sphere, (i < 0 ? "Left" : "Right") + "Ear", head.transform,
                    new Vector3(i * 0.42f, -0.04f, -0.02f), Quaternion.Euler(0, i * -15f, 0), new Vector3(0.12f, 0.18f, 0.14f), skinMat);
            }

            for (int i = -1; i <= 1; i += 2)
            {
                CreatePrimitive(PrimitiveType.Capsule, (i < 0 ? "Left" : "Right") + "TempleHair", head.transform,
                    new Vector3(i * 0.39f, 0.06f, 0.14f), Quaternion.Euler(15f, i * -10f, i * -20f), new Vector3(0.08f, 0.18f, 0.12f), hairMat);
            }

            BuildBeanieHat(head.transform, beanieMat);
            BuildMaskAndFace(head.transform, maskMat, eyeWhiteMat, irisMat, pupilMat, highlightMat, eyebrowMat, mouthMat, skinMat);

            Transform leftForearm, leftHand, rightForearm, rightHand;
            GameObject leftArm = BuildArm(bodyRoot.transform, isLeft: true, stripesMat, skinMat, beanieMat, out leftForearm, out leftHand);
            GameObject rightArm = BuildArm(bodyRoot.transform, isLeft: false, stripesMat, skinMat, beanieMat, out rightForearm, out rightHand);

            Transform leftShin, leftBoot, rightShin, rightBoot;
            GameObject leftLeg = BuildLeg(bodyRoot.transform, isLeft: true, pantsMat, bootsMat, out leftShin, out leftBoot);
            GameObject rightLeg = BuildLeg(bodyRoot.transform, isLeft: false, pantsMat, bootsMat, out rightShin, out rightBoot);

            ThiefCharacterAnimator animator = gameObject.GetComponent<ThiefCharacterAnimator>();
            if (animator == null) animator = gameObject.AddComponent<ThiefCharacterAnimator>();

            animator.bodyRoot = bodyRoot.transform;
            animator.head = head.transform;
            animator.leftArm = leftArm.transform;
            animator.leftForearm = leftForearm;
            animator.leftHand = leftHand;
            animator.rightArm = rightArm.transform;
            animator.rightForearm = rightForearm;
            animator.rightHand = rightHand;
            animator.leftLeg = leftLeg.transform;
            animator.leftShin = leftShin;
            animator.leftBoot = leftBoot;
            animator.rightLeg = rightLeg.transform;
            animator.rightShin = rightShin;
            animator.rightBoot = rightBoot;

            animator.RecordInitialTransforms();
            animator.currentState = ThiefAnimationState.StandingIdle;
            animator.ApplyStandingUprightPose();

            Debug.Log("[ThiefCharacterBuilder] Created Thief NPC standing upright on ground!");
        }

        private void BuildBeanieHat(Transform headTransform, Material beanieMat)
        {
            GameObject beanieRoot = new GameObject("BeanieHat");
            beanieRoot.transform.SetParent(headTransform, false);
            // Sits comfortably on top-back of head leaving forehead clearly exposed
            beanieRoot.transform.localPosition = new Vector3(0, 0.24f, -0.06f);
            beanieRoot.transform.localRotation = Quaternion.Euler(18f, 0, 0);

            CreatePrimitive(PrimitiveType.Sphere, "BeanieCrown", beanieRoot.transform,
                new Vector3(0, 0.16f, 0), Quaternion.identity, new Vector3(0.88f, 0.65f, 0.88f), beanieMat);

            CreatePrimitive(PrimitiveType.Cylinder, "BeanieCuffRing", beanieRoot.transform,
                new Vector3(0, 0.00f, 0), Quaternion.identity, new Vector3(0.92f, 0.12f, 0.92f), beanieMat);

            CreatePrimitive(PrimitiveType.Sphere, "CuffFrontBulge", beanieRoot.transform,
                new Vector3(0, 0.00f, 0.44f), Quaternion.identity, new Vector3(0.76f, 0.16f, 0.14f), beanieMat);
            CreatePrimitive(PrimitiveType.Sphere, "CuffBackBulge", beanieRoot.transform,
                new Vector3(0, 0.00f, -0.44f), Quaternion.identity, new Vector3(0.76f, 0.16f, 0.14f), beanieMat);
        }

        private void BuildMaskAndFace(Transform headTransform, Material maskMat, Material eyeWhiteMat, Material irisMat,
                                      Material pupilMat, Material highlightMat, Material eyebrowMat, Material mouthMat, Material skinMat)
        {
            GameObject faceRoot = new GameObject("FaceAndMask");
            faceRoot.transform.SetParent(headTransform, false);

            GameObject maskObj = new GameObject("DominoMask");
            maskObj.transform.SetParent(faceRoot.transform, false);
            maskObj.transform.localPosition = new Vector3(0, 0.01f, 0.36f);

            CreatePrimitive(PrimitiveType.Cube, "MaskBridge", maskObj.transform,
                new Vector3(0, -0.01f, 0.035f), Quaternion.identity, new Vector3(0.10f, 0.05f, 0.04f), maskMat);

            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";

                CreatePrimitive(PrimitiveType.Sphere, side + "MaskEyePatch", maskObj.transform,
                    new Vector3(i * 0.185f, 0.00f, 0.025f), Quaternion.Euler(0, i * 10f, 0), new Vector3(0.30f, 0.24f, 0.05f), maskMat);

                CreatePrimitive(PrimitiveType.Sphere, side + "MaskWingTip", maskObj.transform,
                    new Vector3(i * 0.33f, 0.02f, -0.015f), Quaternion.Euler(0, i * 30f, i * 12f), new Vector3(0.16f, 0.16f, 0.045f), maskMat);
            }

            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                Vector3 eyeCenter = new Vector3(i * 0.18f, 0.00f, 0.385f);

                CreatePrimitive(PrimitiveType.Sphere, side + "Sclera", faceRoot.transform,
                    eyeCenter, Quaternion.identity, new Vector3(0.16f, 0.18f, 0.055f), eyeWhiteMat);

                CreatePrimitive(PrimitiveType.Sphere, side + "Iris", faceRoot.transform,
                    eyeCenter + new Vector3(0, 0, 0.018f), Quaternion.identity, new Vector3(0.11f, 0.13f, 0.035f), irisMat);

                CreatePrimitive(PrimitiveType.Sphere, side + "Pupil", faceRoot.transform,
                    eyeCenter + new Vector3(0, 0, 0.030f), Quaternion.identity, new Vector3(0.065f, 0.075f, 0.025f), pupilMat);

                CreatePrimitive(PrimitiveType.Sphere, side + "Highlight", faceRoot.transform,
                    eyeCenter + new Vector3(0.022f, 0.025f, 0.038f), Quaternion.identity, new Vector3(0.032f, 0.032f, 0.020f), highlightMat);
            }

            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Capsule, side + "Eyebrow", faceRoot.transform,
                    new Vector3(i * 0.16f, 0.16f, 0.38f), Quaternion.Euler(-12f, i * 14f, i * 28f), new Vector3(0.028f, 0.10f, 0.022f), eyebrowMat);
            }

            CreatePrimitive(PrimitiveType.Sphere, "Nose", faceRoot.transform,
                new Vector3(0, -0.08f, 0.41f), Quaternion.identity, new Vector3(0.038f, 0.032f, 0.028f), skinMat);

            CreatePrimitive(PrimitiveType.Sphere, "WorriedMouth", faceRoot.transform,
                new Vector3(0, -0.16f, 0.39f), Quaternion.identity, new Vector3(0.085f, 0.055f, 0.038f), mouthMat);
        }

        private GameObject BuildArm(Transform bodyRoot, bool isLeft, Material stripesMat, Material skinMat, Material cuffMat,
                                    out Transform forearm, out Transform hand)
        {
            float sideSign = isLeft ? -1f : 1f;
            string armName = isLeft ? "LeftArm" : "RightArm";

            GameObject armObj = new GameObject(armName);
            armObj.transform.SetParent(bodyRoot, false);
            armObj.transform.localPosition = new Vector3(sideSign * 0.32f, 1.15f, 0.02f);
            armObj.transform.localRotation = Quaternion.identity;

            CreatePrimitive(PrimitiveType.Capsule, "UpperSleeve", armObj.transform,
                new Vector3(0, -0.12f, 0), Quaternion.identity, new Vector3(0.18f, 0.22f, 0.18f), stripesMat);

            GameObject forearmObj = new GameObject(isLeft ? "LeftForearm" : "RightForearm");
            forearmObj.transform.SetParent(armObj.transform, false);
            forearmObj.transform.localPosition = new Vector3(0, -0.24f, 0);
            forearmObj.transform.localRotation = Quaternion.identity;
            forearm = forearmObj.transform;

            CreatePrimitive(PrimitiveType.Capsule, "LowerSleeve", forearmObj.transform,
                new Vector3(0, -0.12f, 0), Quaternion.identity, new Vector3(0.16f, 0.20f, 0.16f), stripesMat);

            CreatePrimitive(PrimitiveType.Cylinder, "SleeveCuff", forearmObj.transform,
                new Vector3(0, -0.22f, 0), Quaternion.identity, new Vector3(0.17f, 0.03f, 0.17f), cuffMat);

            GameObject handObj = CreatePrimitive(PrimitiveType.Sphere, isLeft ? "LeftHand" : "RightHand", forearmObj.transform,
                new Vector3(0, -0.28f, 0), Quaternion.identity, new Vector3(0.16f, 0.08f, 0.16f), skinMat);
            hand = handObj.transform;

            CreatePrimitive(PrimitiveType.Capsule, "Thumb", handObj.transform,
                new Vector3(sideSign * -0.3f, 0, 0.35f), Quaternion.Euler(0, sideSign * 45f, 0), new Vector3(0.35f, 0.5f, 0.35f), skinMat);

            float[] fx = { -0.25f, -0.08f, 0.08f, 0.25f };
            for (int f = 0; f < 4; f++)
            {
                CreatePrimitive(PrimitiveType.Capsule, "Finger_" + f, handObj.transform,
                    new Vector3(fx[f], 0, 0.5f), Quaternion.Euler(10f, 0, 0), new Vector3(0.22f, 0.5f, 0.22f), skinMat);
            }

            return armObj;
        }

        private GameObject BuildLeg(Transform bodyRoot, bool isLeft, Material pantsMat, Material bootsMat,
                                    out Transform shin, out Transform boot)
        {
            float sideSign = isLeft ? -1f : 1f;
            string legName = isLeft ? "LeftLeg" : "RightLeg";

            GameObject legObj = new GameObject(legName);
            legObj.transform.SetParent(bodyRoot, false);
            legObj.transform.localPosition = new Vector3(sideSign * 0.15f, 0.74f, 0);
            legObj.transform.localRotation = Quaternion.identity;

            CreatePrimitive(PrimitiveType.Capsule, "Thigh", legObj.transform,
                new Vector3(0, -0.14f, 0), Quaternion.identity, new Vector3(0.20f, 0.22f, 0.20f), pantsMat);

            GameObject shinObj = new GameObject(isLeft ? "LeftShin" : "RightShin");
            shinObj.transform.SetParent(legObj.transform, false);
            shinObj.transform.localPosition = new Vector3(0, -0.28f, 0);
            shinObj.transform.localRotation = Quaternion.identity;
            shin = shinObj.transform;

            CreatePrimitive(PrimitiveType.Capsule, "Shin", shinObj.transform,
                new Vector3(0, -0.12f, 0), Quaternion.identity, new Vector3(0.18f, 0.20f, 0.18f), pantsMat);

            CreatePrimitive(PrimitiveType.Cylinder, "BootCollar", shinObj.transform,
                new Vector3(0, -0.22f, 0), Quaternion.identity, new Vector3(0.21f, 0.05f, 0.21f), bootsMat);

            GameObject bootObj = CreatePrimitive(PrimitiveType.Sphere, isLeft ? "LeftBoot" : "RightBoot", shinObj.transform,
                new Vector3(0, -0.30f, 0.06f), Quaternion.identity, new Vector3(0.22f, 0.16f, 0.32f), bootsMat);
            boot = bootObj.transform;

            CreatePrimitive(PrimitiveType.Sphere, "BootHeel", shinObj.transform,
                new Vector3(0, -0.30f, -0.04f), Quaternion.identity, new Vector3(0.20f, 0.16f, 0.20f), bootsMat);

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
            if (renderer != null && mat != null)
            {
                renderer.sharedMaterial = mat;
            }

            return part;
        }

        private Material GetOrCreateMaterial(string matName, Color color, float smoothness)
        {
            if (materialCache.TryGetValue(matName, out Material cached) && cached != null)
            {
                cached.color = color;
                if (cached.HasProperty("_BaseColor")) cached.SetColor("_BaseColor", color);
                if (cached.HasProperty("_Smoothness")) cached.SetFloat("_Smoothness", smoothness);
                return cached;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader)
            {
                name = matName,
                color = color
            };

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

#if UNITY_EDITOR
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Thief");
            string matPath = "Assets/Materials/Thief/" + matName + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
            {
                existing.color = color;
                if (existing.HasProperty("_BaseColor")) existing.SetColor("_BaseColor", color);
                if (existing.HasProperty("_Smoothness")) existing.SetFloat("_Smoothness", smoothness);
                materialCache[matName] = existing;
                return existing;
            }
            AssetDatabase.CreateAsset(mat, matPath);
#endif
            materialCache[matName] = mat;
            return mat;
        }

        private Material GetOrCreateStripedMaterial(string matName, int stripes)
        {
            if (materialCache.TryGetValue(matName, out Material cached) && cached != null)
                return cached;

            Texture2D tex = CreateStripesTexture(stripes);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader) { name = matName };
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.25f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

#if UNITY_EDITOR
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Thief");
            string matPath = "Assets/Materials/Thief/" + matName + ".mat";
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

        private Texture2D CreateStripesTexture(int stripes)
        {
            const int width = 256;
            const int height = 256;

#if UNITY_EDITOR
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Thief");
            string texPath = "Assets/Materials/Thief/Tex_Thief_Stripes.png";
#endif

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color blackCol = new Color(0.12f, 0.12f, 0.14f);
            Color whiteCol = new Color(0.96f, 0.96f, 0.98f);

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                int band = Mathf.FloorToInt(v * stripes * 2);
                Color c = (band % 2 == 0) ? whiteCol : blackCol;
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();

#if UNITY_EDITOR
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(texPath, bytes);
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
#endif
            return tex;
        }

#if UNITY_EDITOR
        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        [MenuItem("Tools/Character/Generate Thief NPC", priority = 5)]
        public static GameObject GenerateFromMenu()
        {
            GameObject thiefObj = GameObject.Find("ThiefNPC");
            if (thiefObj == null)
            {
                thiefObj = new GameObject("ThiefNPC");
                Undo.RegisterCreatedObjectUndo(thiefObj, "Create Thief NPC");
            }

            ThiefCharacterBuilder builder = thiefObj.GetComponent<ThiefCharacterBuilder>();
            if (builder == null) builder = thiefObj.AddComponent<ThiefCharacterBuilder>();

            builder.BuildCharacter();

            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "NPCs");
            string prefabPath = "Assets/Prefabs/NPCs/NPC_Thief.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(thiefObj, prefabPath, InteractionMode.AutomatedAction);

            Selection.activeGameObject = thiefObj;
            EditorUtility.SetDirty(thiefObj);
            AssetDatabase.SaveAssets();

            Debug.Log("[ThiefCharacterBuilder] Saved Prefab at: " + prefabPath);
            return thiefObj;
        }
#endif
    }
}
