using UnityEngine;
using TavernSim.Building;

namespace TavernSim.UI
{
    /// <summary>
    /// Gerencia a troca de cursores baseada no estado atual do jogo.
    /// </summary>
    public class CursorManager : MonoBehaviour
    {
        [Header("Cursor Textures")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D hoverCursor;
        [SerializeField] private Texture2D buildCursor;
        [SerializeField] private Texture2D buildInvalidCursor;
        [SerializeField] private Texture2D sellCursor;
        [SerializeField] private Texture2D panCursor;

        [Header("Hotspots")]
        [SerializeField] private Vector2 defaultHotspot = new Vector2(6, 6);
        [SerializeField] private Vector2 hoverHotspot = new Vector2(6, 6);
        [SerializeField] private Vector2 buildHotspot = new Vector2(6, 6);
        [SerializeField] private Vector2 buildInvalidHotspot = new Vector2(6, 6);
        [SerializeField] private Vector2 sellHotspot = new Vector2(6, 6);
        [SerializeField] private Vector2 panHotspot = new Vector2(16, 16);

        private void Awake()
        {
            // Load cursor textures if not assigned
            if (defaultCursor == null)
                defaultCursor = Resources.Load<Texture2D>("UI/Cursors/pointer_a");
            if (hoverCursor == null)
                hoverCursor = Resources.Load<Texture2D>("UI/Cursors/hand_point");
            if (buildCursor == null)
                buildCursor = Resources.Load<Texture2D>("UI/Cursors/tool_hammer");
            if (buildInvalidCursor == null)
                buildInvalidCursor = Resources.Load<Texture2D>("UI/Cursors/cross_large");
            if (sellCursor == null)
                sellCursor = Resources.Load<Texture2D>("UI/Cursors/drawing_eraser");
            if (panCursor == null)
                panCursor = Resources.Load<Texture2D>("UI/Cursors/hand_closed");
        }

        private CursorMode _currentMode = CursorMode.Default;
        private bool _isPointerOverHud;
        private bool _isRightMouseDown;
        private bool _isInBuildMode;
        private bool _isInSellMode;
        private bool _hasValidPreview;
        private bool _canAffordPreview;

        public void SetPointerOverHud(bool isOverHud)
        {
            _isPointerOverHud = isOverHud;
            UpdateCursor();
        }

        public void SetRightMouseDown(bool isDown)
        {
            _isRightMouseDown = isDown;
            UpdateCursor();
        }

        public void SetBuildMode(bool inBuildMode)
        {
            _isInBuildMode = inBuildMode;
            UpdateCursor();
        }

        public void SetSellMode(bool inSellMode)
        {
            _isInSellMode = inSellMode;
            UpdateCursor();
        }

        public void SetPreviewState(bool hasValid, bool canAfford)
        {
            _hasValidPreview = hasValid;
            _canAffordPreview = canAfford;
            UpdateCursor();
        }

        private void UpdateCursor()
        {
            if (_isPointerOverHud)
            {
                SetCursor(CursorMode.Default, defaultCursor, defaultHotspot);
                return;
            }

            if (_isRightMouseDown)
            {
                SetCursor(CursorMode.Pan, panCursor, panHotspot);
                return;
            }

            if (_isInSellMode)
            {
                SetCursor(CursorMode.Sell, sellCursor, sellHotspot);
                return;
            }

            if (_isInBuildMode)
            {
                if (_hasValidPreview && _canAffordPreview)
                {
                    SetCursor(CursorMode.Build, buildCursor, buildHotspot);
                }
                else
                {
                    SetCursor(CursorMode.BuildInvalid, buildInvalidCursor, buildInvalidHotspot);
                }
                return;
            }

            SetCursor(CursorMode.Default, defaultCursor, defaultHotspot);
        }

        private void SetCursor(CursorMode mode, Texture2D texture, Vector2 hotspot)
        {
            if (_currentMode == mode && texture != null)
            {
                return;
            }

            _currentMode = mode;
            if (texture != null)
            {
                Cursor.SetCursor(texture, hotspot, UnityEngine.CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, UnityEngine.CursorMode.Auto);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateCursor();
            }
        }

        private enum CursorMode
        {
            Default,
            Hover,
            Build,
            BuildInvalid,
            Sell,
            Pan
        }
    }
}
