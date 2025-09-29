using System.IO;
using UnityEngine;
using TavernSim.Simulation.Systems;

namespace TavernSim.Save
{
    /// <summary>
    /// Simple JSON based save/load service. Designed for deterministic state.
    /// </summary>
    public sealed class SaveService
    {
        private readonly EconomySystem _economySystem;

        public SaveService(EconomySystem economySystem)
        {
            _economySystem = economySystem;
        }

        public string GetDefaultPath()
        {
            return Path.Combine(Application.persistentDataPath, "tavernsim_save.json");
        }

        public void Save(string path, SaveModel model)
        {
            var json = JsonUtility.ToJson(model, true);
            File.WriteAllText(path, json);
        }

        public bool HasSave(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = GetDefaultPath();
            }

            return File.Exists(path);
        }

        public SaveModel CreateModel()
        {
            return new SaveModel
            {
                cash = _economySystem.Cash
            };
        }

        public void Load(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var json = File.ReadAllText(path);
            var model = JsonUtility.FromJson<SaveModel>(json);
            if (model == null)
            {
                return;
            }

            if (model.version != 1)
            {
                Debug.LogWarning($"Unsupported save version {model.version}");
                return;
            }

            var delta = model.cash - _economySystem.Cash;
            if (delta > 0f)
            {
                _economySystem.AddRevenue(delta);
            }
            else if (delta < 0f)
            {
                _economySystem.TrySpend(-delta);
            }
        }
    }
}
