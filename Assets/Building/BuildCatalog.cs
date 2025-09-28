using System;
using System.Collections.Generic;
using UnityEngine;

namespace TavernSim.Building
{
    public enum BuildCategory
    {
        Build,
        Deco
    }

    [CreateAssetMenu(fileName = "BuildCatalog", menuName = "TavernSim/Build Catalog")]
    public sealed class BuildCatalog : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new();

        private readonly Dictionary<GridPlacer.PlaceableKind, Entry> _cache = new();

        public IReadOnlyList<Entry> Entries => entries;

        public IEnumerable<Entry> GetEntries(BuildCategory category)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Category == category)
                {
                    yield return entries[i];
                }
            }
        }

        public bool TryGetEntry(GridPlacer.PlaceableKind kind, out Entry entry)
        {
            if (_cache.TryGetValue(kind, out entry))
            {
                return true;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var candidate = entries[i];
                if (candidate.Kind == kind)
                {
                    _cache[kind] = candidate;
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public float GetCost(GridPlacer.PlaceableKind kind)
        {
            return TryGetEntry(kind, out var entry) ? Mathf.Max(0f, entry.Cost) : 0f;
        }

        public static BuildCatalog CreateDefault()
        {
            var catalog = CreateInstance<BuildCatalog>();
            catalog.entries = new List<Entry>
            {
                new("small_table", "Mesa pequena", GridPlacer.PlaceableKind.SmallTable, BuildCategory.Build, 125f, "Ideal para duplas."),
                new("large_table", "Mesa grande", GridPlacer.PlaceableKind.LargeTable, BuildCategory.Build, 220f, "Acomoda grupos maiores."),
                new("decor_plant", "Planta decorativa", GridPlacer.PlaceableKind.Decoration, BuildCategory.Deco, 30f, "Aumenta a beleza local."),
                new("kitchen_station", "Estação de cozinha", GridPlacer.PlaceableKind.KitchenStation, BuildCategory.Build, 420f, "Desbloqueia receitas quentes."),
                new("bar_counter", "Balcão do bar", GridPlacer.PlaceableKind.BarCounter, BuildCategory.Build, 360f, "Serve bebidas rapidamente."),
                new("pickup_point", "Ponto de retirada", GridPlacer.PlaceableKind.PickupPoint, BuildCategory.Build, 0f, "Define onde a equipe pega pedidos prontos."),
            };
            return catalog;
        }

        [Serializable]
        public struct Entry
        {
            [SerializeField] private string id;
            [SerializeField] private string label;
            [SerializeField] private GridPlacer.PlaceableKind kind;
            [SerializeField] private BuildCategory category;
            [SerializeField] private float cost;
            [SerializeField] [TextArea] private string description;
            [SerializeField] private Sprite icon;

            public Entry(string id, string label, GridPlacer.PlaceableKind kind, BuildCategory category, float cost, string description)
            {
                this.id = id;
                this.label = label;
                this.kind = kind;
                this.category = category;
                this.cost = cost;
                this.description = description;
                icon = null;
            }

            public string Id => string.IsNullOrWhiteSpace(id) ? kind.ToString() : id;
            public string Label => string.IsNullOrWhiteSpace(label) ? kind.ToString() : label;
            public GridPlacer.PlaceableKind Kind => kind;
            public BuildCategory Category => category;
            public float Cost => cost;
            public string Description => description;
            public Sprite Icon => icon;
        }
    }
}
