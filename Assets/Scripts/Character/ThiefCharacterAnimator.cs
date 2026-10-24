using UnityEngine;

namespace NutritionKitchen.Character
{
    public enum ThiefAnimationState
    {
        StandingIdle,       // ยืนตรงสงบนิ่งบนพื้น หายใจเบาๆ
        ActingMouseAndFly,  // ท่าผสม: ถูมือไวๆ แบบแมลงวัน + ย่อตัวย่องระแวงหันซ้ายขวาไวๆ แบบหนู (ค่าเริ่มต้นใหม่)
        ActingFly,          // ท่าแมลงวัน: ถูมือไปมาไวๆ ตรงหน้าอก วางแผนชั่วร้าย (Fly Hand-Rubbing)
        ActingMouse,        // ท่าหนู: ย่อตัวตื่นกลัว กระตุกหันมองซ้ายขวาเร็วๆ ป้องมือที่อก (Timid Mouse)
        SneakWalk,          // เดินย่องเบาสไตล์การ์ตูน
        ReferencePose       // ท่ากางแขนขาลอยตามภาพ Reference
    }

    /// <summary>
    /// ควบคุมแอนิเมชันและท่าทางของ Thief NPC:
    /// - ActingMouseAndFly: เลียนแบบพฤติกรรม "หนู" (ย่อตัวระแวง หันมองกระตุกๆ ไวๆ) + "แมลงวัน" (ถูมือไปมาเร็วๆ ที่หน้าอกแบบตัวโกงวางแผน)
    /// - StandingIdle: ท่ายืนตรงบนพื้น
    /// - SneakWalk: ท่าย่องเบา
    /// - ReferencePose: ท่าทางตามภาพตัวอย่าง
    /// </summary>
    [ExecuteInEditMode]
    public class ThiefCharacterAnimator : MonoBehaviour
    {
        [Header("Animation State")]
        public ThiefAnimationState currentState = ThiefAnimationState.ActingMouseAndFly;

        [Header("Hierarchy References")]
        public Transform bodyRoot;
        public Transform head;
        public Transform leftArm;
        public Transform leftForearm;
        public Transform leftHand;
        public Transform rightArm;
        public Transform rightForearm;
        public Transform rightHand;
        public Transform leftLeg;
        public Transform leftShin;
        public Transform leftBoot;
        public Transform rightLeg;
        public Transform rightShin;
        public Transform rightBoot;

        [Header("Speeds & Intensities")]
        [Range(0.5f, 5f)] public float idleSpeed = 1.8f;
        [Range(0.01f, 0.2f)] public float breathIntensity = 0.035f;
        [Range(10f, 35f)] public float flyHandRubSpeed = 22f; // ความเร็วในการถูมือแบบแมลงวัน
        [Range(0.5f, 4f)] public float mouseGlanceSpeed = 1.6f; // ความเร็วในการหันมองกระตุกแบบหนู
        [Range(0.5f, 5f)] public float sneakSpeed = 2.2f;

        private Vector3 initialBodyPos;
        private Vector3 initialHeadPos;
        private Quaternion initialLeftArmRot;
        private Quaternion initialLeftForearmRot;
        private Quaternion initialRightArmRot;
        private Quaternion initialRightForearmRot;
        private Quaternion initialLeftLegRot;
        private Quaternion initialLeftShinRot;
        private Quaternion initialRightLegRot;
        private Quaternion initialRightShinRot;

        // สำหรับ Mouse Saccadic lookaround
        private float currentHeadYaw = 0f;
        private float currentHeadPitch = 0f;

        private void Awake()
        {
            RecordInitialTransforms();
        }

        public void RecordInitialTransforms()
        {
            if (bodyRoot != null) initialBodyPos = bodyRoot.localPosition;
            if (head != null) initialHeadPos = head.localPosition;
            if (leftArm != null) initialLeftArmRot = leftArm.localRotation;
            if (leftForearm != null) initialLeftForearmRot = leftForearm.localRotation;
            if (rightArm != null) initialRightArmRot = rightArm.localRotation;
            if (rightForearm != null) initialRightForearmRot = rightForearm.localRotation;
            if (leftLeg != null) initialLeftLegRot = leftLeg.localRotation;
            if (leftShin != null) initialLeftShinRot = leftShin.localRotation;
            if (rightLeg != null) initialRightLegRot = rightLeg.localRotation;
            if (rightShin != null) initialRightShinRot = rightShin.localRotation;
        }

