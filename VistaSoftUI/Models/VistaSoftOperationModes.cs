using System;
using System.Collections.Generic;
using System.Linq;

namespace VistaSoftUI.Models
{
    public static class VistaSoftOperationModes
    {
        public const string Local = "local";
        public const string Client = "client";
        public const string Server = "server";

        private static readonly IReadOnlyList<string> KnownModes =
        [
            Local,
            Client,
            Server,
        ];

        public static bool IsKnown(string? operationMode)
        {
            return !string.IsNullOrWhiteSpace(operationMode)
                && KnownModes.Contains(operationMode, StringComparer.OrdinalIgnoreCase);
        }

        public static string NormalizeOrDefault(string? operationMode)
        {
            return IsKnown(operationMode)
                ? operationMode!.Trim().ToLowerInvariant()
                : Local;
        }
    }
}
