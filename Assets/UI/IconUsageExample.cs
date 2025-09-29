using UnityEngine;
using UnityEngine.UIElements;

namespace TavernSim.UI
{
    /// <summary>
    /// Exemplo de como usar os ícones SVG disponíveis na UI.
    /// </summary>
    public class IconUsageExample : MonoBehaviour
    {
        [Header("Icon References")]
        [SerializeField] private Texture2D weatherSunny;
        [SerializeField] private Texture2D weatherCloudy;
        [SerializeField] private Texture2D weatherRainy;
        [SerializeField] private Texture2D weatherStormy;
        [SerializeField] private Texture2D weatherSnowy;

        private void Start()
        {
            // Exemplo de como carregar ícones dinamicamente
            LoadWeatherIcons();
        }

        private void LoadWeatherIcons()
        {
            // Carregar ícones de clima
            weatherSunny = Resources.Load<Texture2D>("UI/Icons/weather_sunny");
            weatherCloudy = Resources.Load<Texture2D>("UI/Icons/weather_cloudy");
            weatherRainy = Resources.Load<Texture2D>("UI/Icons/weather_rainy");
            weatherStormy = Resources.Load<Texture2D>("UI/Icons/weather_stormy");
            weatherSnowy = Resources.Load<Texture2D>("UI/Icons/weather_snowy");
        }

        /// <summary>
        /// Exemplo de como aplicar ícones a elementos UI
        /// </summary>
        public void ApplyIconToButton(Button button, string iconName)
        {
            if (button == null) return;

            var icon = Resources.Load<Texture2D>($"UI/Icons/{iconName}");
            if (icon != null)
            {
                button.style.backgroundImage = new StyleBackground(icon);
            }
        }

        /// <summary>
        /// Exemplo de como usar ícones em labels
        /// </summary>
        public void SetIconLabel(Label label, string iconName, string text)
        {
            if (label == null) return;

            var icon = Resources.Load<Texture2D>($"UI/Icons/{iconName}");
            if (icon != null)
            {
                label.style.backgroundImage = new StyleBackground(icon);
                label.text = text;
            }
        }

        /// <summary>
        /// Exemplo de ícones disponíveis para diferentes contextos
        /// </summary>
        public static class IconNames
        {
            // Weather icons
            public const string WeatherSunny = "weather_sunny";
            public const string WeatherCloudy = "weather_cloudy";
            public const string WeatherRainy = "weather_rainy";
            public const string WeatherStormy = "weather_stormy";
            public const string WeatherSnowy = "weather_snowy";

            // UI icons
            public const string Settings = "settings";
            public const string Menu = "menu";
            public const string Close = "close";
            public const string Add = "add";
            public const string Remove = "remove";
            public const string Edit = "edit";
            public const string Save = "save";
            public const string Load = "load";

            // Staff icons
            public const string StaffWaiter = "staff_waiter";
            public const string StaffCook = "staff_cook";
            public const string StaffBartender = "staff_bartender";
            public const string StaffCleaner = "staff_cleaner";

            // Building icons
            public const string BuildingTable = "building_table";
            public const string BuildingChair = "building_chair";
            public const string BuildingCounter = "building_counter";
            public const string BuildingKitchen = "building_kitchen";

            // Status icons
            public const string StatusHappy = "status_happy";
            public const string StatusNeutral = "status_neutral";
            public const string StatusAngry = "status_angry";
            public const string StatusBusy = "status_busy";
            public const string StatusIdle = "status_idle";
        }
    }
}

