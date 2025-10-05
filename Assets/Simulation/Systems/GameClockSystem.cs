using System;
using UnityEngine;
using TavernSim.Core.Simulation;
using Sim = TavernSim.Core.Simulation.Simulation;

namespace TavernSim.Simulation.Systems
{
    /// <summary>
    /// Lightweight deterministic clock that tracks in-game day and hour based on simulation delta time.
    /// </summary>
    public sealed class GameClockSystem : ISimSystem
    {
        private readonly float _gameMinutesPerRealSecond;
        private float _elapsedMinutes;
        private float _timeScale = 1f;

        public GameClockSystem(float gameMinutesPerRealSecond = 0.5f)
        {
            _gameMinutesPerRealSecond = Mathf.Max(0.01f, gameMinutesPerRealSecond);
            Snapshot = new GameClockSnapshot(1, 8, 0);
        }

        public event Action<GameClockSnapshot> TimeChanged;

        public GameClockSnapshot Snapshot { get; private set; }

        public void Initialize(Sim simulation)
        {
        }

        public void Tick(float deltaTime)
        {
            if (_timeScale <= 0f)
            {
                return;
            }

            _elapsedMinutes += deltaTime * _gameMinutesPerRealSecond * _timeScale;

            if (_elapsedMinutes < 1f)
            {
                return;
            }

            var totalMinutes = Snapshot.Day * 24 * 60 + Snapshot.Hour * 60 + Snapshot.Minute + Mathf.FloorToInt(_elapsedMinutes);
            _elapsedMinutes -= Mathf.Floor(_elapsedMinutes);

            var day = Mathf.Max(1, totalMinutes / (24 * 60));
            var remaining = totalMinutes % (24 * 60);
            var hour = remaining / 60;
            var minute = remaining % 60;

            Snapshot = new GameClockSnapshot(day, hour, minute);
            TimeChanged?.Invoke(Snapshot);
        }

        public void LateTick(float deltaTime)
        {
        }

        public void Dispose()
        {
        }

        public void SetScale(float scale)
        {
            _timeScale = Mathf.Max(0f, scale);
        }

        public void Pause()
        {
            SetScale(0f);
        }

        public void Resume()
        {
            if (_timeScale <= 0f)
            {
                SetScale(1f);
            }
        }

        public void SetSpeed(float speed)
        {
            SetScale(speed);
        }

        public float CurrentScale => _timeScale;

        public readonly struct GameClockSnapshot
        {
            public readonly int Day;
            public readonly int Hour;
            public readonly int Minute;

            public GameClockSnapshot(int day, int hour, int minute)
            {
                Day = Mathf.Max(1, day);
                Hour = Mathf.Clamp(hour, 0, 23);
                Minute = Mathf.Clamp(minute, 0, 59);
            }

            public override string ToString()
            {
                return $"Dia {Day} – {Hour:00}:{Minute:00}";
            }
        }
    }
}
