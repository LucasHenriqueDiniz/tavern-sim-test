using UnityEngine;

namespace TavernSim.UI
{
    /// <summary>
    /// Implementação stub do serviço de clima para desenvolvimento.
    /// </summary>
    public class WeatherServiceStub : MonoBehaviour, IWeatherService
    {
        [SerializeField] private string[] weatherIcons = { "☀️", "⛅", "☁️", "🌧️", "⛈️", "❄️" };
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
            var icon = weatherIcons[Random.Range(0, weatherIcons.Length)];
            var temperature = Random.Range(minTemperature, maxTemperature + 1);
            _currentWeather = new WeatherSnapshot(icon, temperature);
            _lastChangeTime = Time.time;
        }
    }
}

