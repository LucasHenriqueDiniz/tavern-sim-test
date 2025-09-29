using UnityEngine;

namespace TavernSim.UI
{
    /// <summary>
    /// Implementação stub do serviço de clima para desenvolvimento.
    /// </summary>
    public class WeatherServiceStub : MonoBehaviour, IWeatherService
    {
        [SerializeField]
        private WeatherPreset[] presets =
        {
            new WeatherPreset("weather-sun", "Ensolarado"),
            new WeatherPreset("weather-partly", "Parcialmente nublado"),
            new WeatherPreset("weather-cloud", "Nublado"),
            new WeatherPreset("weather-rain", "Chuvoso"),
            new WeatherPreset("weather-storm", "Tempestade"),
            new WeatherPreset("weather-snow", "Nevando")
        };
        [SerializeField] private int minTemperature = 15;
        [SerializeField] private int maxTemperature = 35;
        [SerializeField] private float changeInterval = 30f; // segundos

        private WeatherSnapshot _currentWeather;
        private float _lastChangeTime;

        private void Start()
        {
            GenerateRandomWeather();
        }

        private void Update()
        {
            if (Time.time - _lastChangeTime >= changeInterval)
            {
                GenerateRandomWeather();
            }
        }

        public WeatherSnapshot GetSnapshot()
        {
            return _currentWeather;
        }

        private void GenerateRandomWeather()
        {
            if (presets == null || presets.Length == 0)
            {
                _currentWeather = new WeatherSnapshot("weather-sun", Random.Range(minTemperature, maxTemperature + 1), "Ensolarado");
                _lastChangeTime = Time.time;
                return;
            }

            var preset = presets[Random.Range(0, presets.Length)];
            var temperature = Random.Range(minTemperature, maxTemperature + 1);
            _currentWeather = new WeatherSnapshot(preset.IconName, temperature, preset.Condition);
            _lastChangeTime = Time.time;
        }

        [System.Serializable]
        private struct WeatherPreset
        {
            public string IconName;
            public string Condition;

            public WeatherPreset(string iconName, string condition)
            {
                IconName = iconName;
                Condition = condition;
            }
        }
    }
}

