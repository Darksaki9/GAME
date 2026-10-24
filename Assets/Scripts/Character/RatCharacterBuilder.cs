using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutritionKitchen.Character
{
    [ExecuteInEditMode]
    public class RatCharacterBuilder : MonoBehaviour
    {
        [Header("Realistic Rat Palette (Matching Reference)")]
        public Color furColor = new Color(0.22f, 0.20f, 0.19f);         // ขนสีเทาดำชาร์โคลอมน้ำตาลเข้ม (Dark Agouti / Charcoal)
        public Color underfurColor = new Color(0.36f, 0.34f, 0.32f);    // ขนใต้คอและท้องสีเทาอมน้ำตาลอ่อนกว่า (Lighter Underfur)
        public Color fleshPinkColor = new Color(0.88f, 0.74f, 0.71f);   // จมูก ใบหูด้านใน อุ้งเท้า และหางสีเนื้ออมชมพูธรรมชาติ (Natural Flesh Pink)
        public Color eyeColor = new Color(0.02f, 0.02f, 0.025f);        // ตาดำลูกปัดเงาสนิท (Glossy Jet-Black Bead Eye)
        public Color toothColor = new Color(0.93f, 0.86f, 0.68f);       // ฟันแทะสีงาช้างอ่อน (Ivory Incisors)
        public Color whiskerColor = new Color(0.92f, 0.92f, 0.92f, 0.85f); // หนวดเส้นบางสีขาวเงินสว่าง

        private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        [ContextMenu("Build Rat Model")]
        public void BuildCharacter()
        {
            materialCache.Clear();

            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

            Material furMat = GetOrCreateMaterial("Mat_Rat_Fur", furColor, 0.20f);
            Material underfurMat = GetOrCreateMaterial("Mat_Rat_Underfur", underfurColor, 0.20f);
            Material pinkMat = GetOrCreateMaterial("Mat_Rat_Pink", fleshPinkColor, 0.35f);
            Material eyeMat = GetOrCreateMaterial("Mat_Rat_Eye", eyeColor, 0.98f);
            Material highlightMat = GetOrCreateMaterial("Mat_Rat_Highlight", Color.white, 0.99f);
            Material toothMat = GetOrCreateMaterial("Mat_Rat_Tooth", toothColor, 0.40f);
            Material whiskerMat = GetOrCreateMaterial("Mat_Rat_Whisker", whiskerColor, 0.30f);

            // 1. BodyRoot
            GameObject bodyRoot = new GameObject("BodyRoot");
            bodyRoot.transform.SetParent(transform, false);
            bodyRoot.transform.localPosition = Vector3.zero;

            // 2. ลำตัวหลังโก่งตามกายวิภาคหนูจริง (Natural Arched Rodent Torso)
            GameObject body = new GameObject("Body");
            body.transform.SetParent(bodyRoot.transform, false);

            // สะโพก/บั้นท้ายส่วนหลังยกสูง (Arched Rump)
            CreatePrimitive(PrimitiveType.Sphere, "Rump", body.transform,
                new Vector3(0, 0.28f, -0.16f), Quaternion.Euler(-12f, 0, 0), new Vector3(0.36f, 0.34f, 0.42f), furMat);

            // สันหลังช่วงกลาง (Mid Back)
            CreatePrimitive(PrimitiveType.Sphere, "MidBack", body.transform,
                new Vector3(0, 0.29f, -0.02f), Quaternion.identity, new Vector3(0.34f, 0.32f, 0.32f), furMat);

            // อกและไหล่สโลปลงด้านหน้า (Chest & Shoulders)
            CreatePrimitive(PrimitiveType.Sphere, "Chest", body.transform,
                new Vector3(0, 0.25f, 0.12f), Quaternion.Euler(10f, 0, 0), new Vector3(0.30f, 0.28f, 0.34f), furMat);

            // ใต้ท้องและลำคอ (Underbelly & Throat)
            CreatePrimitive(PrimitiveType.Sphere, "Underbelly", body.transform,
                new Vector3(0, 0.18f, -0.02f), Quaternion.identity, new Vector3(0.26f, 0.18f, 0.40f), underfurMat);
            CreatePrimitive(PrimitiveType.Sphere, "Throat", body.transform,
                new Vector3(0, 0.20f, 0.18f), Quaternion.Euler(15f, 0, 0), new Vector3(0.20f, 0.16f, 0.20f), underfurMat);

            // 3. ส่วนหัวและใบหน้าตาม Reference Image (Sculpted Rodent Head & Face)
            GameObject head = new GameObject("Head");
            head.transform.SetParent(bodyRoot.transform, false);
            head.transform.localPosition = new Vector3(0, 0.23f, 0.26f);

            // กะโหลกศีรษะส่วนหลังและกระหม่อม (Cranium & Crown)
            CreatePrimitive(PrimitiveType.Sphere, "Cranium", head.transform,
                new Vector3(0, 0.025f, 0.02f), Quaternion.identity, new Vector3(0.18f, 0.16f, 0.20f), furMat);

            // กล้ามเนื้อแก้มซ้าย-ขวาให้หัวเรียวลิ่มไปข้างหน้าตามธรรมชาติ (Cheeks)
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Sphere, side + "Cheek", head.transform,
                    new Vector3(i * 0.065f, 0.005f, 0.04f), Quaternion.identity, new Vector3(0.10f, 0.11f, 0.15f), furMat);
            }

            // สันจมูกสโลปเรียวจากหน้าผาก (Nasal Slope Bridge)
            GameObject snoutObj = CreatePrimitive(PrimitiveType.Sphere, "SnoutBridge", head.transform,
                new Vector3(0, 0.010f, 0.12f), Quaternion.Euler(14f, 0, 0), new Vector3(0.13f, 0.12f, 0.17f), furMat);

            // กระบอกปากส่วนหน้าทรงลิ่ม (Tapered Front Muzzle)
            CreatePrimitive(PrimitiveType.Sphere, "FrontMuzzle", head.transform,
                new Vector3(0, -0.010f, 0.19f), Quaternion.Euler(16f, 0, 0), new Vector3(0.095f, 0.088f, 0.13f), furMat);

            // ปลายกระบอกปากก่อนถึงจมูก (Muzzle Tip)
            CreatePrimitive(PrimitiveType.Sphere, "MuzzleTip", head.transform,
                new Vector3(0, -0.022f, 0.245f), Quaternion.Euler(18f, 0, 0), new Vector3(0.065f, 0.058f, 0.08f), furMat);

            // พวงหนวดแก้มซ้าย-ขวา (Whisker Pads)
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Sphere, side + "WhiskerPad", head.transform,
                    new Vector3(i * 0.032f, -0.022f, 0.235f), Quaternion.identity, new Vector3(0.036f, 0.032f, 0.050f), furMat);
            }

            // ปลายจมูกกระดุมสีชมพูเล็กๆ (Pink Button Nose Tip / Rhinarium)
            CreatePrimitive(PrimitiveType.Sphere, "NoseTip", head.transform,
                new Vector3(0, -0.026f, 0.278f), Quaternion.Euler(16f, 0, 0), new Vector3(0.024f, 0.019f, 0.020f), pinkMat);

            // ขากรรไกรล่างและคาง (Lower Jaw & Chin)
            CreatePrimitive(PrimitiveType.Sphere, "ThroatBlend", head.transform,
                new Vector3(0, -0.035f, 0.06f), Quaternion.Euler(10f, 0, 0), new Vector3(0.13f, 0.08f, 0.16f), underfurMat);
            CreatePrimitive(PrimitiveType.Sphere, "LowerJaw", head.transform,
                new Vector3(0, -0.042f, 0.15f), Quaternion.Euler(14f, 0, 0), new Vector3(0.090f, 0.055f, 0.15f), underfurMat);
            CreatePrimitive(PrimitiveType.Sphere, "Chin", head.transform,
                new Vector3(0, -0.040f, 0.22f), Quaternion.identity, new Vector3(0.055f, 0.040f, 0.07f), underfurMat);

            // ฟันแทะหน้าคู่สีงาช้างโผล่ออกมาเล็กน้อยใต้จมูก (Rodent Incisors)
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                CreatePrimitive(PrimitiveType.Cube, side + "Incisor", head.transform,
                    new Vector3(i * 0.0055f, -0.038f, 0.268f), Quaternion.Euler(22f, i * -2f, 0), new Vector3(0.006f, 0.014f, 0.006f), toothMat);
            }

            // 4. ดวงตาลูกปัดกลมสีดำด้านข้างหัว พร้อมประกายแวววาว (Glossy Lateral Bead Eyes with Highlights)
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                Vector3 eyePos = new Vector3(i * 0.076f, 0.036f, 0.10f);

                // เบ้าตา/ขอบตาดำเนียน (Eye Socket Rim)
                CreatePrimitive(PrimitiveType.Sphere, side + "EyeRim", head.transform,
                    new Vector3(i * 0.072f, 0.036f, 0.10f), Quaternion.Euler(0, i * 45f, 0), new Vector3(0.044f, 0.042f, 0.040f), furMat);

                // ลูกตาดำเงาวับ (Glossy Eye Ball)
                GameObject eye = CreatePrimitive(PrimitiveType.Sphere, side + "Eye", head.transform,
                    eyePos, Quaternion.Euler(0, i * 45f, 0), new Vector3(0.038f, 0.038f, 0.038f), eyeMat);

                // ประกายไฮไลต์แสงตกกระทบสีขาว หันออกด้านข้างรับแสงให้เห็นชัดในมุม Profile (Crisp Specular Highlight)
                CreatePrimitive(PrimitiveType.Sphere, side + "Highlight", eye.transform,
                    new Vector3(i * 0.010f, 0.008f, 0.008f), Quaternion.identity, new Vector3(0.010f, 0.010f, 0.010f), highlightMat);
            }

            // 5. ใบหูทรงถ้วยบางสีชมพูด้านใน ขอบโค้งมนตามธรรมชาติ เอียงไปด้านหลัง (Shell-shaped Cupped Ears)
            Transform leftEarTr = null, rightEarTr = null;
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                GameObject earObj = new GameObject(side + "Ear");
                earObj.transform.SetParent(head.transform, false);
                earObj.transform.localPosition = new Vector3(i * 0.080f, 0.092f, -0.010f);
                earObj.transform.localRotation = Quaternion.Euler(12f, i * -18f, i * -18f);

                if (i < 0) leftEarTr = earObj.transform;
                else rightEarTr = earObj.transform;

                // ฐานหู (Ear Base)
                CreatePrimitive(PrimitiveType.Sphere, "EarBase", earObj.transform,
                    new Vector3(0, -0.025f, 0), Quaternion.identity, new Vector3(0.035f, 0.035f, 0.025f), furMat);

                // ใบหูด้านนอกสีขน (Outer Shell)
                CreatePrimitive(PrimitiveType.Sphere, "EarOuter", earObj.transform,
                    Vector3.zero, Quaternion.identity, new Vector3(0.095f, 0.115f, 0.022f), furMat);

                // ร่องใบหูด้านในสีเนื้อชมพูเว้าเข้า (Inner Pink Cavity)
                CreatePrimitive(PrimitiveType.Sphere, "EarInner", earObj.transform,
                    new Vector3(0, 0, 0.006f), Quaternion.identity, new Vector3(0.080f, 0.100f, 0.020f), pinkMat);
            }

            // 6. หนวดแผ่ออกด้านข้างอย่างสวยงามตามธรรมชาติ (Fanned Lateral Whiskers)
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                Vector3 whiskerOrigin = new Vector3(i * 0.036f, -0.024f, 0.245f);

                Vector3[] whiskerDirs = new Vector3[]
                {
                    new Vector3(i * 0.85f, 0.12f, 0.45f).normalized,
                    new Vector3(i * 0.92f, 0.00f, 0.35f).normalized,
                    new Vector3(i * 0.88f, -0.12f, 0.40f).normalized,
                    new Vector3(i * 0.90f, -0.22f, 0.18f).normalized
                };

                float[] lengths = new float[] { 0.17f, 0.19f, 0.17f, 0.15f };

                for (int w = 0; w < whiskerDirs.Length; w++)
                {
                    Vector3 dir = whiskerDirs[w];
                    float len = lengths[w];
                    // Unity Capsule ความสูงเริ่มต้นคือ 2.0 ดังนั้น localScale.y ต้องเป็น len * 0.5f เพื่อให้ความยาวจริงเท่ากับ len
                    CreatePrimitive(PrimitiveType.Capsule, side + "Whisker_" + (w + 1), head.transform,
                        whiskerOrigin + dir * (len * 0.5f), Quaternion.FromToRotation(Vector3.up, dir),
                        new Vector3(0.0026f, len * 0.5f, 0.0026f), whiskerMat);
                }
            }

            // 7. ขาหน้า 2 ข้าง ยืนระนาบกับพื้น (Front Legs Standing on Ground)
            GameObject leftFrontLeg = BuildFrontLeg(bodyRoot.transform, isLeft: true, furMat, pinkMat);
            GameObject rightFrontLeg = BuildFrontLeg(bodyRoot.transform, isLeft: false, furMat, pinkMat);

            // 8. ขาหลัง 2 ข้าง ยืนระนาบกับพื้น (Back Legs Standing on Ground)
            GameObject leftBackLeg = BuildBackLeg(bodyRoot.transform, isLeft: true, furMat, pinkMat);
            GameObject rightBackLeg = BuildBackLeg(bodyRoot.transform, isLeft: false, furMat, pinkMat);

            // 9. หางยาวเรียวสีชมพูทอดเลื้อยบนพื้นตรงตามภาพ (Smooth Continuous Trailing Tail)
            // โคนหางเชื่อมต่อก้นที่ Y = 0.17f, Z = -0.34f
            GameObject tailRoot = new GameObject("TailRoot");
            tailRoot.transform.SetParent(bodyRoot.transform, false);
            tailRoot.transform.localPosition = new Vector3(0, 0.17f, -0.34f);

            // ข้อต่อโคนหางเชื่อมต่อสะโพก (Tail Base Joint)
            CreatePrimitive(PrimitiveType.Sphere, "TailBaseJoint", tailRoot.transform,
                Vector3.zero, Quaternion.identity, new Vector3(0.040f, 0.040f, 0.040f), pinkMat);

            // ท่อน 1: สโลปลงจากสะโพกถึงพื้นดินพอดีเป๊ะ (Sloping Segment to Ground: Delta Y = -0.15f, Delta Z = -0.14f)
            Vector3 seg1Dir = new Vector3(0, -0.15f, -0.14f).normalized;
            float seg1Len = Mathf.Sqrt(0.15f * 0.15f + 0.14f * 0.14f); // ~0.205f
            CreatePrimitive(PrimitiveType.Capsule, "TailSegment1", tailRoot.transform,
                new Vector3(0, -0.075f, -0.070f), Quaternion.FromToRotation(Vector3.up, seg1Dir), new Vector3(0.034f, seg1Len * 0.5f, 0.034f), pinkMat);

            // จุดเปลี่ยนผ่านสู่ระนาบพื้นดิน (Ground Level: Y = 0.02f in world)
            GameObject tailMid = new GameObject("TailMid");
            tailMid.transform.SetParent(tailRoot.transform, false);
            tailMid.transform.localPosition = new Vector3(0, -0.15f, -0.14f);

            // ข้อต่อช่วงแตะพื้นดิน (Ground Transition Joint)
            CreatePrimitive(PrimitiveType.Sphere, "TailMidJoint", tailMid.transform,
                Vector3.zero, Quaternion.identity, new Vector3(0.030f, 0.030f, 0.030f), pinkMat);

            // ท่อน 2: วางราบแนบพื้นดินอย่างสมบูรณ์แบบ (Laying Flat on Ground)
            CreatePrimitive(PrimitiveType.Capsule, "TailSegment2", tailMid.transform,
                new Vector3(0, 0.00f, -0.18f), Quaternion.Euler(90f, 0, 0), new Vector3(0.028f, 0.36f * 0.5f, 0.028f), pinkMat);

            GameObject tailTip = new GameObject("TailTip");
            tailTip.transform.SetParent(tailMid.transform, false);
            tailTip.transform.localPosition = new Vector3(0, 0.00f, -0.36f);

            // ข้อต่อปลายหาง (Tip Joint)
            CreatePrimitive(PrimitiveType.Sphere, "TailTipJoint", tailTip.transform,
                Vector3.zero, Quaternion.identity, new Vector3(0.024f, 0.024f, 0.024f), pinkMat);

            // ท่อน 3: ท้ายหางเรียวเล็กลงแนบพื้นดิน (Tapered End Tail on Ground)
            CreatePrimitive(PrimitiveType.Capsule, "TailSegment3", tailTip.transform,
                new Vector3(0, 0.00f, -0.22f), Quaternion.Euler(90f, 0, 0), new Vector3(0.020f, 0.44f * 0.5f, 0.020f), pinkMat);

            // ปลายสุดของหางมนกลม (Very End Tip Sphere)
            CreatePrimitive(PrimitiveType.Sphere, "TailEndSphere", tailTip.transform,
                new Vector3(0, 0.00f, -0.44f), Quaternion.identity, new Vector3(0.016f, 0.016f, 0.016f), pinkMat);

            // 10. เชื่อมต่อ Animator
            RatCharacterAnimator animator = gameObject.GetComponent<RatCharacterAnimator>();
            if (animator == null) animator = gameObject.AddComponent<RatCharacterAnimator>();

            animator.bodyRoot = bodyRoot.transform;
            animator.head = head.transform;
            animator.snout = snoutObj.transform;
            animator.leftEar = leftEarTr;
            animator.rightEar = rightEarTr;
            animator.leftFrontLeg = leftFrontLeg.transform;
            animator.rightFrontLeg = rightFrontLeg.transform;
            animator.leftBackLeg = leftBackLeg.transform;
            animator.rightBackLeg = rightBackLeg.transform;
            animator.tailRoot = tailRoot.transform;
            animator.tailMid = tailMid.transform;
            animator.tailTip = tailTip.transform;

            animator.RecordInitialTransforms();
            animator.currentState = RatAnimationState.FourLeggedIdle;

            Debug.Log("[RatCharacterBuilder] สร้างโมเดล Rat NPC ตามภาพ Reference สำเร็จเรียบร้อย!");
        }

        private GameObject BuildFrontLeg(Transform parent, bool isLeft, Material furMat, Material pinkMat)
        {
            float sideSign = isLeft ? -1f : 1f;
            string legName = isLeft ? "LeftFrontLeg" : "RightFrontLeg";

            GameObject legObj = new GameObject(legName);
            legObj.transform.SetParent(parent, false);
            legObj.transform.localPosition = new Vector3(sideSign * 0.12f, 0.20f, 0.14f);

            // ท่อนแขนบน (Upper Arm)
            CreatePrimitive(PrimitiveType.Capsule, "UpperLeg", legObj.transform,
                new Vector3(0, -0.06f, 0.01f), Quaternion.Euler(-8f, 0, 0), new Vector3(0.055f, 0.13f, 0.055f), furMat);

            // ปลายขาหน้าเปลี่ยนเป็นสีเนื้อชมพู (Forearm to Paw)
            CreatePrimitive(PrimitiveType.Capsule, "Forearm", legObj.transform,
                new Vector3(0, -0.14f, 0.02f), Quaternion.identity, new Vector3(0.040f, 0.09f, 0.040f), pinkMat);

            // อุ้งเท้าหน้า 4 นิ้ว แนบพื้นราบ (Front Paw Pad)
            GameObject paw = CreatePrimitive(PrimitiveType.Sphere, "PawPad", legObj.transform,
                new Vector3(0, -0.19f, 0.035f), Quaternion.identity, new Vector3(0.055f, 0.022f, 0.075f), pinkMat);

            // นิ้วเท้า
            for (int f = -1; f <= 2; f++)
            {
                CreatePrimitive(PrimitiveType.Capsule, "Toe_" + f, paw.transform,
                    new Vector3((f - 0.5f) * 0.016f, 0, 0.030f), Quaternion.Euler(15f, 0, 0), new Vector3(0.012f, 0.040f, 0.012f), pinkMat);
            }

            return legObj;
        }

        private GameObject BuildBackLeg(Transform parent, bool isLeft, Material furMat, Material pinkMat)
        {
            float sideSign = isLeft ? -1f : 1f;
            string legName = isLeft ? "LeftBackLeg" : "RightBackLeg";

            GameObject legObj = new GameObject(legName);
            legObj.transform.SetParent(parent, false);
            legObj.transform.localPosition = new Vector3(sideSign * 0.15f, 0.22f, -0.16f);

            // น่อง/สะโพกหลังหนานุ่ม (Thigh Flank)
            CreatePrimitive(PrimitiveType.Sphere, "ThighFlank", legObj.transform,
                Vector3.zero, Quaternion.Euler(-12f, 0, 0), new Vector3(0.12f, 0.16f, 0.19f), furMat);

            // ท่อนขาหลังสโลปไปข้างหน้าสู่ข้อเท้า (Hock)
            CreatePrimitive(PrimitiveType.Capsule, "Hock", legObj.transform,
                new Vector3(0, -0.09f, 0.02f), Quaternion.Euler(30f, 0, 0), new Vector3(0.050f, 0.13f, 0.050f), furMat);

            // ฝ่าเท้าหลังเรียวยาวสีเนื้อชมพูแนบพื้นราบ (Long Rear Foot)
            GameObject foot = CreatePrimitive(PrimitiveType.Capsule, "RearFoot", legObj.transform,
                new Vector3(0, -0.20f, 0.06f), Quaternion.Euler(85f, 0, 0), new Vector3(0.055f, 0.13f, 0.030f), pinkMat);

            // นิ้วเท้าหลัง 5 นิ้ว
            for (int f = -2; f <= 2; f++)
            {
                CreatePrimitive(PrimitiveType.Capsule, "Toe_" + f, foot.transform,
                    new Vector3(f * 0.013f, 0.055f, 0.008f), Quaternion.identity, new Vector3(0.012f, 0.038f, 0.012f), pinkMat);
            }

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

        private Material GetOrCreateMaterial(string matName, Color color, float smoothness)
        {
            if (materialCache.TryGetValue(matName, out Material cached) && cached != null)
                return cached;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader) { name = matName, color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

#if UNITY_EDITOR
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Rat");
            string matPath = "Assets/Materials/Rat/" + matName + ".mat";
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

        [MenuItem("Tools/Character/Generate Rat NPC", priority = 6)]
        public static GameObject GenerateFromMenu()
        {
            GameObject ratObj = GameObject.Find("RatNPC");
            if (ratObj == null)
            {
                ratObj = new GameObject("RatNPC");
                Undo.RegisterCreatedObjectUndo(ratObj, "Create Rat NPC");
            }

            RatCharacterBuilder builder = ratObj.GetComponent<RatCharacterBuilder>();
            if (builder == null) builder = ratObj.AddComponent<RatCharacterBuilder>();

            builder.BuildCharacter();

            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "NPCs");
            string prefabPath = "Assets/Prefabs/NPCs/NPC_Rat.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(ratObj, prefabPath, InteractionMode.AutomatedAction);

            Selection.activeGameObject = ratObj;
            EditorUtility.SetDirty(ratObj);
            AssetDatabase.SaveAssets();

            Debug.Log("[RatCharacterBuilder] Saved Prefab at: " + prefabPath);
            return ratObj;
        }
#endif
    }
}
