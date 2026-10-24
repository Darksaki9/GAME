using UnityEngine;

namespace NutritionKitchen.Character
{
    public enum RatAnimationState
    {
        FourLeggedIdle, // ยืนสี่ขาตามธรรมชาติ (ค่าเริ่มต้นตรงตามภาพ Reference)
        IdleSniffing,   // ยืนสองขายกตัวดมกลิ่น
        ScurryRun       // วิ่งซอยเท้าไวๆ สไตล์หนู
    }

    /// <summary>
    /// ควบคุมแอนิเมชันและท่าทางของ Rat NPC อ้างอิงสรีระหนูจริงตาม Reference Image:
    /// - FourLeggedIdle: ยืน 4 ขาแนบพื้น หลังโก่งมนอย่างเป็นธรรมชาติ หางทอดยาวเลื้อยไปด้านหลัง
    /// - IdleSniffing: ยืนสองขาผงกดมกลิ่น
    /// - ScurryRun: วิ่ง 4 ขาอย่างรวดเร็ว
    /// </summary>
    [ExecuteInEditMode]
    public class RatCharacterAnimator : MonoBehaviour
    {
        [Header("Animation State")]
        public RatAnimationState currentState = RatAnimationState.FourLeggedIdle;

        [Header("Hierarchy References")]
        public Transform bodyRoot;
        public Transform head;
        public Transform snout;
        public Transform leftEar;
        public Transform rightEar;
        public Transform leftFrontLeg;
        public Transform rightFrontLeg;
        public Transform leftBackLeg;
        public Transform rightBackLeg;
        public Transform tailRoot;
        public Transform tailMid;
        public Transform tailTip;

        [Header("Animation Speeds")]
        [Range(1f, 6f)] public float breathSpeed = 2.5f;
        [Range(1f, 8f)] public float sniffSpeed = 4.5f;
        [Range(1f, 6f)] public float tailSwaySpeed = 1.8f;
        [Range(2f, 10f)] public float runSpeed = 6.5f;

        private Vector3 initialBodyPos;
        private Quaternion initialHeadRot;

        private void Awake()
        {
            RecordInitialTransforms();
        }

        public void RecordInitialTransforms()
        {
            if (bodyRoot != null) initialBodyPos = bodyRoot.localPosition;
            if (head != null) initialHeadRot = head.localRotation;
        }

        private void Update()
        {
            float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;

            switch (currentState)
            {
                case RatAnimationState.FourLeggedIdle:
                    AnimateFourLeggedIdle(t);
                    break;
                case RatAnimationState.IdleSniffing:
                    AnimateIdleSniffing(t);
                    break;
                case RatAnimationState.ScurryRun:
                    AnimateScurryRun(t);
                    break;
            }
        }

