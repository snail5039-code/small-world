using SmallWorld.Services;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SmallWorld.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonPlayerController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private PlayerInteractionDetector interactionDetector;
        [SerializeField] private PlayerFootstepEmitter footstepEmitter;
        [SerializeField] private Graphic crosshair;
        [SerializeField, Min(0.1f)] private float walkSpeed = 4f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 6f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private PlayerAccessibilitySettings accessibility = new PlayerAccessibilitySettings();
        [SerializeField, Range(0f, 0.05f)] private float bobAmount = 0.012f;
        [SerializeField, Range(0.1f, 20f)] private float bobFrequency = 9f;

        private CharacterController characterController;
        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private float verticalVelocity;
        private float pitch;
        private float bobTime;
        private Vector3 cameraLocalOrigin;

        public PlayerAccessibilitySettings Accessibility => accessibility;
        public bool IsCursorLocked => Cursor.lockState == CursorLockMode.Locked;

        public void Configure(Camera camera, InputActionAsset actions, PlayerInteractionDetector detector,
            PlayerFootstepEmitter footsteps, Graphic crosshairGraphic)
        {
            playerCamera = camera;
            inputActions = actions;
            interactionDetector = detector;
            footstepEmitter = footsteps;
            crosshair = crosshairGraphic;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                cameraLocalOrigin = playerCamera.transform.localPosition;
                playerCamera.fieldOfView = accessibility.FieldOfView;
            }

            CacheInputActions();
            ApplyAccessibility();
        }

        private void OnEnable()
        {
            if (playerMap == null && !CacheInputActions()) return;
            if (playerMap == null) return;
            if (InputService.Instance != null) InputService.Instance.RegisterGameplayMap(playerMap);
            else playerMap.Enable();
            LockCursor();
        }

        private void OnDisable()
        {
            if (playerMap != null)
            {
                if (InputService.Instance != null) InputService.Instance.UnregisterGameplayMap(playerMap);
                else playerMap.Disable();
            }
            playerMap = null;
            moveAction = null;
            lookAction = null;
            sprintAction = null;
            jumpAction = null;
            UnlockCursor();
        }

        private void Update()
        {
            if (!IsCursorLocked)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) LockCursor();
                return;
            }

            UpdateLook();
            UpdateMovement();
        }

        private bool CacheInputActions()
        {
            if (inputActions == null)
            {
                Debug.LogError("[SmallWorld] Player InputActionAsset is missing.", this);
                return false;
            }

            playerMap = inputActions.FindActionMap("Player", false);
            moveAction = playerMap?.FindAction("Move", false);
            lookAction = playerMap?.FindAction("Look", false);
            sprintAction = playerMap?.FindAction("Sprint", false);
            jumpAction = playerMap?.FindAction("Jump", false);
            if (playerMap != null && moveAction != null && lookAction != null && sprintAction != null && jumpAction != null) return true;

            Debug.LogError("[SmallWorld] Player input map requires Move, Look, Sprint, and Jump actions.", this);
            playerMap = null;
            moveAction = null;
            lookAction = null;
            sprintAction = null;
            jumpAction = null;
            return false;
        }

        public void ApplyAccessibility()
        {
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = accessibility.FieldOfView;
                if (!accessibility.CameraBobEnabled) playerCamera.transform.localPosition = cameraLocalOrigin;
            }
            if (crosshair != null) crosshair.enabled = accessibility.CrosshairVisible;
        }

        private void UpdateLook()
        {
            if (playerCamera == null) return;
            Vector2 delta = lookAction.ReadValue<Vector2>() * accessibility.LookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
            transform.Rotate(Vector3.up, delta.x, Space.Self);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateMovement()
        {
            Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            bool sprinting = sprintAction.IsPressed() && input.y > 0.01f;
            float speed = sprinting ? sprintSpeed : walkSpeed;
            Vector3 planarVelocity = (transform.right * input.x + transform.forward * input.y) * speed;
            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = -2f;
                if (jumpAction.WasPressedThisFrame())
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 previousPosition = transform.position;
            Vector3 displacement = (planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime;
            CollisionFlags flags = characterController.Move(displacement);
            if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f) verticalVelocity = -2f;

            Vector3 actualDisplacement = transform.position - previousPosition;
            float distance = new Vector2(actualDisplacement.x, actualDisplacement.z).magnitude;
            footstepEmitter?.Tick(distance, characterController.isGrounded, sprinting);
            UpdateCameraBob(distance > 0.0001f && characterController.isGrounded, sprinting);
        }

        private void UpdateCameraBob(bool moving, bool sprinting)
        {
            if (!accessibility.CameraBobEnabled || playerCamera == null) return;
            if (moving) bobTime += Time.deltaTime * bobFrequency * (sprinting ? 1.25f : 1f);
            float target = moving ? Mathf.Sin(bobTime) * bobAmount : 0f;
            Vector3 position = playerCamera.transform.localPosition;
            position.y = Mathf.Lerp(position.y, cameraLocalOrigin.y + target, 10f * Time.deltaTime);
            playerCamera.transform.localPosition = position;
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
