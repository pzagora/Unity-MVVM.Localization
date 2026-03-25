using static Commons.Extensions.NumericExtensions;
using System;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace MVVM.Localization
{
    public abstract class LocalizationFormatter
    {
        private const string LOG_MISSING_ARGS = "Localized string requires format args but none were provided. Key={0}, Template={1}";
        private const string LOG_BAD_PLACEHOLDERS = "Localized string format failed. Key={0}, Template={1}, Args={2}, Error={3}";

        public static string Format(string key, string template, params object[] args)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            args ??= Array.Empty<object>();

            if (args.Length == Zero)
            {
                if (LooksLikeCompositeFormat(template))
                    Report.Warning<LocalizationFormatter>(string.Format(LOG_MISSING_ARGS, key ?? string.Empty, template));

                return template;
            }

            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException ex)
            {
                Report.Warning<LocalizationFormatter>(string.Format(
                    LOG_BAD_PLACEHOLDERS,
                    key ?? string.Empty,
                    template,
                    BuildArgsPreview(args),
                    ex.Message));

                return template;
            }
            catch (Exception ex)
            {
                Report.Warning<LocalizationFormatter>(string.Format(
                    LOG_BAD_PLACEHOLDERS,
                    key ?? string.Empty,
                    template,
                    BuildArgsPreview(args),
                    ex.Message));

                return template;
            }
        }

        private static bool LooksLikeCompositeFormat(string template)
        {
            for (var i = Zero; i < template.Length; i++)
            {
                if (template[i] != '{' || i + One >= template.Length)
                    continue;

                if (template[i + One] == '{')
                {
                    i++;
                    continue;
                }

                return char.IsDigit(template[i + One]);
            }

            return false;
        }

        private static string BuildArgsPreview(object[] args)
        {
            if (args == null || args.Length == Zero)
                return "[]";

            return "[" + string.Join(", ", args.Select(arg => arg?.ToString() ?? "null")) + "]";
        }
    }
}
