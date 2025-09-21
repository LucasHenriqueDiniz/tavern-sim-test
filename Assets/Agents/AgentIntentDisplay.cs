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

        private static readonly Camera[] CameraSearchBuffer = new Camera[8];

        private TextMeshPro _label;
        private Transform _labelTransform;
        private Camera _camera;

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

            var toCamera = cameraTransform.position - worldPosition;
            if (toCamera.sqrMagnitude <= Mathf.Epsilon)
            {
                toCamera = -cameraTransform.forward;
            }

            _labelTransform.rotation = Quaternion.LookRotation(toCamera, cameraTransform.up);
        }

        public void SetIntent(string text)
        {
            EnsureLabel();
            if (_label != null)
            {
                _label.text = text;
            }
        }

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