        private void Update()
        {
            float t = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;

            switch (currentState)
            {
                case ThiefAnimationState.ActingMouseAndFly:
                    AnimateMouseAndFly(t, true, true);
                    break;
                case ThiefAnimationState.ActingFly:
                    AnimateMouseAndFly(t, false, true);
                    break;
                case ThiefAnimationState.ActingMouse:
                    AnimateMouseAndFly(t, true, false);
                    break;
                case ThiefAnimationState.StandingIdle:
                    AnimateStandingIdle(t);
                    break;
                case ThiefAnimationState.SneakWalk:
                    ApplySneakWalk(t);
                    break;
                case ThiefAnimationState.ReferencePose:
                    ApplyReferencePose();
                    break;
            }
        }

        /// <summary>
        /// แอนิเมชันพฤติกรรม หนู (Mouse) และ แมลงวัน (Fly):
        /// - Fly: มือสองข้างยกขึ้นมาประกบที่หน้าอก และถูมือไปมาอย่างรวดเร็ว (Rubbing hands scheming)
        /// - Mouse: ย่อตัวงอเข่า ก้มตัวไปข้างหน้าเล็กน้อย หันศีรษะมองกระตุกซ้ายขวาอย่างรวดเร็วและระแวดระวัง
        /// </summary>
        private void AnimateMouseAndFly(float t, bool mouseBody, bool flyHands)
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.033f;

            // 1. ท่าทางลำตัวและขา (Mouse Crouch & Jitter)
            if (mouseBody)
            {
                float mouseBreath = Mathf.Sin(t * 14f) * 0.012f;
                if (bodyRoot != null)
                {
                    bodyRoot.localPosition = initialBodyPos + new Vector3(0, -0.14f + mouseBreath, 0.03f);
                    bodyRoot.localRotation = Quaternion.Euler(12f, Mathf.Sin(t * 2f) * 3f, 0f);
                }

                // ขาสองข้างย่อเข่าในท่าย่อตัวแบบหนู (Mouse Crouching stance)
                if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(-20f, -8f, 0f);
                if (leftShin != null) leftShin.localRotation = Quaternion.Euler(34f, 0f, 0f);
                if (leftBoot != null) leftBoot.localRotation = Quaternion.Euler(-14f, -4f, 0f);

                if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-20f, 8f, 0f);
                if (rightShin != null) rightShin.localRotation = Quaternion.Euler(34f, 0f, 0f);
                if (rightBoot != null) rightBoot.localRotation = Quaternion.Euler(-14f, 4f, 0f);

                // ศีรษะหันมองแบบกระตุกเป็นจังหวะตามสัญชาตญาณหนู (Mouse Saccadic glances)
                float cycle = (t * mouseGlanceSpeed) % 4.5f;
                float targetYaw = 0f;
                float targetPitch = -4f;

                if (cycle < 1.1f) { targetYaw = -28f; targetPitch = -6f; }       // หันซ้ายเร็ว
                else if (cycle < 1.6f) { targetYaw = 0f; targetPitch = -10f; }    // หันตรงดมกลิ่น
                else if (cycle < 2.8f) { targetYaw = 30f; targetPitch = -5f; }     // หันขวาเร็ว
                else if (cycle < 3.6f) { targetYaw = 12f; targetPitch = 6f; }      // เหลือบมองบนระแวง
                else { targetYaw = -10f; targetPitch = -8f; }

                currentHeadYaw = Mathf.Lerp(currentHeadYaw, targetYaw, dt * 18f);
                currentHeadPitch = Mathf.Lerp(currentHeadPitch, targetPitch, dt * 18f);

                // การสั่นกระตุกของจมูก/หัวแบบหนู
                float jitter = Mathf.Sin(t * 32f) * 1.5f;

