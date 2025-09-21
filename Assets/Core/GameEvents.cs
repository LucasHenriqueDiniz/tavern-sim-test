using System;
using System.Collections.Generic;
using UnityEngine;

namespace TavernSim.Core.Events
{
    public enum GameEventSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public struct GameEvent
    {
        public string Type { get; }
        public string Message { get; }
        public int TableId { get; }
        public GameEventSeverity Severity { get; }
        public IReadOnlyDictionary<string, object> Data { get; }

        public GameEvent(string type, string message, GameEventSeverity severity = GameEventSeverity.Info, IDictionary<string, object> data = null, int tableId = -1)
        {
            Type = string.IsNullOrEmpty(type) ? string.Empty : type;
            Message = message ?? string.Empty;
            Severity = severity;
            TableId = tableId;
            Data = data != null ? new Dictionary<string, object>(data) : null;
        }

        public static GameEvent Info(string type, string msg, int tableId = -1)
            => new GameEvent(type, msg, GameEventSeverity.Info, null, tableId);
    }

    public interface IEventBus
    {
        void Publish(GameEvent e);
        void Subscribe(Action<GameEvent> handler);
        void Unsubscribe(Action<GameEvent> handler);
    }

    /// <summary> Implementação simples in-memory; suficiente para runtime e testes. </summary>
    public sealed class GameEventBus : IEventBus
    {
        private readonly List<Action<GameEvent>> _handlers = new(32);

        public void Publish(GameEvent e)
        {
            // evitar modificação durante iteração
            var snapshot = _handlers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try { snapshot[i]?.Invoke(e); } catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        public void Subscribe(Action<GameEvent> handler)
        {
            if (handler != null && !_handlers.Contains(handler)) _handlers.Add(handler);
        }

        public void Unsubscribe(Action<GameEvent> handler)
        {
            if (handler != null) _handlers.Remove(handler);
        }
    }
}
