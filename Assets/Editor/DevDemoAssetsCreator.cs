#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using TavernSim.Domain;
using TavernSim.UI;

namespace TavernSim.Editor
{
    /// <summary>
    /// Creates demo ScriptableObjects on first run to make the MVP immediately playable.
    /// </summary>
    public static class DevDemoAssetsCreator
    {
        private const string RootPath = "Assets/ScriptableObjects";
        private const string ItemPath = RootPath + "/Ale.asset";
        private const string RecipePath = RootPath + "/AlePint.asset";
        private const string CatalogPath = RootPath + "/DemoCatalog.asset";
        private const string HudConfigFolder = "Assets/Resources/UI";
        private const string HudConfigPath = HudConfigFolder + "/HUDVisualConfig.asset";
        private const string HudUxmlPath = "Assets/UI/UXML/HUD.uxml";
        private const string HudUssPath = "Assets/UI/USS/HUD.uss";

        [InitializeOnLoadMethod]
        private static void EnsureAssets()
        {
            EnsureFolder(RootPath);
            EnsureFolder(HudConfigFolder);

            var item = LoadOrCreate<ItemSO>(ItemPath);
            ConfigureAle(item);

            var recipe = LoadOrCreate<RecipeSO>(RecipePath);
            ConfigureAleRecipe(recipe, item);

            var catalog = LoadOrCreate<Catalog>(CatalogPath);
            ConfigureCatalog(catalog, item, recipe);

            EnsureHudVisualConfig();

            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string targetPath)
        {
            if (AssetDatabase.IsValidFolder(targetPath))
            {
                return;
            }

            var parts = targetPath.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void ConfigureAle(ItemSO item)
        {
            var so = new SerializedObject(item);
            so.FindProperty("id").stringValue = "ale";
            so.FindProperty("displayName").stringValue = "Ale";
            so.FindProperty("unitCost").intValue = 2;
            so.FindProperty("sellPrice").intValue = 6;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void ConfigureAleRecipe(RecipeSO recipe, ItemSO output)
        {
            var so = new SerializedObject(recipe);
            so.FindProperty("id").stringValue = "ale_pint";
            so.FindProperty("displayName").stringValue = "Ale Pint";
            so.FindProperty("prepTime").floatValue = 3f;
            var ingredients = so.FindProperty("ingredients");
            ingredients.arraySize = 1;
            ingredients.GetArrayElementAtIndex(0).objectReferenceValue = output;
            so.FindProperty("outputItem").objectReferenceValue = output;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(recipe);
        }

        private static void ConfigureCatalog(Catalog catalog, ItemSO item, RecipeSO recipe)
        {
            var so = new SerializedObject(catalog);
            var itemsProp = so.FindProperty("items");
            itemsProp.arraySize = 1;
            itemsProp.GetArrayElementAtIndex(0).objectReferenceValue = item;
            var recipesProp = so.FindProperty("recipes");
            recipesProp.arraySize = 1;
            recipesProp.GetArrayElementAtIndex(0).objectReferenceValue = recipe;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureHudVisualConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<HUDVisualConfig>(HudConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<HUDVisualConfig>();
                AssetDatabase.CreateAsset(config, HudConfigPath);
            }

            var so = new SerializedObject(config);
            so.FindProperty("visualTree").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxmlPath);
            so.FindProperty("styleSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StyleSheet>(HudUssPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }
}
#endif
