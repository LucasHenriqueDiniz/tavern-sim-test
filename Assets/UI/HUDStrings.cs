using System.Globalization;

namespace TavernSim.UI
{
    /// <summary>Centraliza textos usados pelo HUD para facilitar localização futura.</summary>
    public static class HUDStrings
    {
        public const string Ready = "⚡ Pronto";
        public const string EmptyOrders = "Sem pedidos no momento 🍺";
        public const string NoSelection = "Nenhum item selecionado.";
        public const string HireWaiter = "Contratar garçom";
        public const string HireCook = "Contratar cozinheiro";
        public const string HireBartender = "Contratar bartender";
        public const string SaveLabel = "Salvar (F5)";
        public const string LoadLabel = "Carregar (F9)";
        public const string BuildToggle = "🛠️ Construir";
        public const string DecoToggle = "🧱 Decoração";
        public const string BeautyToggle = "✨ Beleza";
        public const string LogButton = "📜 Log";
        public const string StaffButton = "👥 Equipe";
        public const string StaffTitle = "Equipe";
        public const string FireAction = "Demitir";
        public const string MoveAction = "Mover";
        public const string SellAction = "Vender item";
        public const string PinHint = "Painel fixado";
        public const string SaveSuccess = "Progresso salvo.";
        public const string SaveFailed = "Erro ao salvar progresso.";
        public const string LoadSuccess = "Progresso carregado.";
        public const string LoadFailed = "Erro ao carregar progresso.";
        public const string LoadUnavailable = "Nenhum save disponível.";

        public static string FormatCurrency(float value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
