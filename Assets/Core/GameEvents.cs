using System;
using System.Collections.Generic;
using UnityEngine;

namespace TavernSim.Core.Events
{
    public struct GameEvent
    {
        public string Type;     // ex: "CustomerAngry", "MenuBlocked", "NoIngredients"
        public string Message;  // exibição no HUD
        public int TableId;     // opcional
        public static GameEvent Info(string type, string msg, int tableId = -1)
            => new GameEvent { Type = type, Message = msg, TableId = tableId };
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
        private readonly List<Action<GameEvent>> _handlers = new List<Action<GameEvent>>(32);

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
