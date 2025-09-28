using System;

namespace TavernSim.Core.Events
{
    public enum GameEventSeverity { Info, Success, Warning, Error }

    public readonly struct GameEvent
    {
        public readonly string Message;
        public readonly GameEventSeverity Severity;
        public readonly string Source;
        public readonly object Data;

        // Construtor simples
        public GameEvent(string message)
        {
            Message = message; Severity = GameEventSeverity.Info; Source = null; Data = null;
        }

        // === Construtor de 4 argumentos exigido pelos chamadores ===
        public GameEvent(string message, GameEventSeverity severity, string source, object data)
        {
            Message = message; Severity = severity; Source = source; Data = data;
        }

        public override string ToString() => $"[{Severity}] {Source}: {Message}";
    }

    public interface IEventBus
    {
        void Publish(GameEvent ev);
        event Action<GameEvent> OnEvent;
    }

    public sealed class GameEventBus : IEventBus
    {
        public event Action<GameEvent> OnEvent;
        public void Publish(GameEvent ev) => OnEvent?.Invoke(ev);
    }
}
