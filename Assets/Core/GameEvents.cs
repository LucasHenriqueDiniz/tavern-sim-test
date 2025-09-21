using System;
using System.Collections.Generic;

namespace TavernSim.Core
{
    public enum GameEventSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    public readonly struct GameEvent
    {
        public string Code { get; }
        public string Message { get; }
        public GameEventSeverity Severity { get; }
        public IReadOnlyDictionary<string, object> Data { get; }
        public DateTime Time { get; }

        public GameEvent(string code, string message, GameEventSeverity severity, IReadOnlyDictionary<string, object> data = null)
        {
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Severity = severity;
            Data = data ?? EmptyData;
            Time = DateTime.UtcNow;
        }

        private static readonly IReadOnlyDictionary<string, object> EmptyData = new Dictionary<string, object>();
    }

    public interface IEventSink
    {
        void Receive(GameEvent gameEvent);
    }

    public interface IEventBus
    {
        void Publish(GameEvent gameEvent);
        void Subscribe(IEventSink sink);
        void Unsubscribe(IEventSink sink);
    }

    public sealed class GameEventBus : IEventBus
    {
        private readonly List<IEventSink> _sinks = new List<IEventSink>(8);

        public void Publish(GameEvent gameEvent)
        {
            for (int i = 0; i < _sinks.Count; i++)
            {
                _sinks[i]?.Receive(gameEvent);
            }
        }

        public void Subscribe(IEventSink sink)
        {
            if (sink == null || _sinks.Contains(sink))
            {
                return;
            }

            _sinks.Add(sink);
        }

        public void Unsubscribe(IEventSink sink)
        {
            if (sink == null)
            {
                return;
            }

            _sinks.Remove(sink);
        }
    }
}

namespace TavernSim.Core.Events
{
    using GameEvent = TavernSim.Core.GameEvent;
    using GameEventBus = TavernSim.Core.GameEventBus;
    using GameEventSeverity = TavernSim.Core.GameEventSeverity;
    using IEventBus = TavernSim.Core.IEventBus;
    using IEventSink = TavernSim.Core.IEventSink;
}
