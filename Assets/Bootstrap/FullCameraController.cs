using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace TavernSim.Bootstrap
{
    /// <summary>
    /// Provides feature-complete camera controls suitable for isometric gameplay prototypes.
    /// Supports keyboard movement, scroll zoom, mouse drag panning, and orbit rotation while
    /// remaining compatible with either the legacy input manager or the new Input System.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FullCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float fastMoveMultiplier = 2f;
        [SerializeField] private float dragSpeed = 6f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float orbitSpeed = 240f;
        [SerializeField] private float minPitch = 35f;
        [SerializeField] private float maxPitch = 75f;

        [Header("Zoom")]
        [SerializeField] private float zoomSpeed = 6f;
        [SerializeField] private float zoomStep = 1.5f;
        [SerializeField] private float minZoom = 4f;
        [SerializeField] private float maxZoom = 30f;

        private Vector3 _pivot;
        private float _distance;
        private float _targetDistance;
        private float _yaw;
        private float _pitch;
        private bool _isDragging;
        private bool _isOrbiting;
        private Vector2 _lastPointer;

        private void Start()
        {
            var rotation = transform.rotation.eulerAngles;
            _yaw = rotation.y;
            _pitch = Mathf.Clamp(rotation.x, minPitch, maxPitch);

            var ray = new Ray(transform.position, transform.forward);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out var hitDistance))
            {
                hitDistance = Mathf.Abs(transform.position.y);
            }

            _pivot = ray.GetPoint(hitDistance);
            _pivot.y = 0f;

            var offset = transform.position - _pivot;
            _distance = Mathf.Clamp(offset.magnitude, minZoom, maxZoom);
            _targetDistance = _distance;

            ApplyTransform();
        }

        private void Update()
        {
            HandleKeyboardMovement();
            HandleRotation();
            HandleZoom();
            HandlePointerInput();
            SmoothZoom();
            ApplyTransform();
        }

        private void HandleKeyboardMovement()
        {
            Vector2 moveInput = Vector2.zero;
            bool boost = false;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    moveInput.y += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    moveInput.y -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    moveInput.x += 1f;
                }

                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    moveInput.x -= 1f;
                }

                boost = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            }
#else
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                moveInput.y += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                moveInput.y -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                moveInput.x += 1f;
            }

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                moveInput.x -= 1f;
            }

            boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            if (moveInput.sqrMagnitude > 0f)
            {
                var speed = moveSpeed * (boost ? fastMoveMultiplier : 1f);
                var yawRotation = Quaternion.Euler(0f, _yaw, 0f);
                var planarMove = yawRotation * new Vector3(moveInput.x, 0f, moveInput.y);
                _pivot += planarMove * speed * Time.deltaTime;
                _pivot.y = 0f;
            }
        }

        private void HandleRotation()
        {
            float yawInput = 0f;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed)
                {
                    yawInput -= 1f;
                }

                if (keyboard.eKey.isPressed)
                {
                    yawInput += 1f;
                }
            }
#else
            if (Input.GetKey(KeyCode.Q))
            {
                yawInput -= 1f;
            }

            if (Input.GetKey(KeyCode.E))
            {
                yawInput += 1f;
            }
#endif

            if (Mathf.Abs(yawInput) > 0.001f)
            {
                _yaw += yawInput * rotationSpeed * Time.deltaTime;
            }
        }

        private void HandleZoom()
        {
            float scroll = 0f;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = Mouse.current;
            if (mouse != null)
            {
                scroll = mouse.scroll.ReadValue().y;
            }
#else
            scroll = Input.mouseScrollDelta.y;
#endif

            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetDistance = Mathf.Clamp(_targetDistance - scroll * zoomStep, minZoom, maxZoom);
            }
        }

        private void SmoothZoom()
        {
            _targetDistance = Mathf.Clamp(_targetDistance, minZoom, maxZoom);
            _distance = Mathf.MoveTowards(_distance, _targetDistance, zoomSpeed * Time.deltaTime);
        }

        private void HandlePointerInput()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var mouse = Mouse.current;
            if (mouse == null)
            {
                _isDragging = false;
                _isOrbiting = false;
                return;
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _lastPointer = mouse.position.ReadValue();
            }
            else if (mouse.rightButton.wasReleasedThisFrame)
            {
                _isDragging = false;
            }

            if (mouse.middleButton.wasPressedThisFrame)
            {
                _isOrbiting = true;
                _lastPointer = mouse.position.ReadValue();
            }
            else if (mouse.middleButton.wasReleasedThisFrame)
            {
                _isOrbiting = false;
            }

            var pointer = mouse.position.ReadValue();
#else
            if (Input.GetMouseButtonDown(1))
            {
                _isDragging = true;
                _lastPointer = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(1))
            {
                _isDragging = false;
            }

            if (Input.GetMouseButtonDown(2))
            {
                _isOrbiting = true;
                _lastPointer = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(2))
            {
                _isOrbiting = false;
            }

            var pointer = (Vector2)Input.mousePosition;
#endif

            if (_isDragging)
            {
                ApplyDrag(pointer - _lastPointer);
                _lastPointer = pointer;
            }

            if (_isOrbiting)
            {
                ApplyOrbit(pointer - _lastPointer);
                _lastPointer = pointer;
            }
        }

        private void ApplyDrag(Vector2 delta)
        {
            if (delta.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }

            var width = Screen.width > 0 ? Screen.width : 1;
            var height = Screen.height > 0 ? Screen.height : 1;
            var normalized = new Vector2(delta.x / width, delta.y / height);
            var yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            var right = yawRotation * Vector3.right;
            var forward = yawRotation * Vector3.forward;
            var movement = (-right * normalized.x - forward * normalized.y) * dragSpeed * _distance;
            _pivot += movement;
            _pivot.y = 0f;
        }

        private void ApplyOrbit(Vector2 delta)
        {
            if (delta.sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }

            var width = Screen.width > 0 ? Screen.width : 1;
            var height = Screen.height > 0 ? Screen.height : 1;
            _yaw += (delta.x / width) * orbitSpeed;
            _pitch -= (delta.y / height) * orbitSpeed;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        private void ApplyTransform()
        {
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            _distance = Mathf.Clamp(_distance, minZoom, maxZoom);

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var forward = rotation * Vector3.forward;
            var position = _pivot - forward * _distance;
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
