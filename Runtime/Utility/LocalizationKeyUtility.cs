using System;
using System.Collections.Generic;
using System.Linq;

namespace MVVM.Localization
{
    internal static class LocalizationKeyUtility
    {
        public static string NormalizeKey(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace('\\', '.')
                .Replace('/', '.')
                .ToLowerInvariant();
        }

        public static string NormalizeLanguageCode(string value)
            => (value ?? string.Empty).Trim().ToLowerInvariant();

        public static string NormalizeTableId(string value)
            => NormalizeKey(value);

        public static string NormalizeSourceId(string value)
            => NormalizeKey(value);

        public static string BuildTranslationId(string sourceId, string tableId, string rawKey, bool prefixSourceId)
        {
            var normalizedSourceId = NormalizeSourceId(sourceId);
            var normalizedTableId = NormalizeTableId(tableId);
            var normalizedKey = NormalizeKey(rawKey);

            var segments = new List<string>(3);
            if (prefixSourceId && !string.IsNullOrWhiteSpace(normalizedSourceId))
                segments.Add(normalizedSourceId);

            if (!string.IsNullOrWhiteSpace(normalizedTableId))
                segments.Add(normalizedTableId);

            if (!string.IsNullOrWhiteSpace(normalizedKey))
                segments.Add(normalizedKey);

            return string.Join('.', segments);
        }

        public static string SanitizeAssetPathSegment(string value)
        {
            return string.Concat((value ?? string.Empty).Select(c => PathInvalidChars.Contains(c) ? '_' : c));
        }

        private static readonly HashSet<char> PathInvalidChars = new(System.IO.Path.GetInvalidFileNameChars());
    }
}
