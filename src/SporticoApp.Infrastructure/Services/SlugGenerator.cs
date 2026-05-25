using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SporticoApp.Application.Interfaces.Services;

namespace SporticoApp.Infrastructure.Services
{
    public class SlugGenerator : ISlugGenerator
    {
        public string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var trimmed = input.Trim().ToLowerInvariant();
            var normalized = trimmed.Normalize(NormalizationForm.FormD);

            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            var noDiacritics = builder
                .ToString()
                .Normalize(NormalizationForm.FormC);

            var slug = Regex.Replace(noDiacritics, "[^a-z0-9]+", "-");
            slug = Regex.Replace(slug, "-+", "-").Trim('-');

            return slug;
        }
    }
}
