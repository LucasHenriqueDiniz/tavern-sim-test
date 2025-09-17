#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TavernSim.Domain;

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

        [InitializeOnLoadMethod]
        private static void EnsureAssets()
        {
            if (!AssetDatabase.IsValidFolder(RootPath))
            {
                var parts = RootPath.Split('/');
                var current = "Assets";
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

            var item = LoadOrCreate<ItemSO>(ItemPath);
            ConfigureAle(item);

            var recipe = LoadOrCreate<RecipeSO>(RecipePath);
            ConfigureAleRecipe(recipe, item);

            var catalog = LoadOrCreate<Catalog>(CatalogPath);
            ConfigureCatalog(catalog, item, recipe);

            AssetDatabase.SaveAssets();
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
    }
}
#endif
