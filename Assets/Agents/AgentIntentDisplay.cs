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

            if (_camera == null || !_camera.isActiveAndEnabled)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return;
                }
            }

            var worldPosition = transform.position + offset;
            _labelTransform.position = worldPosition;
            var cameraTransform = _camera.transform;
            var toCamera = cameraTransform.position - worldPosition;
            if (toCamera.sqrMagnitude > Mathf.Epsilon)
            {
                var rotation = Quaternion.LookRotation(toCamera, cameraTransform.up);
                _labelTransform.rotation = rotation;

                // If the label ends up aligned with the camera forward we are looking at its back.
                if (Vector3.Dot(_labelTransform.forward, cameraTransform.forward) > 0f)
                {
                    _labelTransform.rotation = rotation * Quaternion.AngleAxis(180f, cameraTransform.up);
                }
            }
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
