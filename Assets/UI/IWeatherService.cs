using UnityEngine;

namespace TavernSim.UI
{
    /// <summary>
    /// Interface para serviços de clima que fornecem informações meteorológicas.
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// Obtém o snapshot atual do clima.
        /// </summary>
        WeatherSnapshot GetSnapshot();
    }

    /// <summary>
    /// Snapshot das condições climáticas atuais.
    /// </summary>
    public readonly struct WeatherSnapshot
    {
        public readonly string IconName;
        public readonly int Temperature;
        public readonly string Condition;

        public WeatherSnapshot(string iconName, int temperature, string condition)
        {
            IconName = iconName ?? string.Empty;
            Temperature = temperature;
            Condition = condition ?? string.Empty;
        }

        public string GetDisplayText()
        {
            if (string.IsNullOrWhiteSpace(Condition))
            {
                return $"{Temperature}°C";
            }

            return $"{Condition} • {Temperature}°C";
        }
    }
}