                if (head != null)
                {
                    head.localRotation = Quaternion.Euler(currentHeadPitch + jitter, currentHeadYaw, jitter * 0.5f);
                }
            }
            else
            {
                // ท่ายืนตรงถ้าไม่เปิด Mouse posture
                if (bodyRoot != null)
                {
                    bodyRoot.localPosition = initialBodyPos + new Vector3(0, -0.08f, 0);
                    bodyRoot.localRotation = Quaternion.identity;
                }
                if (leftLeg != null) leftLeg.localRotation = Quaternion.identity;
                if (leftShin != null) leftShin.localRotation = Quaternion.identity;
                if (rightLeg != null) rightLeg.localRotation = Quaternion.identity;
                if (rightShin != null) rightShin.localRotation = Quaternion.identity;
                if (head != null) head.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 2f) * 10f, 0f);
            }

            // 2. ท่าทางแขนและมือ (Fly Hand-Rubbing หรือ Mouse Paw Guard)
            if (flyHands)
            {
                // แมลงวัน: ยกมือขึ้นมาประกบที่หน้าอกแล้วถูมือไปมาอย่างรวดเร็ว (Fly Hand-Rubbing)
                float rubCycle = Mathf.Sin(t * flyHandRubSpeed);
                float rubOffset = rubCycle * 8f;
                float rubHandWiggle = Mathf.Cos(t * flyHandRubSpeed) * 14f;

                // แขนซ้ายยกมาหน้าอก
                if (leftArm != null)
                    leftArm.localRotation = Quaternion.Euler(46f + rubOffset * 0.4f, 28f, -22f);
                if (leftForearm != null)
                    leftForearm.localRotation = Quaternion.Euler(74f, 18f, 10f);
                if (leftHand != null)
                    leftHand.localRotation = Quaternion.Euler(20f + rubHandWiggle, 65f, 15f);

                // แขนขวายกมาหน้าอก
                if (rightArm != null)
                    rightArm.localRotation = Quaternion.Euler(46f - rubOffset * 0.4f, -28f, 22f);
                if (rightForearm != null)
                    rightForearm.localRotation = Quaternion.Euler(74f, -18f, -10f);
                if (rightHand != null)
                    rightHand.localRotation = Quaternion.Euler(20f - rubHandWiggle, -65f, -15f);
            }
            else
            {
                // หนู: ป้องมือสองข้างไว้ที่หน้าอกเหมือนอุ้งเท้าหนู (Mouse Paws Held In Front)
                float pawTwitch = Mathf.Sin(t * 24f) * 3f;

                if (leftArm != null)
                    leftArm.localRotation = Quaternion.Euler(40f + pawTwitch, 24f, -15f);
                if (leftForearm != null)
                    leftForearm.localRotation = Quaternion.Euler(70f, 15f, 0f);
                if (leftHand != null)
                    leftHand.localRotation = Quaternion.Euler(30f + pawTwitch, 40f, 0f);

                if (rightArm != null)
                    rightArm.localRotation = Quaternion.Euler(40f - pawTwitch, -24f, 15f);
                if (rightForearm != null)
                    rightForearm.localRotation = Quaternion.Euler(70f, -15f, 0f);
                if (rightHand != null)
                    rightHand.localRotation = Quaternion.Euler(30f - pawTwitch, -40f, 0f);
            }
        }

        /// <summary>
        /// จัดท่ายืนตรงบนพื้น (Stand Upright)
        /// </summary>
        [ContextMenu("Apply Standing Upright Pose")]
        public void ApplyStandingUprightPose()
        {
            if (bodyRoot != null)
            {
                bodyRoot.localPosition = initialBodyPos + new Vector3(0, -0.08f, 0);
                bodyRoot.localRotation = Quaternion.identity;
            }

            if (head != null)
                head.localRotation = Quaternion.identity;

            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(8f, 0f, 12f);
            if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(10f, 0f, 0f);
            if (leftHand != null) leftHand.localRotation = Quaternion.Euler(0f, 15f, 0f);

            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(8f, 0f, -12f);
            if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(10f, 0f, 0f);
            if (rightHand != null) rightHand.localRotation = Quaternion.Euler(0f, -15f, 0f);

            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(0f, -5f, 0f);
            if (leftShin != null) leftShin.localRotation = Quaternion.identity;
            if (leftBoot != null) leftBoot.localRotation = Quaternion.Euler(0f, -4f, 0f);

            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(0f, 5f, 0f);
            if (rightShin != null) rightShin.localRotation = Quaternion.identity;
            if (rightBoot != null) rightBoot.localRotation = Quaternion.Euler(0f, 4f, 0f);
        }

        private void AnimateStandingIdle(float t)
        {
            ApplyStandingUprightPose();

            float breath = Mathf.Sin(t * idleSpeed) * breathIntensity;
            float lookAngle = Mathf.Sin(t * idleSpeed * 0.4f) * 6f;

            if (bodyRoot != null)
            {
                bodyRoot.localPosition = initialBodyPos + new Vector3(0, -0.08f + breath * 0.4f, 0);
            }

            if (head != null)
            {
                head.localRotation = Quaternion.Euler(Mathf.Sin(t * idleSpeed * 0.8f) * 2f, lookAngle, 0f);
            }
        }

        [ContextMenu("Apply Reference Pose")]
        public void ApplyReferencePose()
        {
            if (bodyRoot != null)
            {
                bodyRoot.localPosition = initialBodyPos + new Vector3(0, 0.22f, 0);
                bodyRoot.localRotation = Quaternion.Euler(3f, -8f, -2f);
            }

            if (head != null)
                head.localRotation = Quaternion.Euler(-4f, -4f, 1f);

            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(6f, -5f, -76f);
            if (leftForearm != null) leftForearm.localRotation = Quaternion.Euler(0f, 15f, -8f);
            if (leftHand != null) leftHand.localRotation = Quaternion.Euler(90f, 0f, 0f);

            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(6f, 5f, 76f);
            if (rightForearm != null) rightForearm.localRotation = Quaternion.Euler(0f, -15f, 8f);
            if (rightHand != null) rightHand.localRotation = Quaternion.Euler(90f, 0f, 0f);

            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(-45f, -35f, -25f);
            if (rightShin != null) rightShin.localRotation = Quaternion.Euler(70f, 15f, 20f);
            if (rightBoot != null) rightBoot.localRotation = Quaternion.Euler(20f, -10f, 0f);

            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(16f, 10f, 8f);
            if (leftShin != null) leftShin.localRotation = Quaternion.Euler(26f, 0f, 0f);
            if (leftBoot != null) leftBoot.localRotation = Quaternion.Euler(22f, 0f, 0f);
        }

        private void ApplySneakWalk(float t)
        {
            float cycle = t * sneakSpeed * 4f;
            float walkSin = Mathf.Sin(cycle);
            float walkCos = Mathf.Cos(cycle);

            float stepBob = Mathf.Abs(walkCos) * 0.10f;
            if (bodyRoot != null)
            {
                bodyRoot.localPosition = initialBodyPos + new Vector3(0, -0.08f + stepBob, 0);
                bodyRoot.localRotation = Quaternion.identity;
            }

            if (head != null)
                head.localRotation = Quaternion.Euler(walkSin * 4f, Mathf.Sin(t * sneakSpeed) * 15f, -walkSin * 3f);

            if (leftLeg != null)
                leftLeg.localRotation = initialLeftLegRot * Quaternion.Euler(walkSin * 45f, 0, 0);
            if (leftShin != null)
                leftShin.localRotation = initialLeftShinRot * Quaternion.Euler(Mathf.Clamp(walkSin * 40f, 0f, 50f), 0, 0);

            if (rightLeg != null)
                rightLeg.localRotation = initialRightLegRot * Quaternion.Euler(-walkSin * 45f, 0, 0);
            if (rightShin != null)
                rightShin.localRotation = initialRightShinRot * Quaternion.Euler(Mathf.Clamp(-walkSin * 40f, 0f, 50f), 0, 0);

            if (leftArm != null)
                leftArm.localRotation = Quaternion.Euler(15f + walkCos * 10f, -10f, -50f - walkSin * 15f);

            if (rightArm != null)
                rightArm.localRotation = Quaternion.Euler(15f - walkCos * 10f, 10f, 50f + walkSin * 15f);
        }
    }
}
