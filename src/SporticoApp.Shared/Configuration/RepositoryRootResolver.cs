namespace SporticoApp.Shared.Configuration
{
    /// <summary>
    /// Locates the repository root deterministically, so runtime startup and EF design-time tooling
    /// can never disagree about which directory (and therefore which <c>.env</c>) they are using.
    /// <para>
    /// The root is the directory containing <see cref="SolutionFileName"/>. Probing starts at the
    /// current working directory and walks upward; if that fails (EF tooling and test hosts can run
    /// with an unrelated working directory) it repeats the walk from
    /// <see cref="AppContext.BaseDirectory"/>.
    /// </para>
    /// </summary>
    public static class RepositoryRootResolver
    {
        /// <summary>The marker that identifies the repository root.</summary>
        public const string SolutionFileName = "SporticoApp.Api.sln";

        /// <summary>
        /// Bounded so a service deployed outside the repository (e.g. Azure App Service, where no
        /// .sln exists) walks a handful of directories and stops, instead of the whole drive.
        /// </summary>
        private const int MaxDepth = 12;

        /// <summary>Returns the repository root, or null when this is not a repository checkout.</summary>
        public static string? TryResolve()
        {
            return SearchUpward(SafeCurrentDirectory())
                ?? SearchUpward(AppContext.BaseDirectory);
        }

        private static string? SearchUpward(string? startDirectory)
        {
            if (string.IsNullOrWhiteSpace(startDirectory))
            {
                return null;
            }

            DirectoryInfo? current;
            try
            {
                current = new DirectoryInfo(startDirectory);
            }
            catch
            {
                return null;
            }

            for (var depth = 0; depth < MaxDepth && current != null; depth++)
            {
                try
                {
                    if (File.Exists(Path.Combine(current.FullName, SolutionFileName)))
                    {
                        return current.FullName;
                    }
                }
                catch
                {
                    // Unreadable directory (permissions, unmapped drive) — keep walking up.
                }

                current = current.Parent;
            }

            return null;
        }

        private static string? SafeCurrentDirectory()
        {
            try
            {
                return Directory.GetCurrentDirectory();
            }
            catch
            {
                return null;
            }
        }
    }
}
