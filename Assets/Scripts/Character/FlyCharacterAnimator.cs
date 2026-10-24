using UnityEngine;

namespace NutritionKitchen.Character
{
    public enum FlyAnimationState
    {
        HoverAndRub,  // บินลอยตัวในอากาศ ปีกกระพือเร็ว ถูขาหน้าไปมา (ค่าเริ่มต้น)
        PerchedRub,   // เกาะอยู่กับที่ ถูขาหน้าวางแผน
        BuzzDart      // บินโฉบไปมาอย่างรวดเร็ว
    }

    /// <summary>
    /// ควบคุมแอนิเมชันและท่าทางของ Fly NPC (แมลงวัน):
    /// - HoverAndRub: บินลอยตัว ปีกกระพือถี่สูง (Buzzing Wings) ลอยขึ้นลงเบาๆ และถูขาหน้าไปมาอย่างเมามัน
    /// - PerchedRub: เกาะนิ่งบนพื้น/โต๊ะ ถูขาหน้าทำความสะอาด/วางแผน
    /// - BuzzDart: บินโฉบเฉี่ยวไปมา
    /// </summary>
    [ExecuteInEditMode]
    public class FlyCharacterAnimator : MonoBehaviour
    {
        [Header("Animation State")]
        public FlyAnimationState currentState = FlyAnimationState.HoverAndRub;

        [Header("Hierarchy References")]
        public Transform bodyRoot;
        public Transform head;
        public Transform leftWing;
        public Transform rightWing;
        public Transform leftFrontLeg;
        public Transform rightFrontLeg;
        public Transform leftMidLeg;
        public Transform rightMidLeg;
        public Transform leftHindLeg;
        public Transform rightHindLeg;
        public Transform leftAntenna;
        public Transform rightAntenna;

        [Header("Speeds & Frequencies")]
        [Range(20f, 80f)] public float wingBuzzSpeed = 55f;   // ความเร็วกระพือปีกถี่สูง
        [Range(10f, 35f)] public float handRubSpeed = 24f;    // ความเร็วในการถูขาหน้า
        [Range(1f, 6f)] public float hoverBobSpeed = 2.5f;    // ความเร็วลอยตัวขึ้นลง
        [Range(0.02f, 0.25f)] public float hoverBobAmount = 0.08f;

        private Vector3 initialBodyPos;
        private Quaternion initialLeftWingRot;
        private Quaternion initialRightWingRot;

        private void Awake()
        {
            RecordInitialTransforms();
        }

        public void RecordInitialTransforms()
        {
            if (bodyRoot != null) initialBodyPos = bodyRoot.localPosition;
            if (leftWing != null) initialLeftWingRot = leftWing.localRotation;
            if (rightWing != null) initialRightWingRot = rightWing.localRotation;
        }

        private void Update()
        {
            float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;

            switch (currentState)
            {
                case FlyAnimationState.HoverAndRub:
                    AnimateHoverAndRub(t, isHovering: true);
                    break;
                case FlyAnimationState.PerchedRub:
                    AnimateHoverAndRub(t, isHovering: false);
                    break;
                case FlyAnimationState.BuzzDart:
                    AnimateBuzzDart(t);
                    break;
            }
        }

        private void AnimateHoverAndRub(float t, bool isHovering)
        {
            // 1. ปีกกระพือถี่สูงแบบแมลงวัน (High-frequency wing buzz)
            float wingBuzz = Mathf.Sin(t * wingBuzzSpeed) * 32f;
            if (leftWing != null)
                leftWing.localRotation = initialLeftWingRot * Quaternion.Euler(0, wingBuzz, wingBuzz * 0.6f);
            if (rightWing != null)
                rightWing.localRotation = initialRightWingRot * Quaternion.Euler(0, -wingBuzz, -wingBuzz * 0.6f);

            // 2. ลอยตัวขึ้นลงในอากาศ (Hover bobbing)
            if (isHovering)
            {
                float bob = Mathf.Sin(t * hoverBobSpeed) * hoverBobAmount;
                float tilt = Mathf.Sin(t * hoverBobSpeed * 0.7f) * 4f;
                if (bodyRoot != null)
                {
                    bodyRoot.localPosition = initialBodyPos + new Vector3(0, bob, 0);
                    bodyRoot.localRotation = Quaternion.Euler(15f + tilt, 0, tilt * 0.5f);
                }
            }
            else
            {
                if (bodyRoot != null)
                {
                    bodyRoot.localPosition = initialBodyPos;
                    bodyRoot.localRotation = Quaternion.Euler(5f, 0, 0);
                }
            }

            // 3. ท่าถูขาหน้าไปมาอย่างรวดเร็ว (Iconic Fly Front-Leg Rubbing)
            float rub = Mathf.Sin(t * handRubSpeed);
            float rubOffset = rub * 15f;
            float rubWiggle = Mathf.Cos(t * handRubSpeed) * 12f;

            if (leftFrontLeg != null)
                leftFrontLeg.localRotation = Quaternion.Euler(30f + rubOffset, 25f + rubWiggle, -15f);

            if (rightFrontLeg != null)
                rightFrontLeg.localRotation = Quaternion.Euler(30f - rubOffset, -25f - rubWiggle, 15f);

            // 4. ขากลางและขาหลังห้อยหย่อนตามแรงโน้มถ่วง
            float legDangle = Mathf.Sin(t * hoverBobSpeed * 1.5f) * 6f;
            if (leftMidLeg != null) leftMidLeg.localRotation = Quaternion.Euler(-10f + legDangle, 0, -10f);
            if (rightMidLeg != null) rightMidLeg.localRotation = Quaternion.Euler(-10f + legDangle, 0, 10f);
            if (leftHindLeg != null) leftHindLeg.localRotation = Quaternion.Euler(-25f + legDangle, 0, -15f);
            if (rightHindLeg != null) rightHindLeg.localRotation = Quaternion.Euler(-25f + legDangle, 0, 15f);

            // 5. หนวดขยับดุ๊กดิ๊ก
            float antennaTwitch = Mathf.Sin(t * 18f) * 8f;
            if (leftAntenna != null) leftAntenna.localRotation = Quaternion.Euler(20f + antennaTwitch, -15f, 0);
            if (rightAntenna != null) rightAntenna.localRotation = Quaternion.Euler(20f - antennaTwitch, 15f, 0);
        }

        private void AnimateBuzzDart(float t)
        {
            float wingBuzz = Mathf.Sin(t * wingBuzzSpeed * 1.2f) * 38f;
            if (leftWing != null) leftWing.localRotation = initialLeftWingRot * Quaternion.Euler(0, wingBuzz, 0);
            if (rightWing != null) rightWing.localRotation = initialRightWingRot * Quaternion.Euler(0, -wingBuzz, 0);

            // บินพุ่งไปข้างหน้าและเอียงตัว
            float dartBob = Mathf.Sin(t * 8f) * 0.12f;
            float roll = Mathf.Sin(t * 4f) * 18f;
            if (bodyRoot != null)
            {
                bodyRoot.localPosition = initialBodyPos + new Vector3(Mathf.Sin(t * 3f) * 0.2f, dartBob, 0);
                bodyRoot.localRotation = Quaternion.Euler(30f, Mathf.Sin(t * 3f) * 20f, -roll);
            }
        }
    }
}
