using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SporticoApp.Application.DTOs.Advisory;
using SporticoApp.Application.Interfaces.Services;
using SporticoApp.Infrastructure.Persistence;
using SporticoApp.Shared.Constants;
using SporticoApp.Shared.Exceptions;

namespace SporticoApp.Infrastructure.Services.Advisory
{
    public class GeminiAdvisoryService : IGeminiAdvisoryService
    {
        /// <summary>Cap on how many packages / coaches are pulled into the prompt context.</summary>
        private const int MaxCatalogRecords = 30;

        private const string FallbackReply =
            "Sorry, I couldn't generate advice right now. Please try rephrasing your question.";

        private readonly HttpClient _httpClient;
        private readonly GeminiSettings _settings;
        private readonly AppDbContext _context;
        private readonly ILogger<GeminiAdvisoryService> _logger;

        public GeminiAdvisoryService(
            HttpClient httpClient,
            IOptions<GeminiSettings> settings,
            AppDbContext context,
            ILogger<GeminiAdvisoryService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _context = context;
            _logger = logger;
        }

        public async Task<GeminiAdvisoryResult> GenerateReplyAsync(
            GeminiAdvisoryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                throw new FailureException(
                    ErrorCodes.AdvisoryReplyFailed,
                    "Advisory chatbot is not configured",
                    new List<string> { "Gemini__ApiKey" });
            }

            var coaches = await GetActiveCoachesAsync(cancellationToken);
            var packages = await GetActivePackagesAsync(cancellationToken);

            var validCoachIds = coaches.Select(c => c.CoachId).ToHashSet();

            var body = BuildRequestBody(request, coaches, packages);

            var model = string.IsNullOrWhiteSpace(_settings.Model)
                ? "gemini-2.0-flash"
                : _settings.Model;

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/v1beta/models/{model}:generateContent?key={_settings.ApiKey}")
            {
                Content = JsonContent.Create(body)
            };

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Log the HTTP status only — never the API key or full URL.
                _logger.LogError(
                    "Gemini generateContent failed with status {StatusCode}: {RawJson}",
                    (int)response.StatusCode,
                    rawJson);

                throw new FailureException(
                    ErrorCodes.AdvisoryReplyFailed,
                    "Failed to generate advisory reply",
                    new List<string> { $"StatusCode: {(int)response.StatusCode}" });
            }

            var modelText = ExtractModelText(rawJson);

