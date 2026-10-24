using UnityEngine;
using UnityEngine.InputSystem;
using NutritionKitchen.Character;

/// <summary>
/// First-person walk / sprint / crouch / jump on a CharacterController.
/// Unity 6 (6000.x), new Input System package (keyboard + mouse read directly,
/// so no Input Actions asset setup is needed).
/// Put on the player root next to a CharacterController; the camera is a child.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Speed (m/s)")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float groundAcceleration = 50f;
    [SerializeField] private float airAcceleration = 10f;   // limited air control
    [SerializeField] private bool allowSprintInAir = false;

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = 20f;           // positive, m/s^2
    [SerializeField] private float groundStickForce = 2f;   // keeps the capsule on slopes

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 6f; // metres of height per second

    [Header("Look")]
    [SerializeField] private Transform cameraTransform;     // auto-found if empty
    [SerializeField] private float mouseSensitivity = 0.1f; // degrees per mouse pixel
    [SerializeField] private float maxPitch = 85f;

    private CharacterController controller;
    private float standingHeight;
    private Vector3 standingCenter;
    private float standingCameraY;
    private float verticalVelocity;
    private Vector3 horizontalVelocity;
    private float targetSpeed;
    private float pitch;
    private SimpleCharacterAnimator anim;

    // Unused layer: head/hat live here so only the owner's own camera skips them.
    private const int HiddenFromSelfLayer = 30;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        standingHeight = controller.height;
        standingCenter = controller.center;
        targetSpeed = walkSpeed;
        if (cameraTransform == null) cameraTransform = GetComponentInChildren<Camera>().transform;
        standingCameraY = cameraTransform.localPosition.y;
        anim = GetComponent<SimpleCharacterAnimator>();

        // The camera sits inside the head, so hide head+hat from it. Shadows still draw,
        // and other players' cameras still see the head (this script runs on the owner only).
        Transform head = transform.Find("BodyRoot/Head");
        if (head != null)
        {
            foreach (Transform t in head.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = HiddenFromSelfLayer;
            cameraTransform.GetComponent<Camera>().cullingMask &= ~(1 << HiddenFromSelfLayer);
        }
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return; // no device yet: do nothing

        Look(mouse.delta.ReadValue());

        Vector2 input = ReadMoveInput(keyboard);
        bool grounded = controller.isGrounded;
        bool crouching = UpdateCrouch(keyboard.leftCtrlKey.isPressed);

        // Speed class is decided on the ground and kept in the air, so releasing
        // Shift mid-jump (or jumping from a sprint) doesn't change speed.
        if (grounded || allowSprintInAir)
        {
            bool sprinting = keyboard.leftShiftKey.isPressed && input != Vector2.zero && !crouching;
            targetSpeed = crouching ? crouchSpeed : sprinting ? sprintSpeed : walkSpeed;
        }

        Vector3 wish = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
        float accel = grounded ? groundAcceleration : airAcceleration;
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wish, accel * Time.deltaTime);

        // Jump only from the ground and never while crouched (including forced crouch under a ceiling).
        if (grounded && !crouching && keyboard.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight); // v = sqrt(2gh)
        else if (grounded && verticalVelocity < 0f)
            verticalVelocity = -groundStickForce;
        else
            verticalVelocity -= gravity * Time.deltaTime;

        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (anim != null)
        {
            anim.isMoving = grounded && horizontalVelocity.sqrMagnitude > 0.25f;
            anim.isRunning = targetSpeed == sprintSpeed;
        }
    }

    private void Look(Vector2 mouseDelta)
    {
        transform.Rotate(0f, mouseDelta.x * mouseSensitivity, 0f);
        pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, -maxPitch, maxPitch);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private static Vector2 ReadMoveInput(Keyboard k)
    {
        Vector2 v = new Vector2(
            (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f),
            (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f));
        return Vector2.ClampMagnitude(v, 1f); // no faster diagonals
    }

    /// <summary>Resizes the capsule toward the wanted height. Returns true while crouched (held, or stuck under a ceiling).</summary>
    private bool UpdateCrouch(bool crouchHeld)
    {
        float current = controller.height;
        float target = crouchHeld ? crouchHeight : standingHeight;

        // Growing: only grow as far as the free space overhead allows.
        if (target > current)
        {
            float room = FreeHeadroom(target - current);
            target = current + room;
        }

        float next = Mathf.MoveTowards(current, target, crouchTransitionSpeed * Time.deltaTime);
        if (!Mathf.Approximately(next, current))
        {
            controller.height = next;
            // Keep the capsule bottom fixed so the player never sinks into the floor.
            controller.center = new Vector3(standingCenter.x, standingCenter.y - (standingHeight - next) * 0.5f, standingCenter.z);
            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, standingCameraY - (standingHeight - next), cameraTransform.localPosition.z);
        }

        // Still "crouching" until fully standing again.
        return next < standingHeight - 0.01f;
    }

    /// <summary>How much of 'wanted' extra height is free above the capsule's top.</summary>
    private float FreeHeadroom(float wanted)
    {
        float r = controller.radius;
        Vector3 topSphere = transform.TransformPoint(controller.center) + Vector3.up * (controller.height * 0.5f - r);
        // Slightly thinner sphere so walls beside us don't count as a ceiling.
        if (Physics.SphereCast(topSphere, r * 0.9f, Vector3.up, out RaycastHit hit, wanted + 0.05f, ~0, QueryTriggerInteraction.Ignore))
            return Mathf.Max(0f, hit.distance - 0.05f);
        return wanted;
    }
}
