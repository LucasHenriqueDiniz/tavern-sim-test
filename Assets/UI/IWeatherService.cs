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
        public readonly string Icon;
        public readonly int Temperature;
        public readonly string Description;

        public WeatherSnapshot(string icon, int temperature, string description = null)
        {
            Icon = icon;
            Temperature = temperature;
            Description = description ?? GetDefaultDescription(icon);
        }

        private static string GetDefaultDescription(string icon)
        {
            return icon switch
            {
                "☀️" => "Ensolarado",
                "⛅" => "Parcialmente nublado",
                "☁️" => "Nublado",
                "🌧️" => "Chuvoso",
                "⛈️" => "Tempestade",
                "❄️" => "Nevando",
                _ => "Clima normal"
            };
        }

        public override string ToString()
        {
            return $"{Icon} {Temperature}°C";
        }
    }
}

