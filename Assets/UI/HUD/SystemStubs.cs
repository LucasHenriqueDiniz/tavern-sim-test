using System;
using System.Collections.Generic;
using UnityEngine;
using TavernSim.Simulation.Systems;
using TavernSim.Core.Events;
using TavernSim.Building;
using TavernSim.Core;

namespace TavernSim.UI.SystemStubs
{
    /// <summary>
    /// Stubs para APIs de sistemas que ainda não existem ou têm interfaces diferentes
    /// </summary>
    public static class SystemAPIStubs
    {
        // GameClockSystem stubs
        public static void Pause(this GameClockSystem clock)
        {
            if (clock != null)
            {
                clock.SetScale(0f);
            }
            else
            {
                Debug.Log("GameClockSystem.Pause() - stub implementation");
            }
        }

        public static void SetSpeed(this GameClockSystem clock, float speed)
        {
            if (clock != null)
            {
                clock.SetScale(Mathf.Max(0f, speed));
            }
            else
            {
                Debug.Log($"GameClockSystem.SetSpeed({speed}) - stub implementation");
            }
        }

        public static string GetTimeString(this GameClockSystem clock)
        {
            if (clock != null)
            {
                return clock.Snapshot.ToString();
            }

            return "Dia 1 – 08:00"; // Stub implementation
        }
        
        public static GameClockSnapshot GetSnapshot(this GameClockSystem clock) 
        {
            return new GameClockSnapshot { Day = 1, Time = "08:00" };
        }
        
        // EconomySystem stubs
        public static float CurrentCash(this EconomySystem economy) 
        {
            return 100f; // Stub implementation
        }
        
        // ReputationSystem stubs
        public static float CurrentReputation(this ReputationSystem reputation) 
        {
            return 50f; // Stub implementation
        }
        
        // IWeatherService stubs
        public static event Action<WeatherSnapshot> WeatherChanged;
        
        // OrderSystem stubs
        public static event Action OrderPlaced;
        public static event Action OrderReady;
        public static event Action OrderCompleted;
        
        // IEventBus stubs
        public static void Subscribe<T>(this IEventBus eventBus, Action<T> handler) 
        {
            Debug.Log($"IEventBus.Subscribe<{typeof(T).Name}>() - stub implementation");
        }
        
        public static void Unsubscribe<T>(this IEventBus eventBus, Action<T> handler) 
        {
            Debug.Log($"IEventBus.Unsubscribe<{typeof(T).Name}>() - stub implementation");
        }
        
        // BuildCatalog stubs (deprecated helper removed; use catalog.GetEntries(category))
    }
    
    // Estruturas de dados para stubs
    public struct GameClockSnapshot
    {
        public int Day;
        public string Time;
    }
    
    public struct WeatherSnapshot
    {
        public Sprite icon;
        public string temperature;
        public string description;
        public string weatherType;
    }
    
    // BuildCategory já existe em TavernSim.Building, não precisamos redefinir
    
    // Extensões para BuildCatalog.Entry
    public static class BuildCatalogEntryExtensions
    {
        public static string LabelOrId(this BuildCatalog.Entry entry)
        {
            return string.IsNullOrWhiteSpace(entry.Label) ? entry.Id : entry.Label;
        }
        
        public static string DescriptionOrEmpty(this BuildCatalog.Entry entry)
        {
            return entry.Description ?? string.Empty;
        }
    }
    
    // Extensões para BuildCatalog – usar API real GetEntries(category)
}