        /// <summary>
        /// ท่ายืน 4 ขาตามภาพ Reference Image:
        /// - เท้าทั้ง 4 วางระนาบกับพื้นอย่างมั่นคง
        /// - ลำตัวหลังโก่งโค้งมนอย่างเป็นธรรมชาติ หายใจแผ่วเบา
        /// - หัวผงกมองและดมกลิ่นเป็นระยะ
        /// - หางทอดยาวบนพื้นและเลื้อยขยับเบาๆ
        /// </summary>
        private void AnimateFourLeggedIdle(float t)
        {
            float breath = Mathf.Sin(t * breathSpeed) * 0.006f;

            if (bodyRoot != null)
            {
                bodyRoot.localPosition = initialBodyPos + new Vector3(0, breath, 0);
                bodyRoot.localRotation = Quaternion.identity;
            }

            // ศีรษะผงกมองและดมกลิ่นเป็นจังหวะตามสัญชาตญาณสัตว์ฟันแทะ
            float sniffCycle = (t * 1.5f) % 5f;
            float targetPitch = 0f;
            float targetYaw = 0f;

            if (sniffCycle < 1.2f) { targetYaw = -10f; targetPitch = -2f; }
            else if (sniffCycle < 2.5f) { targetYaw = 8f; targetPitch = 3f; }
            else if (sniffCycle < 3.8f) { targetYaw = -4f; targetPitch = -6f; }
            else { targetYaw = 0f; targetPitch = 0f; }

            float microSniff = Mathf.Sin(t * sniffSpeed * 2.5f) * 1.2f;

            if (head != null)
            {
                head.localRotation = Quaternion.Euler(targetPitch + microSniff, targetYaw, microSniff * 0.3f);
            }

            // หูกระดิกเบาๆ
            float earTwitch = Mathf.Sin(t * sniffSpeed * 2f) * 2.5f;
            if (leftEar != null) leftEar.localRotation = Quaternion.Euler(12f + earTwitch, -18f, -18f);
            if (rightEar != null) rightEar.localRotation = Quaternion.Euler(12f - earTwitch, 18f, 18f);

            // ขาทั้ง 4 ข้างยืนบนพื้นอย่างมั่นคง
            if (leftFrontLeg != null) leftFrontLeg.localRotation = Quaternion.identity;
            if (rightFrontLeg != null) rightFrontLeg.localRotation = Quaternion.identity;
            if (leftBackLeg != null) leftBackLeg.localRotation = Quaternion.identity;
            if (rightBackLeg != null) rightBackLeg.localRotation = Quaternion.identity;

            // หางทอดแนบพื้นและส่ายเลื้อยอ่อนช้อยไปมาซ้ายขวาบนพื้นดิน
            float tailSway1 = Mathf.Sin(t * tailSwaySpeed) * 8f;
            float tailSway2 = Mathf.Cos(t * tailSwaySpeed) * 12f;
            if (tailRoot != null) tailRoot.localRotation = Quaternion.Euler(0f, tailSway1, 0f);
            if (tailMid != null) tailMid.localRotation = Quaternion.Euler(0f, tailSway2, 0f);
            if (tailTip != null) tailTip.localRotation = Quaternion.Euler(0f, tailSway1 * 1.2f, 0f);
        }

        private void AnimateIdleSniffing(float t)
        {
            float breath = Mathf.Sin(t * sniffSpeed) * 0.012f;

            if (bodyRoot != null)
            {
                bodyRoot.localPosition = initialBodyPos + new Vector3(0, 0.10f + breath, 0);
                bodyRoot.localRotation = Quaternion.Euler(-28f, 0, 0); // ยืนสองขา
            }

            float sniffTwitch = Mathf.Sin(t * sniffSpeed * 3f) * 3f;
            if (head != null) head.localRotation = Quaternion.Euler(-18f + sniffTwitch, Mathf.Sin(t * 2f) * 15f, 0);

            // ขาหน้ายกขึ้นประกบที่อก
            if (leftFrontLeg != null) leftFrontLeg.localRotation = Quaternion.Euler(35f, 15f, 0);
            if (rightFrontLeg != null) rightFrontLeg.localRotation = Quaternion.Euler(35f, -15f, 0);

            // ขาหลังรับน้ำหนัก
            if (leftBackLeg != null) leftBackLeg.localRotation = Quaternion.Euler(20f, 0, 0);
            if (rightBackLeg != null) rightBackLeg.localRotation = Quaternion.Euler(20f, 0, 0);

            float tailWag = Mathf.Sin(t * tailSwaySpeed * 2f) * 16f;
            if (tailRoot != null) tailRoot.localRotation = Quaternion.Euler(15f, tailWag, 0);
        }

        private void AnimateScurryRun(float t)
        {
            float cycle = t * runSpeed * 4f;
            float runSin = Mathf.Sin(cycle);
            float runCos = Mathf.Cos(cycle);

            if (bodyRoot != null)
            {
                bodyRoot.localPosition = initialBodyPos + new Vector3(0, Mathf.Abs(runCos) * 0.03f, 0);
                bodyRoot.localRotation = Quaternion.Euler(runSin * 3f, 0, 0);
            }

            if (leftFrontLeg != null) leftFrontLeg.localRotation = Quaternion.Euler(runSin * 32f, 0, 0);
            if (rightFrontLeg != null) rightFrontLeg.localRotation = Quaternion.Euler(-runSin * 32f, 0, 0);
            if (leftBackLeg != null) leftBackLeg.localRotation = Quaternion.Euler(-runSin * 32f, 0, 0);
            if (rightBackLeg != null) rightBackLeg.localRotation = Quaternion.Euler(runSin * 32f, 0, 0);

            if (tailRoot != null) tailRoot.localRotation = Quaternion.Euler(10f, runSin * 10f, 0);
        }
    }
}
