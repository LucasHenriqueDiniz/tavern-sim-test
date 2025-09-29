using UnityEngine;

namespace TavernSim.UI
{
    /// <summary>
    /// Gerencia a troca de cursores baseada no estado atual do jogo.
    /// </summary>
    [ExecuteAlways]
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

        private CursorState _currentState = CursorState.Default;
        private bool _isPointerOverHud;
        private bool _isPanActive;
        private bool _isInBuildMode;
        private bool _isInSellMode;
        private bool _hasValidPreview;
        private bool _canAffordPreview;
        private bool _hasHoverAction;

        public void SetPointerOverHud(bool isOverHud)
        {
            _isPointerOverHud = isOverHud;
            UpdateCursor();
        }

        public void SetPanActive(bool isActive)
        {
            _isPanActive = isActive;
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

        public void SetHoverState(bool hasHoverAction)
        {
            _hasHoverAction = hasHoverAction;
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
            EnsureTextures();

            var targetState = CursorState.Default;

            if (_isPointerOverHud)
            {
                targetState = CursorState.Default;
            }
            else if (_isPanActive)
            {
                targetState = CursorState.Pan;
            }
            else if (_isInSellMode)
            {
                targetState = CursorState.Sell;
            }
            else if (_isInBuildMode)
            {
                targetState = (_hasValidPreview && _canAffordPreview) ? CursorState.BuildPlace : CursorState.BuildInvalid;
            }
            else if (_hasHoverAction)
            {
                targetState = CursorState.HoverAction;
            }

            ApplyCursor(targetState);
        }

        private void ApplyCursor(CursorState state)
        {
            if (_currentState == state && Application.isPlaying)
            {
                return;
            }

            _currentState = state;
            var (texture, hotspot) = GetCursorData(state);
            Cursor.SetCursor(texture, hotspot, UnityEngine.CursorMode.Auto);
        }

        private (Texture2D texture, Vector2 hotspot) GetCursorData(CursorState state)
        {
            return state switch
            {
                CursorState.HoverAction => (hoverCursor, hoverHotspot),
                CursorState.BuildPlace => (buildCursor, buildHotspot),
                CursorState.BuildInvalid => (buildInvalidCursor, buildInvalidHotspot),
                CursorState.Sell => (sellCursor, sellHotspot),
                CursorState.Pan => (panCursor, panHotspot),
                _ => (defaultCursor, defaultHotspot)
            };
        }

        private void EnsureTextures()
        {
            defaultCursor ??= LoadCursorTexture("default", "pointer_a");
            hoverCursor ??= LoadCursorTexture("hover", "hand_point");
            buildCursor ??= LoadCursorTexture("build", "tool_hammer");
            buildInvalidCursor ??= LoadCursorTexture("build_invalid", "cross_large");
            sellCursor ??= LoadCursorTexture("sell", "drawing_eraser");
            panCursor ??= LoadCursorTexture("pan", "hand_closed");
        }

        private static Texture2D LoadCursorTexture(string primaryName, string fallbackName)
        {
            var texture = Resources.Load<Texture2D>($"UI/Cursors/{primaryName}");
            if (texture == null && !string.IsNullOrEmpty(fallbackName))
            {
                texture = Resources.Load<Texture2D>($"UI/Cursors/{fallbackName}");
            }

            return texture;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                UpdateCursor();
            }
        }

        private enum CursorState
        {
            Default,
            HoverAction,
            BuildPlace,
            BuildInvalid,
            Sell,
            Pan
        }
    }
}
