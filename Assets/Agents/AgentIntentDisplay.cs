using TMPro;
using UnityEngine;

namespace TavernSim.Agents
{
    /// <summary>
    /// Spawns and orients a floating TextMeshPro label above an agent to describe its current intent.
    /// </summary>
    public sealed class AgentIntentDisplay : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);
        [SerializeField] private float fontSize = 2f;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private bool yawOnly = true;
        [SerializeField] private Transform pivot;

        private static readonly Camera[] CameraSearchBuffer = new Camera[8];

        private TextMeshPro _label;
        private Transform _labelTransform;
        private Camera _camera;
        private string _currentIntent = string.Empty;

        private void Awake()
        {
            EnsureLabel();
        }

        private void OnEnable()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_labelTransform == null)
            {
                return;
            }

            if (!TryResolveCamera(out var camera))
            {
                return;
            }

            var cameraTransform = camera.transform;
            var worldPosition = transform.position + offset;
            _labelTransform.position = worldPosition;

            if (!faceCamera)
            {
                return;
            }

            var target = pivot != null ? pivot : _labelTransform;
            if (target == null)
            {
                return;
            }

            var toCamera = cameraTransform.position - target.position;
            if (yawOnly)
            {
                toCamera.y = 0f;
            }

            if (toCamera.sqrMagnitude <= Mathf.Epsilon)
            {
                toCamera = -cameraTransform.forward;
                if (yawOnly)
                {
                    toCamera.y = 0f;
                }
            }

            if (toCamera.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var up = yawOnly ? Vector3.up : cameraTransform.up;
            target.rotation = Quaternion.LookRotation(toCamera.normalized, up);
        }

        public void SetIntent(string text)
        {
            EnsureLabel();
            if (_label != null)
            {
                _label.text = text;
            }

            _currentIntent = text;
        }

        public string CurrentIntent => _currentIntent;

        private void EnsureLabel()
        {
            if (_label != null)
            {
                return;
            }

            var go = new GameObject("IntentLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = offset;
            _labelTransform = go.transform;
            if (pivot == null)
            {
                pivot = _labelTransform;
            }

            _label = go.AddComponent<TextMeshPro>();
            _label.text = string.Empty;
            _label.fontSize = fontSize;
            _label.color = textColor;
            _label.alignment = TextAlignmentOptions.Center;
            _label.enableAutoSizing = false;
            _label.sortingOrder = int.MaxValue;
        }

        private bool TryResolveCamera(out Camera camera)
        {
            if (_camera != null && _camera.isActiveAndEnabled)
            {
                camera = _camera;
                return true;
            }

            var mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.isActiveAndEnabled)
            {
                _camera = mainCamera;
                camera = _camera;
                return true;
            }

            var count = Camera.GetAllCameras(CameraSearchBuffer);
            for (var i = 0; i < count; i++)
            {
                var candidate = CameraSearchBuffer[i];
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                _camera = candidate;
                camera = _camera;
                return true;
            }

            camera = null;
            return false;
        }

        private void OnDestroy()
        {
            if (_labelTransform == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_labelTransform.gameObject);
            }
            else
            {
                DestroyImmediate(_labelTransform.gameObject);
            }
        }
    }
}