            return ParseAdvisoryResult(modelText, validCoachIds);
        }

        // ── Catalog queries ──────────────────────────────────────────────────

        private async Task<List<CoachCandidate>> GetActiveCoachesAsync(CancellationToken ct)
        {
            return await _context.CoachProfiles
                .AsNoTracking()
                .Where(c => c.TrainingPackages.Any(p => p.Status == TrainingPackageStatuses.Published))
                .OrderByDescending(c => c.Rating)
                .Take(MaxCatalogRecords)
                .Select(c => new CoachCandidate
                {
                    CoachId = c.UserId,
                    Name = c.User.FullName,
                    Headline = c.Headline,
                    Specialties = c.Specialties,
                    Rating = c.Rating,
                    ExperienceYears = c.ExperienceYears
                })
                .ToListAsync(ct);
        }

        private async Task<List<PackageCandidate>> GetActivePackagesAsync(CancellationToken ct)
        {
            return await _context.TrainingPackages
                .AsNoTracking()
                .Where(p => p.Status == TrainingPackageStatuses.Published)
                .OrderByDescending(p => p.CreatedAt)
                .Take(MaxCatalogRecords)
                .Select(p => new PackageCandidate
                {
                    Title = p.Title,
                    Sport = p.Sport.Name,
                    CoachId = p.CoachId,
                    CoachName = p.Coach.User.FullName,
                    Price = p.Price,
                    Level = p.Level,
                    GoalType = p.GoalType,
                    IsOnline = p.IsOnline
                })
                .ToListAsync(ct);
        }

        // ── Request building ─────────────────────────────────────────────────

        private static object BuildRequestBody(
            GeminiAdvisoryRequest request,
            List<CoachCandidate> coaches,
            List<PackageCandidate> packages)
        {
            var systemInstruction = BuildSystemInstruction(coaches, packages);

            var contents = new List<object>();

            foreach (var turn in request.History)
            {
                // Gemini roles: "user" for the learner/admin, "model" for the assistant.
                var role = string.Equals(turn.Sender, AdvisorySenderConstants.Assistant, StringComparison.OrdinalIgnoreCase)
                    ? "model"
                    : "user";

                contents.Add(new
                {
                    role,
                    parts = new[] { new { text = turn.Content } }
                });
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = request.UserMessage } }
            });

            return new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemInstruction } }
                },
                contents,
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0.4
                }
            };
        }

        private static string BuildSystemInstruction(
            List<CoachCandidate> coaches,
            List<PackageCandidate> packages)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "You are Sportico's sports-coaching advisory assistant. You help users (learners and admins) " +
                "with practical, encouraging sports-training advice and recommend suitable coaches from the catalog below.");
            sb.AppendLine();
            sb.AppendLine("ACTIVE COACHES (recommend only from this list, by their exact coachId):");
            if (coaches.Count == 0)
            {
                sb.AppendLine("(none available)");
            }
            else
            {
                foreach (var c in coaches)
                {
                    sb.AppendLine(
                        $"- coachId={c.CoachId} | name={c.Name} | rating={c.Rating:0.0} | " +
                        $"experienceYears={c.ExperienceYears?.ToString() ?? "n/a"} | " +
                        $"specialties={Clean(c.Specialties)} | headline={Clean(c.Headline)}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("ACTIVE TRAINING PACKAGES:");
            if (packages.Count == 0)
            {
                sb.AppendLine("(none available)");
            }
            else
            {
                foreach (var p in packages)
                {
                    sb.AppendLine(
                        $"- title={Clean(p.Title)} | sport={Clean(p.Sport)} | coachId={p.CoachId} | " +
                        $"coach={Clean(p.CoachName)} | price={p.Price} | level={Clean(p.Level)} | " +
                        $"goal={Clean(p.GoalType)} | mode={(p.IsOnline ? "online" : "offline")}");
                }
            }

            sb.AppendLine();
            sb.AppendLine(
                "Respond with ONLY a JSON object of this exact shape: " +
                "{\"reply\": string, \"recommendedCoachIds\": string[]}. " +
                "\"reply\" is your advice in the user's language. " +
                "\"recommendedCoachIds\" contains 0..N coachId values copied verbatim from the ACTIVE COACHES list " +
                "that best match the user's needs — never invent ids, and use an empty array when nothing fits. " +
                "Do not include any text outside the JSON object.");

            return sb.ToString();
        }

        private static string Clean(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "n/a";
            }

            // Collapse newlines so each catalog entry stays on a single prompt line.
            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        // ── Response parsing ─────────────────────────────────────────────────

        /// <summary>
        /// Pulls the model's text out of the generateContent envelope. Returns null when the
        /// expected candidates/parts shape is missing so the caller can fall back gracefully.
        /// </summary>
        private string? ExtractModelText(string rawJson)
        {
            try
            {
                using var document = JsonDocument.Parse(rawJson);
                var root = document.RootElement;

                if (!root.TryGetProperty("candidates", out var candidates) ||
                    candidates.ValueKind != JsonValueKind.Array ||
                    candidates.GetArrayLength() == 0)
                {
                    return null;
                }

                var first = candidates[0];
                if (!first.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array ||
                    parts.GetArrayLength() == 0)
                {
                    return null;
                }

                var builder = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var text) &&
                        text.ValueKind == JsonValueKind.String)
                    {
                        builder.Append(text.GetString());
                    }
                }

                var combined = builder.ToString();
                return string.IsNullOrWhiteSpace(combined) ? null : combined;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to read Gemini response envelope");
                return null;
            }
        }

        /// <summary>
        /// Safely parses the model text as {reply, recommendedCoachIds[]}. Falls back to a plain
        /// reply (and no recommendations) whenever the model returns malformed or non-JSON output.
        /// </summary>
        private GeminiAdvisoryResult ParseAdvisoryResult(string? modelText, HashSet<Guid> validCoachIds)
        {
            if (string.IsNullOrWhiteSpace(modelText))
            {
                return new GeminiAdvisoryResult { Reply = FallbackReply };
            }

            try
            {
                using var document = JsonDocument.Parse(modelText);
                var root = document.RootElement;

                var reply = root.TryGetProperty("reply", out var replyProp) &&
                            replyProp.ValueKind == JsonValueKind.String
                    ? replyProp.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(reply))
                {
                    // Valid JSON but no usable reply — surface the raw text rather than nothing.
                    reply = modelText.Trim();
                }

                var recommendedCoachIds = new List<Guid>();
                if (root.TryGetProperty("recommendedCoachIds", out var idsProp) &&
                    idsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in idsProp.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String &&
                            Guid.TryParse(item.GetString(), out var id) &&
                            validCoachIds.Contains(id) &&
                            !recommendedCoachIds.Contains(id))
                        {
                            recommendedCoachIds.Add(id);
                        }
                    }
                }

                return new GeminiAdvisoryResult
                {
                    Reply = reply!,
                    RecommendedCoachIds = recommendedCoachIds
                };
            }
            catch (JsonException ex)
            {
                // Malformed JSON from the model: fall back to returning its raw text as the reply.
                _logger.LogWarning(ex, "Gemini returned non-JSON advisory output; falling back to raw text");

                return new GeminiAdvisoryResult { Reply = modelText.Trim() };
            }
        }

        private sealed class CoachCandidate
        {
            public Guid CoachId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Headline { get; set; }
            public string? Specialties { get; set; }
            public decimal Rating { get; set; }
            public int? ExperienceYears { get; set; }
        }

        private sealed class PackageCandidate
        {
            public string Title { get; set; } = string.Empty;
            public string Sport { get; set; } = string.Empty;
            public Guid CoachId { get; set; }
            public string CoachName { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string? Level { get; set; }
            public string? GoalType { get; set; }
            public bool IsOnline { get; set; }
        }
    }
}
