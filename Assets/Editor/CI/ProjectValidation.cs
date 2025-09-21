using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TavernSim.Editor.CI
{
    public static class ProjectValidation
    {
        private const string MenuPath = "TavernSim/CI/Run Project Validation";

        [MenuItem(MenuPath)]
        public static void ValidateFromMenu()
        {
            try
            {
                Run();
                Debug.Log("Project validation completed without issues.");
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError(ex.Message);
                throw;
            }
        }

        public static void Run()
        {
            var failures = CollectValidationFailures();

            if (failures.Count == 0)
            {
                if (Application.isBatchMode)
                {
                    Debug.Log("Project validation passed.");
                }

                return;
            }

            foreach (var failure in failures)
            {
                Debug.LogError(failure.Message, failure.Context);
            }

            var summary = $"Project validation failed with {failures.Count} issue(s).";

            if (Application.isBatchMode)
            {
                Debug.LogError(summary);
                EditorApplication.Exit(1);
            }
            else
            {
                throw new InvalidOperationException(summary);
            }
        }

        private static IReadOnlyList<ProjectValidationIssue> CollectValidationFailures()
        {
            var failures = new List<ProjectValidationIssue>();

            foreach (var ruleType in TypeCache.GetTypesDerivedFrom<IProjectValidationRule>())
            {
                if (ruleType.IsAbstract || ruleType.IsInterface)
                {
                    continue;
                }

                IProjectValidationRule rule;

                try
                {
                    rule = (IProjectValidationRule)Activator.CreateInstance(ruleType);
                }
                catch (Exception ex)
                {
                    failures.Add(new ProjectValidationIssue(
                        $"Failed to instantiate validation rule {ruleType.FullName}: {ex.Message}"));
                    continue;
                }

                IEnumerable<ProjectValidationIssue> results;

                try
                {
                    results = rule.Validate() ?? Enumerable.Empty<ProjectValidationIssue>();
                }
                catch (Exception ex)
                {
                    failures.Add(new ProjectValidationIssue(
                        $"{GetRuleName(rule, ruleType)} threw an exception: {ex.Message}"));
                    continue;
                }

                foreach (var issue in results)
                {
                    if (string.IsNullOrWhiteSpace(issue.Message))
                    {
                        continue;
                    }

                    var message = issue.RuleName ?? GetRuleName(rule, ruleType);
                    var formattedMessage = string.IsNullOrEmpty(message)
                        ? issue.Message
                        : $"{message}: {issue.Message}";

                    failures.Add(new ProjectValidationIssue(formattedMessage, issue.Context));
                }
            }

            return failures;
        }

        private static string GetRuleName(IProjectValidationRule rule, Type ruleType)
        {
            return string.IsNullOrWhiteSpace(rule?.DisplayName)
                ? ruleType.Name
                : rule!.DisplayName;
        }
    }

    public interface IProjectValidationRule
    {
        string DisplayName { get; }

        IEnumerable<ProjectValidationIssue> Validate();
    }

    public readonly struct ProjectValidationIssue
    {
        public ProjectValidationIssue(string message, UnityEngine.Object context = null, string ruleName = null)
        {
            Message = message;
            Context = context;
            RuleName = ruleName;
        }

        public string Message { get; }

        public UnityEngine.Object Context { get; }

        public string RuleName { get; }
    }
}
