#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MVVM.Localization.Editor
{
    internal sealed class LocalizationImportReport
    {
        public readonly List<string> errors = new();
        public readonly List<string> warnings = new();

        public int sourceCount;
        public int tableCount;
        public int keyCount;

        public bool HasErrors => errors.Count > 0;
        public bool HasWarnings => warnings.Count > 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors.Add(message);
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                warnings.Add(message);
        }

        public void Add(bool isError, string message)
        {
            if (isError)
                AddError(message);
            else
                AddWarning(message);
        }

        public string BuildMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Localization validation report: sources={sourceCount}, tables={tableCount}, keys={keyCount}, errors={errors.Count}, warnings={warnings.Count}.");

            if (HasErrors)
            {
                sb.AppendLine("Errors:");
                foreach (var error in errors.Distinct())
                    sb.AppendLine($"- {error}");
            }

            if (HasWarnings)
            {
                sb.AppendLine("Warnings:");
                foreach (var warning in warnings.Distinct())
                    sb.AppendLine($"- {warning}");
            }

            return sb.ToString().TrimEnd();
        }

        public void LogToUnityConsole()
        {
            foreach (var warning in warnings.Distinct())
                UnityEngine.Debug.LogWarning(warning);

            if (HasErrors)
                UnityEngine.Debug.LogError(BuildMessage());
            else if (HasWarnings)
                UnityEngine.Debug.LogWarning(BuildMessage());
        }

        public void ThrowIfErrors()
        {
            if (HasErrors)
                throw new InvalidOperationException(BuildMessage());
        }
    }
}
#endif
