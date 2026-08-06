namespace SporticoApp.Shared.Configuration
{
    /// <summary>Outcome of an <see cref="EnvironmentFileLoader"/> load, safe to log verbatim.</summary>
    public sealed class EnvironmentFileLoadResult
    {
        /// <summary>Absolute path of the file that was loaded, or null when none was.</summary>
        public string? LoadedPath { get; init; }

        public bool Loaded => LoadedPath != null;

        /// <summary>Repository root that was detected, or null when running outside a checkout.</summary>
        public string? RepositoryRoot { get; init; }

        /// <summary>
        /// Paths of stray <c>.env</c> files that exist but were deliberately NOT loaded. Their
        /// presence is worth a warning; their contents are never read.
        /// </summary>
        public IReadOnlyList<string> IgnoredPaths { get; init; } = Array.Empty<string>();

        /// <summary>Number of variables set. Names and values are never exposed here.</summary>
        public int VariablesSet { get; init; }
    }

    /// <summary>
    /// Loads the ONE environment file this repository recognises: <c>&lt;repository-root&gt;/.env</c>.
    /// <para>
    /// Both the API host and the EF design-time factory call this, so <c>dotnet run</c>,
    /// <c>dotnet ef migrations list</c>, <c>dotnet ef database update</c> and design-time context
    /// creation all read the exact same file regardless of the working directory they are invoked
    /// from. Previously each had its own probing logic, which let EF tooling (running with the
    /// startup-project folder as its working directory) silently pick up
    /// <c>src/SporticoApp.Api/.env</c> and target a different database than <c>dotnet run</c>.
    /// </para>
    /// This type never logs, returns or otherwise exposes a variable's value.
    /// </summary>
    public static class EnvironmentFileLoader
    {
        public const string EnvFileName = ".env";

        /// <summary>Legacy location that must never be loaded again — kept only to warn about it.</summary>
        private static readonly string[] DeprecatedRelativePaths =
        {
            Path.Combine("src", "SporticoApp.Api", ".env")
        };

        private static readonly object Gate = new();
        private static EnvironmentFileLoadResult? _result;

        /// <summary>
        /// Loads the repository-root <c>.env</c> once per process and returns what happened.
        /// Missing file is not an error: deployed environments (Azure App Service) supply real
        /// environment variables instead.
        /// </summary>
        public static EnvironmentFileLoadResult Load()
        {
            if (_result != null)
            {
                return _result;
            }

            lock (Gate)
            {
                return _result ??= LoadCore();
            }
        }

        private static EnvironmentFileLoadResult LoadCore()
        {
            var root = RepositoryRootResolver.TryResolve();
            if (root == null)
            {
                return new EnvironmentFileLoadResult();
            }

            var ignored = new List<string>();
            foreach (var relative in DeprecatedRelativePaths)
            {
                var strayPath = Path.Combine(root, relative);
                if (File.Exists(strayPath))
                {
                    ignored.Add(strayPath);
                }
            }

            var envPath = Path.Combine(root, EnvFileName);
            if (!File.Exists(envPath))
            {
                return new EnvironmentFileLoadResult
                {
                    RepositoryRoot = root,
                    IgnoredPaths = ignored
                };
            }

            var count = ApplyEnvFile(envPath);

            return new EnvironmentFileLoadResult
            {
                LoadedPath = envPath,
                RepositoryRoot = root,
                IgnoredPaths = ignored,
                VariablesSet = count
            };
        }

        /// <summary>
        /// Minimal, dependency-free .env parser. Deliberately conservative:
        /// <list type="bullet">
        ///   <item>splits on the FIRST '=' only, because a connection string value legitimately
        ///         contains further '=' characters;</item>
        ///   <item>treats '#' as a comment only at the very start of a line, so a '#' inside a
        ///         password is preserved;</item>
        ///   <item>strips one matching pair of surrounding quotes;</item>
        ///   <item>overwrites variables already present in the process environment, matching the
        ///         previous DotNetEnv behaviour this replaces.</item>
        /// </list>
        /// </summary>
        private static int ApplyEnvFile(string path)
        {
            var count = 0;

            foreach (var rawLine in File.ReadLines(path))
            {
                var line = rawLine.Trim();

                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                if (line.StartsWith("export ", StringComparison.Ordinal))
                {
                    line = line["export ".Length..].TrimStart();
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line[..separator].Trim();
                if (key.Length == 0)
                {
                    continue;
                }

                var value = line[(separator + 1)..].Trim();
                value = StripSurroundingQuotes(value);

                Environment.SetEnvironmentVariable(key, value);
                count++;
            }

            return count;
        }

        private static string StripSurroundingQuotes(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                return value[1..^1];
            }

            return value;
        }
    }
}
