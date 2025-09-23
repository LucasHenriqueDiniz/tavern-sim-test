using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TavernSim.Building
{
    /// <summary>
    /// Draws a simple grid using line renderers to help players visualize build placement cells.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildGridVisualizer : MonoBehaviour
    {
        [SerializeField] private int gridExtent = 12;
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private float lineWidth = 0.02f;
        [SerializeField] private float lineHeight = 0.02f;
        [SerializeField] private Color lineColor = new Color(0.3f, 0.8f, 1f, 0.35f);

        private readonly List<LineRenderer> _lines = new List<LineRenderer>();
        private Material _lineMaterial;

        private void Awake()
        {
            EnsureMaterial();
            RebuildLines();
            ApplySettings();
            UpdateLinePositions();
        }

        private void OnEnable()
        {
            ApplySettings();
            SetLinesEnabled(true);
        }

        private void OnDisable()
        {
            SetLinesEnabled(false);
        }

        private void OnValidate()
        {
            gridExtent = Mathf.Max(1, gridExtent);
            gridSize = Mathf.Max(0.1f, gridSize);
            lineWidth = Mathf.Clamp(lineWidth, 0.001f, 0.25f);
            lineHeight = Mathf.Max(0.001f, lineHeight);

            EnsureMaterial();
            RebuildLines();
            ApplySettings();
            UpdateLinePositions();
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_lineMaterial);
                }
                else
                {
                    DestroyImmediate(_lineMaterial);
                }

                _lineMaterial = null;
            }
        }

        /// <summary>Adjusts the grid step to match the placer grid size.</summary>
        public void SetGridSize(float size)
        {
            size = Mathf.Max(0.1f, size);
            if (Mathf.Approximately(gridSize, size))
            {
                return;
            }

            gridSize = size;
            UpdateLinePositions();
        }

        /// <summary>Shows or hides the grid object.</summary>
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf == visible)
            {
                return;
            }

            gameObject.SetActive(visible);
        }

        private void EnsureMaterial()
        {
            if (_lineMaterial != null)
            {
                _lineMaterial.color = lineColor;
                return;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                return;
            }

            _lineMaterial = new Material(shader)
            {
                color = lineColor,
                renderQueue = 5000,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void RebuildLines()
        {
            int targetCount = (gridExtent * 2 + 1) * 2;
            while (_lines.Count < targetCount)
            {
                var lineObject = new GameObject("GridLine")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                lineObject.transform.SetParent(transform, false);

                var lineRenderer = lineObject.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = false;
                lineRenderer.positionCount = 2;
                lineRenderer.loop = false;
                lineRenderer.textureMode = LineTextureMode.Stretch;
                _lines.Add(lineRenderer);
            }

            while (_lines.Count > targetCount)
            {
                int index = _lines.Count - 1;
                var line = _lines[index];
                _lines.RemoveAt(index);
                if (line != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(line.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(line.gameObject);
                    }
                }
            }
        }

        private void ApplySettings()
        {
            EnsureMaterial();
            foreach (var line in _lines)
            {
                if (line == null)
                {
                    continue;
                }

                line.startWidth = lineWidth;
                line.endWidth = lineWidth;
                line.startColor = lineColor;
                line.endColor = lineColor;
                line.receiveShadows = false;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.alignment = LineAlignment.View;

                if (_lineMaterial != null)
                {
                    line.sharedMaterial = _lineMaterial;
                }
            }
        }

        private void UpdateLinePositions()
        {
            if (_lines.Count == 0)
            {
                return;
            }

            float min = -gridExtent * gridSize;
            float max = gridExtent * gridSize;
            float y = lineHeight;
            int index = 0;

            for (int x = -gridExtent; x <= gridExtent; x++)
            {
                float pos = x * gridSize;
                var line = _lines[index++];
                if (line != null)
                {
                    line.SetPosition(0, new Vector3(pos, y, min));
                    line.SetPosition(1, new Vector3(pos, y, max));
                }
            }

            for (int z = -gridExtent; z <= gridExtent; z++)
            {
                float pos = z * gridSize;
                var line = _lines[index++];
                if (line != null)
                {
                    line.SetPosition(0, new Vector3(min, y, pos));
                    line.SetPosition(1, new Vector3(max, y, pos));
                }
            }
        }

        private void SetLinesEnabled(bool enabled)
        {
            foreach (var line in _lines)
            {
                if (line != null)
                {
                    line.enabled = enabled;
                }
            }
        }
    }
}
