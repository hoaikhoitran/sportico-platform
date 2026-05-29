using System;
using System.Collections.Generic;

namespace SporticoApp.Shared.Constants
{
    public static class CoachProfileMediaTypes
    {
        public const string Certificate = "certificate";
        public const string Award = "award";
        public const string Gallery = "gallery";
        public const string Identity = "identity";
        public const string Other = "other";

        public static readonly IReadOnlyCollection<string> All = new[]
        {
            Certificate,
            Award,
            Gallery,
            Identity,
            Other
        };

        public static bool IsValid(string? mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                return false;
            }

            foreach (var type in All)
            {
                if (string.Equals(type, mediaType, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
