using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NutritionKitchen.Character
{
    /// <summary>
    /// สคริปต์ประกอบและสร้างโมเดลตัวละคร 3D Chibi Chef สไตล์ "Cozy Forest Baker"
    /// มีเอกลักษณ์เฉพาะตัวที่อบอุ่นและลงตัวกับเกมทำอาหารเพื่อสุขภาพ (Nutrition Kitchen)
    /// ไม่ลอกเลียนแบบ Reference เดิม แต่คงความสมส่วน มีมิติ และน่ารักในระดับมืออาชีพ:
    /// - ผมสีคาราเมลน้ำตาลอบอุ่น นุ่มนวลพร้อมปอยผมสไตล์อนิเมะและผมกระดก (Ahoge) ขี้เล่น
    /// - หูมนธรรมชาติสไตล์ชิบิ (Natural Rounded Chibi Ears)
    /// - เสื้อแจ็กเก็ตเชฟสีเขียวเสจธรรมชาติ (Sage Green Chef Coat) พร้อมกระดุมคู่สีงาช้าง
    /// - ผ้าพันคอผูกคอสไตล์คนทำขนมปังสีมัสตาร์ดทองอบอุ่น (Golden Neckerchief Foulard)
    /// - ผ้ากันเปื้อนลินินสีครีมข้าวโอ๊ตพร้อมกระเป๋าหน้าหนังสีน้ำตาลวอลนัต
    /// - ช้อนชิมไม้แกะสลักน่ารักเหน็บไว้ในกระเป๋าผ้ากันเปื้อน (Carved Wooden Tasting Spoon)
    /// - เข็มกลัดใบสมุนไพรสีเขียวสดใสประดับบนสายรัดหมวกเชฟ (Herb Sprout Pin)
    /// - แขนเสื้อพับทบพร้อมมือเนื้อนุ่มธรรมชาติ (Bare Hands with Baker Cuffs)
    /// - รองเท้าหนังสีเชสต์นัตพร้อมพื้นยางสีครีมในท่ายืนกางปลายเท้า (Chestnut Leather Boots)
    /// </summary>
    [ExecuteInEditMode]
    public class ProceduralCharacterBuilder : MonoBehaviour
    {
        [Header("Cozy Forest Baker Palette")]
        public Color skinColor = new Color(1.0f, 0.90f, 0.83f);          // ผิวโทนเนื้อพีชอบอุ่น
        public Color hairColor = new Color(0.70f, 0.42f, 0.22f);          // ผมสีคาราเมลน้ำตาลอบอุ่น
        public Color hairShadowColor = new Color(0.52f, 0.28f, 0.14f);    // ผมเงาสีช็อกโกแลตอุ่น
        public Color jacketColor = new Color(0.42f, 0.58f, 0.46f);        // เสื้อโค้ทเชฟสีเขียวเสจธรรมชาติ (Sage Green)
        public Color neckerchiefColor = new Color(0.94f, 0.82f, 0.45f);   // ผ้าพันคอผูกคอสีมัสตาร์ดทองอบอุ่น
        public Color innerCollarColor = new Color(0.32f, 0.46f, 0.35f);   // ขอบคอเสื้อด้านในสีเขียวเข้ม
        public Color apronColor = new Color(0.96f, 0.94f, 0.88f);         // ผ้ากันเปื้อนผ้าลินินสีครีมข้าวโอ๊ต
        public Color brownAccentColor = new Color(0.44f, 0.30f, 0.22f);   // กระเป๋าหน้าผ้ากันเปื้อนและสายคาดสีน้ำตาลวอลนัต
        public Color woodSpoonColor = new Color(0.82f, 0.62f, 0.40f);     // ช้อนชิมไม้แกะสลักเหน็บกระเป๋า
        public Color leafPinColor = new Color(0.48f, 0.72f, 0.32f);       // เข็มกลัดใบสมุนไพรต้นกล้าบนหมวก
        public Color pantsColor = new Color(0.26f, 0.19f, 0.17f);         // กางเกงสีโกโก้เข้ม
        public Color bootColor = new Color(0.42f, 0.26f, 0.18f);          // รองเท้าหนังสีน้ำตาลเชสต์นัต
        public Color bootSoleColor = new Color(0.85f, 0.80f, 0.72f);      // พื้นรองเท้ายางสีครีมธรรมชาติ
        public Color hatColor = new Color(0.98f, 0.98f, 0.97f);           // หมวกคนทำขนมปังสีขาวสะอาด
        public Color eyeColor = new Color(0.22f, 0.14f, 0.10f);           // ตาสีเอสเปรสโซอบอุ่น
        public Color eyebrowColor = new Color(0.58f, 0.35f, 0.18f);       // คิ้วสีน้ำตาลเชสต์นัต
        public Color blushColor = new Color(0.94f, 0.46f, 0.42f);         // ริ้วแก้มสีพีชอมชมพู
        public Color buttonColor = new Color(0.92f, 0.88f, 0.80f);        // กระดุมสีงาช้าง/ไม้เบิร์ช

        // โครงสร้างข้อมูลช่อผม
        private struct HairLockConfig
        {
            public string name;
            public Vector3 pos;
            public Vector3 rot;
            public float width;
            public float length;
            public float curveX;
        }

        // เก็บแคชของ Material เพื่อหลีกเลี่ยงการสร้างซ้ำซ้อน
        private readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        [ContextMenu("Reset to Cozy Forest Baker Palette")]
        public void ResetToCozyForestBakerPalette()
        {
            skinColor = new Color(1.0f, 0.90f, 0.83f);
            hairColor = new Color(0.70f, 0.42f, 0.22f);
            hairShadowColor = new Color(0.52f, 0.28f, 0.14f);
            jacketColor = new Color(0.42f, 0.58f, 0.46f);
            neckerchiefColor = new Color(0.94f, 0.82f, 0.45f);
            innerCollarColor = new Color(0.32f, 0.46f, 0.35f);
            apronColor = new Color(0.96f, 0.94f, 0.88f);
            brownAccentColor = new Color(0.44f, 0.30f, 0.22f);
            woodSpoonColor = new Color(0.82f, 0.62f, 0.40f);
            leafPinColor = new Color(0.48f, 0.72f, 0.32f);
            pantsColor = new Color(0.26f, 0.19f, 0.17f);
            bootColor = new Color(0.42f, 0.26f, 0.18f);
            bootSoleColor = new Color(0.85f, 0.80f, 0.72f);
            hatColor = new Color(0.98f, 0.98f, 0.97f);
            eyeColor = new Color(0.22f, 0.14f, 0.10f);
            eyebrowColor = new Color(0.58f, 0.35f, 0.18f);
            blushColor = new Color(0.94f, 0.46f, 0.42f);
            buttonColor = new Color(0.92f, 0.88f, 0.80f);
        }

        [ContextMenu("Build Character Model")]
        public void BuildCharacter()
        {
            materialCache.Clear();

            // ลบชิ้นส่วนเดิมทั้งหมด
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }

            // 1. Root & Base Pivot
            GameObject bodyRoot = new GameObject("BodyRoot");
            bodyRoot.transform.SetParent(transform, false);

            // สร้าง Materials ที่ปรับแต่ง Smoothness ให้สมจริงตามประเภทพื้นผิว
            Material skinMat = GetOrCreateMaterial("Mat_Baker_Skin", skinColor, 0.30f);
            Material hairMat = GetOrCreateMaterial("Mat_Baker_Hair", hairColor, 0.35f);
            Material hairShadowMat = GetOrCreateMaterial("Mat_Baker_HairShadow", hairShadowColor, 0.35f);
            Material jacketMat = GetOrCreateMaterial("Mat_Baker_Jacket", jacketColor, 0.25f);
            Material neckerchiefMat = GetOrCreateMaterial("Mat_Baker_Neckerchief", neckerchiefColor, 0.22f);
            Material innerCollarMat = GetOrCreateMaterial("Mat_Baker_InnerCollar", innerCollarColor, 0.22f);
            Material apronMat = GetOrCreateMaterial("Mat_Baker_Apron", apronColor, 0.20f);
            Material brownAccentMat = GetOrCreateMaterial("Mat_Baker_BrownAccent", brownAccentColor, 0.28f);
            Material woodSpoonMat = GetOrCreateMaterial("Mat_Baker_WoodSpoon", woodSpoonColor, 0.40f);
            Material leafPinMat = GetOrCreateMaterial("Mat_Baker_LeafPin", leafPinColor, 0.35f);
            Material pantsMat = GetOrCreateMaterial("Mat_Baker_Pants", pantsColor, 0.20f);
            Material bootMat = GetOrCreateMaterial("Mat_Baker_Boot", bootColor, 0.35f);
            Material bootSoleMat = GetOrCreateMaterial("Mat_Baker_BootSole", bootSoleColor, 0.25f);
            Material hatMat = GetOrCreateMaterial("Mat_Baker_Hat", hatColor, 0.18f);
            Material eyeMat = GetOrCreateMaterial("Mat_Baker_Eye", eyeColor, 0.85f);
            Material eyeHighlightMat = GetOrCreateMaterial("Mat_Baker_EyeHighlight", Color.white, 0.90f);
            Material eyebrowMat = GetOrCreateMaterial("Mat_Baker_Eyebrow", eyebrowColor, 0.30f);
            Material blushMat = GetOrCreateMaterial("Mat_Baker_Blush", blushColor, 0.30f);
            Material buttonMat = GetOrCreateMaterial("Mat_Baker_Button", buttonColor, 0.50f);

            // 2. ลำตัวและเครื่องแต่งกาย (Torso & Cozy Forest Baker Clothing)
            GameObject body = new GameObject("Body");
            body.transform.SetParent(bodyRoot.transform, false);
            body.transform.localPosition = new Vector3(0, 0.90f, 0);

            // คอเต่าด้านใน (Inner Collar)
            CreatePrimitive(PrimitiveType.Cylinder, "InnerCollar", body.transform,
                new Vector3(0, 0.40f, 0), Quaternion.identity, new Vector3(0.32f, 0.12f, 0.32f), innerCollarMat);

            // ผ้าพันคอสไตล์คนทำขนมปังผูกคออย่างอบอุ่น (Tied Neckerchief / Foulard)
            BuildNeckerchief(body.transform, neckerchiefMat);

            // เสื้อแจ็กเก็ตเชฟสีเขียวเสจ (Sage Green Chef Coat)
            CreatePrimitive(PrimitiveType.Capsule, "JacketTorso", body.transform,
                new Vector3(0, 0.15f, 0), Quaternion.identity, new Vector3(0.60f, 0.40f, 0.46f), jacketMat);

            // ปกเสื้อพับทบสไตล์เชฟ (Lapels)
            CreatePrimitive(PrimitiveType.Cube, "LeftLapel", body.transform,
                new Vector3(-0.08f, 0.22f, 0.21f), Quaternion.Euler(14f, 14f, -24f), new Vector3(0.12f, 0.20f, 0.05f), jacketMat);
            CreatePrimitive(PrimitiveType.Cube, "RightLapel", body.transform,
                new Vector3(0.08f, 0.22f, 0.21f), Quaternion.Euler(14f, -14f, 24f), new Vector3(0.12f, 0.20f, 0.05f), jacketMat);

            // กระดุมคู่สีงาช้างสวยงาม 2 คู่ (Double-Breasted Ivory Buttons)
            CreatePrimitive(PrimitiveType.Cylinder, "Button_TL", body.transform,
                new Vector3(-0.06f, 0.18f, 0.23f), Quaternion.Euler(85f, 0, 0), new Vector3(0.045f, 0.02f, 0.045f), buttonMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Button_TR", body.transform,
                new Vector3(0.06f, 0.18f, 0.23f), Quaternion.Euler(85f, 0, 0), new Vector3(0.045f, 0.02f, 0.045f), buttonMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Button_BL", body.transform,
                new Vector3(-0.06f, 0.08f, 0.23f), Quaternion.Euler(85f, 0, 0), new Vector3(0.045f, 0.02f, 0.045f), buttonMat);
            CreatePrimitive(PrimitiveType.Cylinder, "Button_BR", body.transform,
                new Vector3(0.06f, 0.08f, 0.23f), Quaternion.Euler(85f, 0, 0), new Vector3(0.045f, 0.02f, 0.045f), buttonMat);

            // แถบเข็มขัด/ผูกเอวสีน้ำตาลวอลนัต (Brown Waistband / Sash)
            CreatePrimitive(PrimitiveType.Cylinder, "WaistBand", body.transform,
                new Vector3(0, -0.06f, 0), Quaternion.identity, new Vector3(0.62f, 0.10f, 0.48f), brownAccentMat);

            // ผ้ากันเปื้อนลินินสีครีมข้าวโอ๊ตพร้อมกระเป๋าหน้าและช้อนชิมไม้ (Apron with Tasting Spoon)
            BuildApronWithSpoon(body.transform, apronMat, brownAccentMat, woodSpoonMat);

            // 3. ส่วนหัว (Head, Cozy Hair, Natural Ears, Face & Baker Hat with Herb Pin)
            GameObject head = new GameObject("Head");
            head.transform.SetParent(bodyRoot.transform, false);
            head.transform.localPosition = new Vector3(0, 1.55f, 0);

            // กะโหลกศีรษะทรงชิบิกลมแป้นเรียบเนียนนุ่มนวล (Smooth Chibi Head)
            CreatePrimitive(PrimitiveType.Sphere, "HeadMesh", head.transform,
                new Vector3(0, 0.00f, 0), Quaternion.identity, new Vector3(0.88f, 0.78f, 0.82f), skinMat);

            // หูมนธรรมชาติสไตล์ชิบิ (Natural Rounded Ears)
            BuildNaturalEars(head.transform, skinMat);

            // ใบหน้า: ตา, แววตา, คิ้วสีน้ำตาลเป็นมิตร, ริ้วแก้มชมพู, ปากยิ้มอบอุ่น, จมูกเล็ก
            BuildFaceFeatures(head.transform, eyeMat, eyeHighlightMat, eyebrowMat, blushMat, skinMat);

            // ทรงผมสีคาราเมลน้ำตาลอบอุ่น นุ่มนวลพร้อมผมกระดก Ahoge (Cozy Caramel Anime Hair)
            BuildCozyCaramelHair(head.transform, hairMat, hairShadowMat);

            // หมวกคนทำขนมปังฟูทรงเมฆพร้อมเข็มกลัดใบสมุนไพรต้นกล้า (Puffy Baker Toque with Herb Pin)
            BuildPuffyBakerHat(head.transform, hatMat, leafPinMat);

            // 4. แขนเสื้อพับทบและมือเนื้อนุ่มธรรมชาติ (Arms, Folded Sleeves & Bare Hands)
            GameObject leftArm = BuildArm(bodyRoot.transform, isLeft: true, jacketMat, skinMat, buttonMat);
            GameObject rightArm = BuildArm(bodyRoot.transform, isLeft: false, jacketMat, skinMat, buttonMat);

            // 5. ขาและรองเท้าหนังสีน้ำตาลเชสต์นัต (Legs & Chestnut Leather Boots)
            GameObject leftLeg = BuildLeg(bodyRoot.transform, isLeft: true, pantsMat, bootMat, bootSoleMat);
            GameObject rightLeg = BuildLeg(bodyRoot.transform, isLeft: false, pantsMat, bootMat, bootSoleMat);

            // 6. เชื่อมต่อเข้ากับ SimpleCharacterAnimator
            SimpleCharacterAnimator animator = gameObject.GetComponent<SimpleCharacterAnimator>();
            if (animator == null) animator = gameObject.AddComponent<SimpleCharacterAnimator>();

            animator.bodyRoot = bodyRoot.transform;
            animator.head = head.transform;
            animator.leftArm = leftArm.transform;
            animator.rightArm = rightArm.transform;
            animator.leftLeg = leftLeg.transform;
            animator.rightLeg = rightLeg.transform;

            Debug.Log("[ProceduralCharacterBuilder] โมเดลตัวละคร 3D Chibi Chef สไตล์ 'Cozy Forest Baker' ถูกสร้างเสร็จสมบูรณ์เรียบร้อย!");
        }

        #region Natural Rounded Ears

        private void BuildNaturalEars(Transform headTransform, Material skinMat)
        {
            Material innerEarMat = GetOrCreateMaterial("Mat_Baker_InnerEar", Color.Lerp(skinColor, new Color(0.96f, 0.55f, 0.55f), 0.25f), 0.30f);

            for (int i = -1; i <= 1; i += 2)
            {
                string earName = i < 0 ? "LeftEar" : "RightEar";
                GameObject earObj = new GameObject(earName);
                earObj.transform.SetParent(headTransform, false);
                earObj.transform.localPosition = new Vector3(i * 0.42f, -0.04f, 0.02f);
                earObj.transform.localRotation = Quaternion.Euler(4f, i * -15f, i * 4f);

                // ใบหูมนธรรมชาติสไตล์ชิบิ (Natural Rounded Ear Shell)
                CreatePrimitive(PrimitiveType.Sphere, "EarShell", earObj.transform,
                    Vector3.zero, Quaternion.identity, new Vector3(0.12f, 0.18f, 0.14f), skinMat);

                // ร่องด้านในใบหู (Inner Ear Cavity)
                CreatePrimitive(PrimitiveType.Sphere, "InnerEar", earObj.transform,
                    new Vector3(i * 0.02f, 0, 0.02f), Quaternion.identity, new Vector3(0.07f, 0.12f, 0.08f), innerEarMat);
            }
        }

        #endregion

        #region Neckerchief (Foulard)

        private void BuildNeckerchief(Transform bodyTransform, Material neckerchiefMat)
        {
            GameObject scarfRoot = new GameObject("Neckerchief");
            scarfRoot.transform.SetParent(bodyTransform, false);
            scarfRoot.transform.localPosition = new Vector3(0, 0.36f, 0);

            // ปลอกคอผ้าพันคอโอบรอบคอ (Collar Ring)
            CreatePrimitive(PrimitiveType.Cylinder, "ScarfCollar", scarfRoot.transform,
                Vector3.zero, Quaternion.identity, new Vector3(0.36f, 0.08f, 0.36f), neckerchiefMat);

            // ปมเงื่อนผ้าพันคอด้านหน้า (Front Scarf Knot)
            CreatePrimitive(PrimitiveType.Sphere, "ScarfKnot", scarfRoot.transform,
                new Vector3(0, -0.02f, 0.20f), Quaternion.identity, new Vector3(0.09f, 0.09f, 0.08f), neckerchiefMat);

            // หางผ้าพันคอทิ้งชายลงมา 2 ชาย (Dangling Scarf Tails)
            CreatePrimitive(PrimitiveType.Cube, "ScarfTailLeft", scarfRoot.transform,
                new Vector3(-0.04f, -0.11f, 0.21f), Quaternion.Euler(14f, -12f, -18f), new Vector3(0.06f, 0.16f, 0.03f), neckerchiefMat);
            CreatePrimitive(PrimitiveType.Cube, "ScarfTailRight", scarfRoot.transform,
                new Vector3(0.04f, -0.10f, 0.21f), Quaternion.Euler(14f, 12f, 15f), new Vector3(0.05f, 0.14f, 0.03f), neckerchiefMat);
        }

        #endregion

        #region Apron with Tasting Spoon

        private void BuildApronWithSpoon(Transform bodyTransform, Material apronMat, Material brownAccentMat, Material woodSpoonMat)
        {
            GameObject apronRoot = new GameObject("Apron");
            apronRoot.transform.SetParent(bodyTransform, false);
            apronRoot.transform.localPosition = new Vector3(0, -0.08f, 0);

            // โครงผ้ากันเปื้อนส่วนบน-กลางรอบเอวและสะโพก (Flared Curved Half-Skirt Wrap)
            GameObject apronFront = CreatePrimitive(PrimitiveType.Cube, "ApronFront", apronRoot.transform,
                new Vector3(0, -0.26f, 0.17f), Quaternion.Euler(7f, 0, 0), new Vector3(0.60f, 0.48f, 0.16f), apronMat);

            // ขอบชายผ้ากันเปื้อนด้านล่างที่บานออกเล็กน้อย (A-Line Flared Hem)
            CreatePrimitive(PrimitiveType.Cube, "ApronHem", apronFront.transform,
                new Vector3(0, -0.23f, 0.02f), Quaternion.identity, new Vector3(1.08f, 0.15f, 1.14f), apronMat);

            // ปีกด้านข้างผ้ากันเปื้อนโอบรอบเอว (Side Wraps)
            CreatePrimitive(PrimitiveType.Cube, "ApronLeftFlap", apronRoot.transform,
                new Vector3(-0.26f, -0.22f, 0.06f), Quaternion.Euler(6f, -28f, 2f), new Vector3(0.18f, 0.44f, 0.32f), apronMat);
            CreatePrimitive(PrimitiveType.Cube, "ApronRightFlap", apronRoot.transform,
                new Vector3(0.26f, -0.22f, 0.06f), Quaternion.Euler(6f, 28f, -2f), new Vector3(0.18f, 0.44f, 0.32f), apronMat);

            // กระเป๋าผ้ากันเปื้อนสีน้ำตาลวอลนัตขอบมนด้านหน้า (Brown Front Pocket)
            GameObject pocket = CreatePrimitive(PrimitiveType.Cube, "FrontPocket", apronFront.transform,
                new Vector3(0, -0.05f, 0.52f), Quaternion.identity, new Vector3(0.50f, 0.46f, 0.12f), brownAccentMat);

            // ขอบกระเป๋ามนล่างทรงตัว U (Pocket Rounded U-Base)
            CreatePrimitive(PrimitiveType.Cylinder, "PocketRoundBase", pocket.transform,
                new Vector3(0, -0.45f, 0), Quaternion.Euler(90f, 0, 0), new Vector3(0.96f, 0.5f, 0.35f), brownAccentMat);

            // ช้อนชิมไม้แกะสลักเหน็บอยู่ในกระเป๋าผ้ากันเปื้อน (Carved Wooden Tasting Spoon)
            GameObject spoon = new GameObject("WoodenTastingSpoon");
            spoon.transform.SetParent(pocket.transform, false);
            spoon.transform.localPosition = new Vector3(0.18f, 0.15f, 0.20f);
            spoon.transform.localRotation = Quaternion.Euler(6f, -12f, 16f);

            // ด้ามช้อนไม้ (Spoon Handle)
            CreatePrimitive(PrimitiveType.Cylinder, "SpoonHandle", spoon.transform,
                new Vector3(0, 0.35f, 0), Quaternion.identity, new Vector3(0.08f, 0.65f, 0.08f), woodSpoonMat);

            // ปลายตักช้อนไม้ทรงรี (Spoon Scoop Head)
            CreatePrimitive(PrimitiveType.Sphere, "SpoonScoop", spoon.transform,
                new Vector3(0, 0.85f, 0), Quaternion.Euler(0, 0, 0), new Vector3(0.26f, 0.38f, 0.14f), woodSpoonMat);

            // โบว์ผูกผ้ากันเปื้อนด้านหลัง (Apron Back Tie Knot & Tails)
            CreatePrimitive(PrimitiveType.Sphere, "TieKnot", apronRoot.transform,
                new Vector3(0, 0.02f, -0.25f), Quaternion.identity, new Vector3(0.11f, 0.09f, 0.09f), apronMat);
            CreatePrimitive(PrimitiveType.Cube, "TieTailLeft", apronRoot.transform,
                new Vector3(-0.06f, -0.11f, -0.26f), Quaternion.Euler(15f, -10f, -15f), new Vector3(0.04f, 0.20f, 0.02f), apronMat);
            CreatePrimitive(PrimitiveType.Cube, "TieTailRight", apronRoot.transform,
                new Vector3(0.06f, -0.11f, -0.26f), Quaternion.Euler(15f, 10f, 15f), new Vector3(0.04f, 0.20f, 0.02f), apronMat);
        }

        #endregion

        #region Face Features

        private void BuildFaceFeatures(Transform headTransform, Material eyeMat, Material eyeHighlightMat, Material eyebrowMat, Material blushMat, Material skinMat)
        {
            GameObject faceRoot = new GameObject("FaceFeatures");
            faceRoot.transform.SetParent(headTransform, false);

            // จมูกกลมเล็กชิบิน่ารัก (Cute Tiny Nose)
            CreatePrimitive(PrimitiveType.Sphere, "Nose", faceRoot.transform,
                new Vector3(0, -0.06f, 0.430f), Quaternion.identity, new Vector3(0.045f, 0.040f, 0.035f), skinMat);

            // ปากยิ้มอบอุ่นและเป็นมิตรสไตล์ Cozy Forest Baker (Warm Gentle Smile)
            Material mouthMat = GetOrCreateMaterial("Mat_Baker_Mouth", new Color(0.20f, 0.12f, 0.09f), 0.40f);
            GameObject mouthObj = CreatePrimitive(PrimitiveType.Capsule, "WarmSmile", faceRoot.transform,
                new Vector3(0, -0.13f, 0.412f), Quaternion.Euler(-5f, 0, 90f), new Vector3(0.020f, 0.075f, 0.022f), mouthMat);

            // มุมปากยิ้มยกขึ้นทั้งสองข้างอย่างสดใส
            CreatePrimitive(PrimitiveType.Sphere, "SmileCornerL", mouthObj.transform,
                new Vector3(0, -0.45f, 0.03f), Quaternion.identity, new Vector3(1.1f, 0.8f, 1.1f), mouthMat);
            CreatePrimitive(PrimitiveType.Sphere, "SmileCornerR", mouthObj.transform,
                new Vector3(0, 0.45f, 0.03f), Quaternion.identity, new Vector3(1.1f, 0.8f, 1.1f), mouthMat);

            // ตา, ประกายตา, คิ้วสีน้ำตาลเป็นมิตร และริ้วแก้มชมพู (ซ้าย-ขวา)
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";

                // ดวงตากลมโตสีน้ำตาลเอสเปรสโซอบอุ่น (Warm Espresso Anime Eye)
                GameObject eye = CreatePrimitive(PrimitiveType.Sphere, side + "Eye", faceRoot.transform,
                    new Vector3(i * 0.19f, -0.04f, 0.400f), Quaternion.identity, new Vector3(0.125f, 0.145f, 0.05f), eyeMat);

                // ประกายแววตาสีขาวด้านบนขวา (Glossy Eye Highlight)
                CreatePrimitive(PrimitiveType.Sphere, side + "EyeHighlight", eye.transform,
                    new Vector3(0.28f, 0.28f, 0.45f), Quaternion.identity, new Vector3(0.32f, 0.32f, 0.25f), eyeHighlightMat);

                // คิ้วสีน้ำตาลเชสต์นัตทรงโค้งเป็นมิตรอบอุ่น (Friendly Curved Eyebrows)
                CreatePrimitive(PrimitiveType.Capsule, side + "Eyebrow", faceRoot.transform,
                    new Vector3(i * 0.19f, 0.075f, 0.415f), Quaternion.Euler(-10f, i * 10f, i * -30f), new Vector3(0.038f, 0.130f, 0.032f), eyebrowMat);

                // พวงแก้มสีพีชชมพูน่ารัก (Rosy Peach Blush Cheeks)
                CreatePrimitive(PrimitiveType.Sphere, side + "BlushOval", faceRoot.transform,
                    new Vector3(i * 0.32f, -0.075f, 0.35f), Quaternion.Euler(0, i * 30f, 0), new Vector3(0.08f, 0.05f, 0.03f), blushMat);
            }
        }

        #endregion

        #region Cozy Caramel Anime Hair

        private void BuildCozyCaramelHair(Transform headTransform, Material hairMat, Material hairShadowMat)
        {
            GameObject hairRoot = new GameObject("HairRoot");
            hairRoot.transform.SetParent(headTransform, false);

            // 1. กะโหลกผมด้านหลังและด้านข้าง (Hair Base Skullcap)
            CreatePrimitive(PrimitiveType.Sphere, "HairCapBase", hairRoot.transform,
                new Vector3(0, 0.12f, -0.05f), Quaternion.identity, new Vector3(0.88f, 0.74f, 0.86f), hairMat);

            // ท้ายทอยและโคนผมด้านหลัง (Nape Volume)
            CreatePrimitive(PrimitiveType.Sphere, "HairNape", hairRoot.transform,
                new Vector3(0, -0.08f, -0.34f), Quaternion.identity, new Vector3(0.70f, 0.38f, 0.36f), hairShadowMat);

            // 2. ปอยผมข้างแก้มด้านหน้าหูนุ่มนวลเป็นลอน (Soft Wavy Sideburns)
            for (int i = -1; i <= 1; i += 2)
            {
                string side = i < 0 ? "Left" : "Right";
                Mesh sideburnMesh = CreateFacetedHairLockMesh("BakerSideburn_" + side, 0.13f, 0.20f, 0.07f, 0.035f, i * 0.015f);
                CreateCustomMeshPart("SideburnLock_" + side, hairRoot.transform,
                    new Vector3(i * 0.38f, 0.04f, 0.18f), Quaternion.Euler(-8f, i * 20f, i * 12f), Vector3.one, sideburnMesh, hairMat);
            }

            // 3. หน้าม้าสไตล์คนทำขนมปังอบอุ่น นุ่มนวล แสกกลางอย่างเป็นธรรมชาติ (Cozy Baker Bangs)
            // ช่อผมหน้าม้าที่โค้งรับกับใบหน้าอย่างนุ่มนวล พร้อมเปิดแนวสายตาให้สดใส
            HairLockConfig[] bangs = new HairLockConfig[]
            {
                // ช่อซ้ายสุด
                new HairLockConfig { name = "CozyBang_0", pos = new Vector3(-0.28f, 0.25f, 0.33f), rot = new Vector3(-16f, 26f, 20f), width = 0.12f, length = 0.16f, curveX = -0.02f },
                // ช่อซ้ายกลาง
                new HairLockConfig { name = "CozyBang_1", pos = new Vector3(-0.19f, 0.24f, 0.37f), rot = new Vector3(-20f, 14f, 10f), width = 0.13f, length = 0.17f, curveX = -0.01f },
                // ช่อซ้ายใน
                new HairLockConfig { name = "CozyBang_2", pos = new Vector3(-0.09f, 0.24f, 0.39f), rot = new Vector3(-22f, 4f, 2f), width = 0.12f, length = 0.15f, curveX = 0f },
                // ช่อแสกกลางเปิดหน้าผาก
                new HairLockConfig { name = "CozyBang_3", pos = new Vector3(0.01f, 0.26f, 0.40f), rot = new Vector3(-22f, -2f, -2f), width = 0.10f, length = 0.12f, curveX = 0.01f },
                // ช่อขวาใน
                new HairLockConfig { name = "CozyBang_4", pos = new Vector3(0.10f, 0.24f, 0.39f), rot = new Vector3(-22f, -10f, -8f), width = 0.12f, length = 0.15f, curveX = 0.01f },
                // ช่อขวากลาง
                new HairLockConfig { name = "CozyBang_5", pos = new Vector3(0.19f, 0.24f, 0.37f), rot = new Vector3(-20f, -20f, -16f), width = 0.13f, length = 0.17f, curveX = 0.01f },
                // ช่อขวาสุด
                new HairLockConfig { name = "CozyBang_6", pos = new Vector3(0.28f, 0.25f, 0.33f), rot = new Vector3(-16f, -28f, -22f), width = 0.12f, length = 0.16f, curveX = 0.02f }
            };

            for (int b = 0; b < bangs.Length; b++)
            {
                Mesh lockMesh = CreateFacetedHairLockMesh(bangs[b].name + "_Mesh", bangs[b].width, bangs[b].length, 0.06f, 0.035f, bangs[b].curveX);
                CreateCustomMeshPart(bangs[b].name, hairRoot.transform,
                    bangs[b].pos, Quaternion.Euler(bangs[b].rot), Vector3.one, lockMesh, hairMat);
            }

            // ปอยผมกระดกด้านบนขี้เล่นสไตล์ชิบิ (Cute Playful Ahoge Cowlick)
            Mesh ahogeMesh = CreateFacetedHairLockMesh("HairAhoge_Mesh", 0.08f, 0.16f, 0.04f, 0.025f, 0.04f);
            CreateCustomMeshPart("PlayfulAhoge", hairRoot.transform,
                new Vector3(0.04f, 0.35f, 0.32f), Quaternion.Euler(35f, 15f, 25f), Vector3.one, ahogeMesh, hairShadowMat);
        }

        #endregion

        #region Puffy Baker Hat with Herb Sprout Pin

        private void BuildPuffyBakerHat(Transform headTransform, Material hatMat, Material leafPinMat)
        {
            GameObject hatRoot = new GameObject("PuffyBakerHat");
            hatRoot.transform.SetParent(headTransform, false);

            // เอียงหมวกไปข้างหลังเล็กน้อยอย่างผ่อนคลาย (Cozy Baker Tilt)
            hatRoot.transform.localPosition = new Vector3(0.01f, 0.38f, -0.06f);
            hatRoot.transform.localRotation = Quaternion.Euler(15f, 5f, -3f);

            // 1. สายรัดฐานหมวก (Hat Band Rim) ทรงกระบอกโอบรอบศีรษะ
            CreatePrimitive(PrimitiveType.Cylinder, "HatBand", hatRoot.transform,
                new Vector3(0, 0.06f, 0), Quaternion.identity, new Vector3(0.82f, 0.12f, 0.82f), hatMat);

            // ขอบบัวล่างรอบสายรัดหมวก (Lower Trim Ring)
            CreatePrimitive(PrimitiveType.Cylinder, "HatBandTrim", hatRoot.transform,
                new Vector3(0, 0.00f, 0), Quaternion.identity, new Vector3(0.85f, 0.035f, 0.85f), hatMat);

            // เข็มกลัดใบสมุนไพรต้นกล้าสีเขียวสดใสบนสายรัดหมวก (Herb Sprout Leaf Pin)
            GameObject badge = new GameObject("HerbSproutBadge");
            badge.transform.SetParent(hatRoot.transform, false);
            badge.transform.localPosition = new Vector3(-0.35f, 0.08f, 0.24f);
            badge.transform.localRotation = Quaternion.Euler(10f, -45f, 15f);

            // ฐานเข็มกลัดกลมสีทอง
            Material goldPinMat = GetOrCreateMaterial("Mat_Baker_GoldPin", new Color(0.92f, 0.78f, 0.28f), 0.70f);
            CreatePrimitive(PrimitiveType.Cylinder, "PinBase", badge.transform,
                Vector3.zero, Quaternion.Euler(90f, 0, 0), new Vector3(0.07f, 0.02f, 0.07f), goldPinMat);

            // ใบไม้สมุนไพร 2 ใบโผล่ออกมา (2 Sprout Leaves)
            CreatePrimitive(PrimitiveType.Sphere, "SproutLeaf1", badge.transform,
                new Vector3(-0.02f, 0.05f, 0.01f), Quaternion.Euler(0, 0, 35f), new Vector3(0.04f, 0.09f, 0.02f), leafPinMat);
            CreatePrimitive(PrimitiveType.Sphere, "SproutLeaf2", badge.transform,
                new Vector3(0.03f, 0.04f, 0.01f), Quaternion.Euler(0, 0, -30f), new Vector3(0.035f, 0.08f, 0.02f), leafPinMat);

            // 2. หมวกทรงฟูพองกลีบเมฆขนาดใหญ่ (Voluminous Cloud Toque Crown)
            GameObject crownRoot = new GameObject("HatCrownPuffs");
            crownRoot.transform.SetParent(hatRoot.transform, false);
            crownRoot.transform.localPosition = new Vector3(0, 0.14f, 0);

            // โดมแกนกลางหมวก (Central Crown Dome)
            CreatePrimitive(PrimitiveType.Sphere, "CentralDome", crownRoot.transform,
                new Vector3(0, 0.36f, 0), Quaternion.identity, new Vector3(1.15f, 0.74f, 1.15f), hatMat);

            // 7 กลีบจีบพองฟูรอบทิศทางขนาดใหญ่ (7 Large Radiating Billowy Toque Lobes)
            float lobeRadius = 0.44f;
            int lobeCount = 7;
            for (int i = 0; i < lobeCount; i++)
            {
                float angle = i * (360f / lobeCount) * Mathf.Deg2Rad;
                float lx = Mathf.Cos(angle) * lobeRadius;
                float lz = Mathf.Sin(angle) * lobeRadius;

                float ly = 0.32f + ((i % 2 == 0) ? 0.05f : -0.03f);
                Vector3 lobeScale = new Vector3(0.66f, 0.54f, 0.66f);

                CreatePrimitive(PrimitiveType.Sphere, "PuffLobe_" + i, crownRoot.transform,
                    new Vector3(lx, ly, lz), Quaternion.identity, lobeScale, hatMat);
            }

            // จีบพองเสริมด้านบนให้ยอดหมวกฟูเป็นลอนนุ่มนวล (Top Billow Accents)
            CreatePrimitive(PrimitiveType.Sphere, "TopBillowLeft", crownRoot.transform,
                new Vector3(-0.18f, 0.56f, -0.05f), Quaternion.identity, new Vector3(0.72f, 0.48f, 0.72f), hatMat);
            CreatePrimitive(PrimitiveType.Sphere, "TopBillowRight", crownRoot.transform,
                new Vector3(0.20f, 0.54f, 0.06f), Quaternion.identity, new Vector3(0.70f, 0.46f, 0.70f), hatMat);
        }

        #endregion

        #region Arms & Bare Hands

        private GameObject BuildArm(Transform bodyRoot, bool isLeft, Material jacketMat, Material skinMat, Material buttonMat)
        {
            float sideSign = isLeft ? -1f : 1f;
            string armName = isLeft ? "LeftArm" : "RightArm";

            GameObject armObj = new GameObject(armName);
            armObj.transform.SetParent(bodyRoot, false);
            armObj.transform.localPosition = new Vector3(sideSign * 0.43f, 1.14f, 0.02f);
            // ท่าทางพักแขนผ่อนคลายแนบข้างลำตัวอย่างเป็นธรรมชาติ
            armObj.transform.localRotation = Quaternion.Euler(8f, sideSign * -5f, sideSign * 8f);

            // แขนเสื้อเชฟสีเขียวเสจพับทบ (Rolled Sage-Green Sleeve)
            CreatePrimitive(PrimitiveType.Capsule, "SleeveMesh", armObj.transform,
                new Vector3(0, -0.14f, 0), Quaternion.identity, new Vector3(0.24f, 0.20f, 0.24f), jacketMat);

            // ปลายแขนเสื้อพับทบขึ้น (Rolled Sleeve Cuff Rim)
            CreatePrimitive(PrimitiveType.Cylinder, "SleeveCuff", armObj.transform,
                new Vector3(0, -0.23f, 0), Quaternion.identity, new Vector3(0.26f, 0.045f, 0.26f), jacketMat);

            // ท่อนแขนเปลือยสีผิวเนื้อนุ่มนวล (Bare Forearm)
            CreatePrimitive(PrimitiveType.Capsule, "Forearm", armObj.transform,
                new Vector3(0, -0.32f, 0), Quaternion.identity, new Vector3(0.18f, 0.16f, 0.18f), skinMat);

            // ข้อมือเสื้อคนทำขนมปัง/สายรัดข้อมือสีเบจอบอุ่น (Baker Wristband)
            CreatePrimitive(PrimitiveType.Cylinder, "WristBand", armObj.transform,
                new Vector3(0, -0.39f, 0), Quaternion.identity, new Vector3(0.20f, 0.035f, 0.20f), buttonMat);

            // มือชิบิสีเนื้อนุ่มนวลธรรมชาติ (Cute Chibi Bare Hand)
            GameObject hand = CreatePrimitive(PrimitiveType.Sphere, isLeft ? "LeftHand" : "RightHand", armObj.transform,
                new Vector3(0, -0.47f, 0), Quaternion.identity, new Vector3(0.20f, 0.20f, 0.16f), skinMat);

            // นิ้วโป้งสีเนื้อหันเข้าด้านหน้า (Thumb)
            CreatePrimitive(PrimitiveType.Capsule, "Thumb", hand.transform,
                new Vector3(sideSign * 0.38f, 0.10f, 0.30f), Quaternion.Euler(-20f, sideSign * 35f, sideSign * -25f), new Vector3(0.38f, 0.52f, 0.38f), skinMat);

            // นิ้วมือทั้ง 4 เรียงตัวเป็นร่องสวยงามตามสไตล์การ์ตูน (4 Chibi Fingers)
            float[] fingerOffsetsX = { -0.27f, -0.09f, 0.09f, 0.27f };
            float[] fingerLengths = { 0.52f, 0.62f, 0.58f, 0.48f };
            for (int f = 0; f < 4; f++)
            {
                CreatePrimitive(PrimitiveType.Capsule, "Finger_" + f, hand.transform,
                    new Vector3(fingerOffsetsX[f], -0.36f, 0.02f), Quaternion.Euler(8f, 0, 0), new Vector3(0.25f, fingerLengths[f], 0.25f), skinMat);
            }

            return armObj;
        }

        #endregion

        #region Legs & Chestnut Leather Boots

        private GameObject BuildLeg(Transform bodyRoot, bool isLeft, Material pantsMat, Material bootMat, Material bootSoleMat)
        {
            float sideSign = isLeft ? -1f : 1f;
            string legName = isLeft ? "LeftLeg" : "RightLeg";

            GameObject legObj = new GameObject(legName);
            legObj.transform.SetParent(bodyRoot, false);
            legObj.transform.localPosition = new Vector3(sideSign * 0.18f, 0.50f, 0);
            // ปลายเท้าบิดออกเล็กน้อยเพิ่มบุคลิกชิบิที่มั่นคงน่ารัก (V-Stance)
            legObj.transform.localRotation = Quaternion.Euler(0, sideSign * 8f, 0);

            // ท่อนขากางเกงสีโกโก้เข้ม (Dark Cocoa Pants)
            CreatePrimitive(PrimitiveType.Capsule, "LegMesh", legObj.transform,
                new Vector3(0, -0.14f, 0), Quaternion.identity, new Vector3(0.20f, 0.22f, 0.20f), pantsMat);

            // ขอบข้อเท้ารองเท้าหนัง (Leather Boot Collar)
            CreatePrimitive(PrimitiveType.Cylinder, "BootCollar", legObj.transform,
                new Vector3(0, -0.30f, 0), Quaternion.identity, new Vector3(0.24f, 0.08f, 0.24f), bootMat);

            // หัวรองเท้าหนังสีน้ำตาลเชสต์นัตทรงกลมหนานุ่ม (Chunky Chestnut Boot Foot)
            CreatePrimitive(PrimitiveType.Sphere, isLeft ? "LeftFoot" : "RightFoot", legObj.transform,
                new Vector3(0, -0.42f, 0.07f), Quaternion.identity, new Vector3(0.26f, 0.18f, 0.35f), bootMat);

            // ส้นรองเท้าด้านหลัง (Boot Heel)
            CreatePrimitive(PrimitiveType.Sphere, "BootHeel", legObj.transform,
                new Vector3(0, -0.42f, -0.04f), Quaternion.identity, new Vector3(0.24f, 0.18f, 0.22f), bootMat);

            // พื้นรองเท้ายางสีครีมธรรมชาติกันลื่นใต้บูท (Natural Gum Sole Plate)
            CreatePrimitive(PrimitiveType.Cube, "BootSole", legObj.transform,
                new Vector3(0, -0.49f, 0.05f), Quaternion.identity, new Vector3(0.26f, 0.04f, 0.38f), bootSoleMat);

            return legObj;
        }

        #endregion

        #region Helpers & Procedural Mesh Generators

        /// <summary>
        /// สร้างโมเดลช่อผมหน้าม้าอนิเมะทรงเหลี่ยม (Faceted Anime Hair Strand Mesh)
        /// มีสันนูนตรงกลาง (Front Ridge) และเหลี่ยมด้านข้าง ทำให้รับแสงเงาได้อย่างคมชัด
        /// </summary>
        private Mesh CreateFacetedHairLockMesh(string meshName, float width, float length, float thickness, float ridgeHeight, float curveX)
        {
            Mesh mesh = new Mesh { name = meshName };

            Vector3[] verts = new Vector3[]
            {
                // 0: Root Top Center (สันบน)
                new Vector3(0, 0, ridgeHeight),
                // 1: Root Top Left
                new Vector3(-width * 0.5f, 0, 0),
                // 2: Root Top Right
                new Vector3(width * 0.5f, 0, 0),
                // 3: Root Top Back
                new Vector3(0, 0, -thickness * 0.5f),

                // 4: Mid Center (สันกลาง)
                new Vector3(curveX * 0.4f, -length * 0.5f, ridgeHeight * 0.85f),
                // 5: Mid Left
                new Vector3(-width * 0.45f + curveX * 0.3f, -length * 0.5f, 0),
                // 6: Mid Right
                new Vector3(width * 0.45f + curveX * 0.3f, -length * 0.5f, 0),
                // 7: Mid Back
                new Vector3(curveX * 0.3f, -length * 0.5f, -thickness * 0.4f),

                // 8: Tip (ปลายแหลม)
                new Vector3(curveX, -length, 0)
            };

            int[] tris = new int[]
            {
                // ด้านหน้าซ้ายบน
                0, 1, 4,   1, 5, 4,
                // ด้านหน้าขวาบน
                0, 4, 2,   2, 4, 6,
                // ด้านหน้าซ้ายล่าง
                4, 5, 8,
                // ด้านหน้าขวาล่าง
                4, 8, 6,
                // ฝาปิดด้านบน
                0, 2, 3,   0, 3, 1,
                // ด้านหลังซ้าย
                1, 3, 5,   3, 7, 5,   5, 7, 8,
                // ด้านหลังขวา
                3, 2, 7,   2, 6, 7,   7, 6, 8
            };

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

#if UNITY_EDITOR
            EnsureFolder("Assets", "Meshes");
            EnsureFolder("Assets/Meshes", "Chef");
            string assetPath = "Assets/Meshes/Chef/" + meshName + ".asset";
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(mesh, assetPath);
#endif
            return mesh;
        }

        private GameObject CreateCustomMeshPart(string partName, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale, Mesh mesh, Material mat)
        {
            GameObject part = new GameObject(partName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPos;
            part.transform.localRotation = localRot;
            part.transform.localScale = localScale;

            MeshFilter filter = part.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;

            return part;
        }

        private GameObject CreatePrimitive(PrimitiveType type, string partName, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale, Material mat)
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
            if (shader == null) shader = Shader.Find("Diffuse");

            Material mat = new Material(shader)
            {
                name = matName,
                color = color
            };

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);

