using System;

namespace TavernSim.Core
{
    /// <summary>
    /// Maintains the current selection and notifies listeners when it changes.
    /// </summary>
    public sealed class SelectionService
    {
        private ISelectable _current;

        public event Action<ISelectable> SelectionChanged;

        public ISelectable Current => _current;

        public void Select(ISelectable selectable)
        {
            if (_current == selectable)
            {
                return;
            }

            _current = selectable;
            SelectionChanged?.Invoke(_current);
        }

        public void ClearSelection()
        {
            if (_current == null)
            {
                return;
            }

            _current = null;
            SelectionChanged?.Invoke(null);
        }
    }
}
