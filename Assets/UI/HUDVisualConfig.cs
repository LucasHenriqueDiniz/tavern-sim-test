using UnityEngine;
using UnityEngine.UIElements;

namespace TavernSim.UI
{
    /// <summary>
    /// Holds references to HUD visual assets so runtime bootstrap can assign them without duplicating files.
    /// </summary>
    [CreateAssetMenu(fileName = "HUDVisualConfig", menuName = "TavernSim/UI/HUD Visual Config")]
    public sealed class HUDVisualConfig : ScriptableObject
    {
        [SerializeField] private VisualTreeAsset visualTree;
        [SerializeField] private StyleSheet styleSheet;

        public VisualTreeAsset VisualTree => visualTree;
        public StyleSheet StyleSheet => styleSheet;
    }
}
