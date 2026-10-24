using UnityEngine;

namespace NutritionKitchen.Character
{
    /// <summary>
    /// สคริปต์ควบคุมการเคลื่อนไหวแบบ Procedural Animation (Idle, Walk, Head Bobbing)
    /// ทำงานได้ทันทีโดยไม่ต้องพึ่งพาระบบ Bone Rigging หรือ Animation Clip ภายนอก
    /// </summary>
    public class SimpleCharacterAnimator : MonoBehaviour
    {
        [Header("Transform Hierarchy References")]
        public Transform bodyRoot;
        public Transform head;
        public Transform leftArm;
        public Transform rightArm;
        public Transform leftLeg;
        public Transform rightLeg;

        [Header("Movement State")]
        [Tooltip("สลับโหมดว่าตัวละครกำลังเดินหรือไม่")]
        public bool isMoving = false;
        [Tooltip("Run: faster steps, bigger swing, higher bounce (only used while isMoving)")]
        public bool isRunning = false;
        [Range(0.5f, 5f)] public float walkSpeed = 2f;

        [Header("Idle Animation Settings")]
        [Range(0.5f, 5f)] public float idleFrequency = 2.0f;
        [Range(0.01f, 0.2f)] public float breathAmount = 0.04f;

        private Vector3 initialBodyPos;
        private Vector3 initialHeadPos;
        private Quaternion initialLeftArmRot;
        private Quaternion initialRightArmRot;
        private Quaternion initialLeftLegRot;
        private Quaternion initialRightLegRot;

        private void Start()
        {
            // บันทึกตำแหน่งและมุมเริ่มต้นของแต่ละส่วน
            if (bodyRoot != null) initialBodyPos = bodyRoot.localPosition;
            if (head != null) initialHeadPos = head.localPosition;
            if (leftArm != null) initialLeftArmRot = leftArm.localRotation;
            if (rightArm != null) initialRightArmRot = rightArm.localRotation;
            if (leftLeg != null) initialLeftLegRot = leftLeg.localRotation;
            if (rightLeg != null) initialRightLegRot = rightLeg.localRotation;
        }

        private void Update()
        {
            float time = Time.time;

            if (isMoving)
            {
                AnimateWalk(time);
            }
            else
            {
                AnimateIdle(time);
            }
        }

        /// <summary>
        /// ท่าทางยืนนิ่ง: ตัวโยกขึ้นลงเบาๆ เหมือนหายใจ (Breathing / Idle Bobbing)
        /// </summary>
        private void AnimateIdle(float time)
        {
            float bob = Mathf.Sin(time * idleFrequency) * breathAmount;
            
            if (bodyRoot != null)
            {
                bodyRoot.localPosition = initialBodyPos + new Vector3(0, bob * 0.5f, 0);
            }

            if (head != null)
            {
                head.localPosition = initialHeadPos + new Vector3(0, bob * 0.7f, 0);
                head.localRotation = Quaternion.Euler(Mathf.Sin(time * idleFrequency * 0.5f) * 2f, 0, 0);
            }

            // คืนตำแหน่งแขนและขาในท่าพัก
            if (leftArm != null) leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, initialLeftArmRot, Time.deltaTime * 5f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, initialRightArmRot, Time.deltaTime * 5f);
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Slerp(leftLeg.localRotation, initialLeftLegRot, Time.deltaTime * 5f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, initialRightLegRot, Time.deltaTime * 5f);
        }

        /// <summary>
        /// ท่าทางเดิน: แขนและขาสลับกันแกว่งหน้า-หลัง พร้อมลำตัวโยกเล็กน้อย
        /// </summary>
        private void AnimateWalk(float time)
        {
            float run = isRunning ? 1.5f : 1f;
            float legSwingAngle = 35f * run;
            float armSwingAngle = 30f * run;
            float cycleSpeed = walkSpeed * 6f * (isRunning ? 1.6f : 1f);
            float walkCycle = Mathf.Sin(time * cycleSpeed);

            // แกว่งขาซ้าย-ขวาสลับกัน
            if (leftLeg != null)
                leftLeg.localRotation = initialLeftLegRot * Quaternion.Euler(walkCycle * legSwingAngle, 0, 0);

            if (rightLeg != null)
                rightLeg.localRotation = initialRightLegRot * Quaternion.Euler(-walkCycle * legSwingAngle, 0, 0);

            // แกว่งแขนตรงข้ามกับขา
            if (leftArm != null)
                leftArm.localRotation = initialLeftArmRot * Quaternion.Euler(-walkCycle * armSwingAngle, 0, 0);

            if (rightArm != null)
                rightArm.localRotation = initialRightArmRot * Quaternion.Euler(walkCycle * armSwingAngle, 0, 0);

            // ลำตัวโยกขึ้นลงตามก้าวเดิน
            if (bodyRoot != null)
            {
                float stepBob = Mathf.Abs(Mathf.Cos(time * cycleSpeed)) * 0.08f * run;
                bodyRoot.localPosition = initialBodyPos + new Vector3(0, stepBob, 0);
            }
        }
    }
}
