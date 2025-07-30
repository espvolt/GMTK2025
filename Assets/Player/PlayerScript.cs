using UnityEngine;
using UnityEngine.InputSystem; // New Input System

[RequireComponent(typeof(CharacterController))]
public class PlayerScript : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign the child camera transform (first-person view).")]
    public Transform cameraTransform;

    [Header("Input (New Input System)")]
    [Tooltip("Vector2: WASD (2D Vector composite).")]
    public InputActionProperty moveAction;
    [Tooltip("Vector2: Mouse delta or Right Stick.")]
    public InputActionProperty lookAction;
    [Tooltip("Button: Space.")]
    public InputActionProperty jumpAction;
    [Tooltip("Button: Left Shift (hold to sprint).")]
    public InputActionProperty sprintAction;

    [Header("Look")]
    [Tooltip("Mouse sensitivity (deg/s per mouse pixel).")]
    public float mouseSensitivity = 150f;
    [Tooltip("Clamp vertical look (pitch).")]
    public float minPitch = -85f;
    public float maxPitch = 85f;
    [Tooltip("Enable smoothing for gamepads (optional).")]
    public bool smoothGamepadLook = true;
    [Tooltip("Lerp factor for gamepad look (0-1). Higher = snappier.")]
    [Range(0f, 1f)] public float gamepadLookLerp = 0.5f;

    [Header("Move")]
    [Tooltip("Walking speed in m/s.")]
    public float moveSpeed = 5f;
    [Tooltip("Hold Sprint action to use sprint speed.")]
    public float sprintSpeed = 8f;
    public bool enableSprint = true;

    [Header("Jump & Gravity")]
    public bool enableJump = true;
    [Tooltip("Jump height in meters.")]
    public float jumpHeight = 1.2f;
    [Tooltip("Gravity (negative).")]
    public float gravity = -9.81f;

    [Header("Grounding")]
    [Tooltip("Extra downward force to keep grounded on slopes.")]
    public float groundedStickForce = -2f;

    private CharacterController controller;
    private float pitch;                 // camera pitch (x-rotation)
    private Vector3 verticalVelocity;    // y-velocity
    private Vector2 gamepadLookVel;      // for smoothing

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam) cameraTransform = cam.transform;
        }
    }

    void OnEnable()
    {
        // If these actions are not enabled by a PlayerInput, enable them here.
        EnableIfValid(moveAction);
        EnableIfValid(lookAction);
        EnableIfValid(jumpAction);
        EnableIfValid(sprintAction);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        DisableIfValid(moveAction);
        DisableIfValid(lookAction);
        DisableIfValid(jumpAction);
        DisableIfValid(sprintAction);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        Look();
        Move();
    }

    private void Look()
    {
        if (cameraTransform == null) return;

        Vector2 look = Vector2.zero;
        if (lookAction.action != null && lookAction.action.enabled)
            look = lookAction.action.ReadValue<Vector2>();

        // Heuristic: If coming from mouse, it's unscaled pixel delta.
        // We apply sensitivity * deltaTime to keep frame-rate independent.
        bool isMouse = Mouse.current != null && Mouse.current.delta.IsActuated();

        Vector2 lookInput = look;

        if (!isMouse && smoothGamepadLook)
        {
            // Smooth gamepad right-stick to avoid jitter
            gamepadLookVel = Vector2.Lerp(gamepadLookVel, lookInput, gamepadLookLerp);
            lookInput = gamepadLookVel;
        }

        float yawDelta   = lookInput.x * mouseSensitivity * Time.deltaTime;
        float pitchDelta = lookInput.y * mouseSensitivity * Time.deltaTime;

        // Rotate body by yaw
        transform.Rotate(Vector3.up, yawDelta, Space.Self);

        // Pitch camera (clamped)
        pitch -= pitchDelta;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Vector3 camEuler = cameraTransform.localEulerAngles;
        camEuler.x = pitch;
        camEuler.y = 0f;
        camEuler.z = 0f;
        cameraTransform.localEulerAngles = camEuler;
    }

    private void Move()
    {
        Vector2 moveInput = Vector2.zero;
        if (moveAction.action != null && moveAction.action.enabled)
            moveInput = moveAction.action.ReadValue<Vector2>();

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (move.sqrMagnitude > 1f) move.Normalize();

        float speed = moveSpeed;
        if (enableSprint && sprintAction.action != null && sprintAction.action.IsPressed())
            speed = sprintSpeed;

        // Grounding
        if (controller.isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = groundedStickForce;

        // Jump
        if (enableJump && controller.isGrounded && jumpAction.action != null && jumpAction.action.WasPressedThisFrame())
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Gravity
        verticalVelocity.y += gravity * Time.deltaTime;

        // Move
        Vector3 horizontal = move * speed * Time.deltaTime;
        Vector3 vertical   = verticalVelocity * Time.deltaTime;
        controller.Move(horizontal + vertical);
    }

    // Optional: re-lock cursor if window regains focus
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private static void EnableIfValid(InputActionProperty prop)
    {
        if (prop.reference == null && prop.action == null) return;
        var action = prop.action ?? prop.reference.action;
        if (action != null && !action.enabled) action.Enable();
    }

    private static void DisableIfValid(InputActionProperty prop)
    {
        if (prop.reference == null && prop.action == null) return;
        var action = prop.action ?? prop.reference.action;
        if (action != null && action.enabled) action.Disable();
    }
}