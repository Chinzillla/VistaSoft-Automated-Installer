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

        public static string NormalizeOrDefault(string? operationMode)
        {
            if (string.IsNullOrWhiteSpace(operationMode))
            {
                return Local;
            }

            string normalizedOperationMode = operationMode.Trim().ToLowerInvariant();

            return KnownModes.Contains(normalizedOperationMode, StringComparer.OrdinalIgnoreCase)
                ? normalizedOperationMode
                : Local;
        }
    }
}