#if UNITY_EDITOR
            // บันทึก Material ลงใน Assets/Materials/Chef เพื่อความคงทนและไม่รั่วไหลในโปรเจกต์
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets/Materials", "Chef");
            string matPath = "Assets/Materials/Chef/" + matName + ".mat";
            Material existingAsset = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existingAsset != null)
            {
                existingAsset.color = color;
                if (existingAsset.HasProperty("_BaseColor")) existingAsset.SetColor("_BaseColor", color);
                if (existingAsset.HasProperty("_Smoothness")) existingAsset.SetFloat("_Smoothness", smoothness);
                materialCache[matName] = existingAsset;
                return existingAsset;
            }
            else
            {
                AssetDatabase.CreateAsset(mat, matPath);
            }
#endif
            materialCache[matName] = mat;
            return mat;
        }

#if UNITY_EDITOR
        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        [MenuItem("Tools/Character/Generate Cozy Chef Model", priority = 10)]
        public static void GenerateFromMenu()
        {
            GameObject characterObj = GameObject.Find("CozyChefCharacter");
            if (characterObj == null)
            {
                characterObj = new GameObject("CozyChefCharacter");
                Undo.RegisterCreatedObjectUndo(characterObj, "Create Cozy Chef Character");
            }

            ProceduralCharacterBuilder builder = characterObj.GetComponent<ProceduralCharacterBuilder>();
            if (builder == null) builder = characterObj.AddComponent<ProceduralCharacterBuilder>();

            builder.ResetToCozyForestBakerPalette();
            builder.BuildCharacter();
            Selection.activeGameObject = characterObj;
            EditorUtility.SetDirty(characterObj);
        }
#endif
        #endregion
    }
}
