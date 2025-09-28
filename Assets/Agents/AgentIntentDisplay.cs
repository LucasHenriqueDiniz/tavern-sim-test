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

            var camera = ResolveCamera();
            if (camera == null)
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

            if (yawOnly)
            {
                var toCamera = cameraTransform.position - target.position;
                toCamera.y = 0f;
                if (toCamera.sqrMagnitude <= 0.0001f)
                {
                    return;
                }

                target.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
            }
            else
            {
                target.rotation = cameraTransform.rotation;
            }
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

        private Camera ResolveCamera()
        {
            if (_camera != null && _camera.isActiveAndEnabled)
            {
                return _camera;
            }

            _camera = Camera.main;
            return _camera != null && _camera.isActiveAndEnabled ? _camera : null;
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
