#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace TavernSim.Editor.CI
{
    public static class ProjectValidation
    {
        [MenuItem("Tools/TavernSim/Validate Project")]
        public static void ValidateFromMenu() => Run();

        public static void Run()
        {
            try
            {
                var failures = new List<string>(16);

                var expected = "2022.3.62f1";
                if (!Application.unityVersion.StartsWith("2022.3."))
                    failures.Add($"Unity LTS esperado 2022.3.x; detectado {Application.unityVersion}. Atualize a doc/CI se for intencional.");
                else if (Application.unityVersion != expected)
                    Debug.LogWarning($"[Validation] Versão diferente da recomendada ({expected}); detectado {Application.unityVersion}.");

                if (!HasPackage("com.unity.ai.navigation", out var navVersion))
                    failures.Add("Pacote AI Navigation (com.unity.ai.navigation) ausente — necessário para NavMeshSurface/NavMeshAgent em CI. Veja Package Manager.");
                else
                    Debug.Log($"[Validation] AI Navigation OK (v{navVersion}).");

                RequireType("Unity.AI.Navigation.NavMeshSurface", failures);
                RequireType("UnityEngine.AI.NavMeshAgent", failures);

                RequireMonoBehaviour("TavernSim.Agents.Customer", failures);
                RequireMonoBehaviour("TavernSim.Agents.Waiter", failures);

                var dups = FindDuplicateTypes();
                if (dups.Count > 0)
                {
                    foreach (var kv in dups)
                        failures.Add($"Tipo duplicado: {kv.Key} em [{string.Join(", ", kv.Value)}]");
                }

                var metaWarnings = FindOrphanMetas();
                foreach (var w in metaWarnings) Debug.LogWarning(w);

                if (failures.Count > 0)
                {
                    foreach (var f in failures) Debug.LogError($"[Validation] {f}");
                    EditorApplication.Exit(1);
                }
                else
                {
                    Debug.Log("[Validation] OK ✔ Projeto válido para CI/PR.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Validation] Exceção: " + ex);
                EditorApplication.Exit(1);
            }
        }

        private static bool HasPackage(string packageName, out string version)
        {
            version = null;
            ListRequest req = Client.List(true);
            while (!req.IsCompleted) { }
            if (req.Status == StatusCode.Failure || req.Result == null) return false;
            var p = req.Result.FirstOrDefault(r => r.name == packageName);
            if (p == null) return false;
            version = p.version;
            return true;
        }

        private static void RequireType(string fullName, List<string> failures)
        {
            var t = Type.GetType(fullName) ?? AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a => a.GetType(fullName))
                .FirstOrDefault(x => x != null);
            if (t == null) failures.Add($"Tipo obrigatório ausente: {fullName}");
        }

        private static void RequireMonoBehaviour(string fullName, List<string> failures)
        {
            var t = Type.GetType(fullName) ?? AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a => a.GetType(fullName))
                .FirstOrDefault(x => x != null);

            if (t == null) { failures.Add($"Classe obrigatória ausente: {fullName}"); return; }
            if (!typeof(MonoBehaviour).IsAssignableFrom(t))
                failures.Add($"{fullName} deve herdar de MonoBehaviour.");
        }

        private static Dictionary<string, List<string>> FindDuplicateTypes()
        {
            var map = new Dictionary<string, HashSet<string>>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.FullName.StartsWith("System") || asm.FullName.StartsWith("Unity")) continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(x => x != null).ToArray(); }

                foreach (var t in types)
                {
                    var full = t.FullName;
                    if (string.IsNullOrEmpty(full)) continue;
                    if (!map.TryGetValue(full, out var set)) map[full] = set = new HashSet<string>();
                    set.Add(asm.GetName().Name);
                }
            }

            return map.Where(kv => kv.Value.Count > 1)
                      .ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
        }

        private static IEnumerable<string> FindOrphanMetas()
        {
            var warnings = new List<string>();
            var guids = AssetDatabase.FindAssets(string.Empty);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || path.EndsWith(".meta")) continue;
                var metaPath = path + ".meta";
                if (!System.IO.File.Exists(metaPath))
                {
                    warnings.Add($"[Validation] Possível .meta ausente: {path}");
                }
            }
            return warnings;
        }
    }
}
#endif
