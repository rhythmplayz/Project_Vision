using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class DualPerspectiveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private CinemachineCamera firstPersonCam;
    [SerializeField] private CinemachineCamera thirdPersonCam;
    [SerializeField] private Renderer[] firstPersonHiddenRenderers; // Assign PlayerVisual MeshRenderer here

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float rotationSmoothTime = 0.08f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private CharacterController _controller;
    private float _verticalVelocity;
    private float _yaw;
    private float _pitch;
    private float _turnSmoothVelocity;
    private bool _isFirstPerson;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        _yaw = transform.eulerAngles.y;
        ApplyPerspectiveState();
    }

    private void Update()
    {
        HandlePerspectiveToggle();
        HandleLook();
        HandleMovement();
    }

    private void HandlePerspectiveToggle()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            _isFirstPerson = !_isFirstPerson;
            ApplyPerspectiveState();
        }
    }

    private void ApplyPerspectiveState()
    {
        firstPersonCam.Priority = _isFirstPerson ? 20 : 10;
        thirdPersonCam.Priority = _isFirstPerson ? 10 : 20;

        // Hide capsule mesh in 1st-person so it never blocks the lens
        foreach (var r in firstPersonHiddenRenderers)
        {
            if (r != null) r.enabled = _isFirstPerson;
        }

        // Snap player facing direction to camera yaw when entering 1st-person
        if (_isFirstPerson)
        {
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }
    }

    private void HandleLook()
    {
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        _yaw += delta.x * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, minPitch, maxPitch);

        cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        if (_isFirstPerson)
        {
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        }
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);
        }

        bool isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 moveDir = Vector3.zero;
        if (input.sqrMagnitude >= 0.01f)
        {
            float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + _yaw;

            if (!_isFirstPerson)
            {
                float smoothAngle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
            }

            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        // Gravity & Jump
        if (_controller.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }

        if (_controller.isGrounded && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * targetSpeed + Vector3.up * _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }
}