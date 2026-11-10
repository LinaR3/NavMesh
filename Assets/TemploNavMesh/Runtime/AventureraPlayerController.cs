using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class AventureraPlayerController : MonoBehaviour
{
    [SerializeField] CharacterController controller;
    [SerializeField] Transform cameraTransform;
    [SerializeField, Min(0.1f)] float walkSpeed = 4.2f;
    [SerializeField, Min(0.1f)] float sprintSpeed = 6.2f;
    [SerializeField, Min(0f)] float jumpHeight = 1.1f;
    [SerializeField, Min(0.1f)] float gravity = 24f;
    [SerializeField, Min(0f)] float turnSpeed = 720f;

    float verticalSpeed;
    bool inputEnabled = true;

    public CharacterController Controller => controller;
    public float Gravity => gravity;
    public bool InputEnabled { get => inputEnabled; set => inputEnabled = value; }

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        var animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
    }

    public void SetCamera(Transform cameraReference) => cameraTransform = cameraReference;

    void Update()
    {
        if (!inputEnabled || Keyboard.current == null || controller == null || !controller.enabled) return;

        var keyboard = Keyboard.current;
        var input = Vector2.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        var forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        forward.y = 0f;
        forward.Normalize();
        var right = cameraTransform != null ? cameraTransform.right : transform.right;
        right.y = 0f;
        right.Normalize();
        var moveDirection = forward * input.y + right * input.x;
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            var targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        if (controller.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
            verticalSpeed = Mathf.Sqrt(jumpHeight * 2f * gravity);
        verticalSpeed -= gravity * Time.deltaTime;

        var speed = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? sprintSpeed : walkSpeed;
        var displacement = moveDirection.normalized * (speed * Time.deltaTime) + Vector3.up * (verticalSpeed * Time.deltaTime);
        controller.Move(displacement);
    }
}
