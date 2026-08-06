using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using SporticoApp.Shared.Configuration;

namespace SporticoApp.Infrastructure.Persistence.Context;

/// <summary>
/// Design-time context used by <c>dotnet ef</c>.
/// <para>
/// It resolves configuration exactly the way the running API does: the repository root is located
/// by its solution file, and the ONE recognised environment file — <c>&lt;repository-root&gt;/.env</c>
/// — is loaded through the shared <see cref="EnvironmentFileLoader"/>.
/// </para>
/// <para>
/// This previously combined <c>Directory.GetCurrentDirectory()</c> with the relative path
/// <c>../SporticoApp.Api/.env</c>. Because <c>dotnet ef</c> runs with the startup project's folder
/// as its working directory, that resolved to <c>src/SporticoApp.Api/.env</c> — a different file
/// than <c>dotnet run</c> from the repository root picked up, so migrations could silently target a
/// different database than the application. Both paths now go through the same resolver.
/// </para>
/// </summary>
public class AppDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var envResult = EnvironmentFileLoader.Load();

        // Non-sensitive diagnostics only: never a key name, never a value.
        Console.WriteLine(envResult.Loaded
            ? "[ef] Environment file loaded from repository root."
            : "[ef] No repository-root .env found; relying on process environment variables.");

        foreach (var _ in envResult.IgnoredPaths)
        {
            Console.WriteLine(
                "[ef] WARNING: a legacy .env exists under src/SporticoApp.Api and was IGNORED.");
        }

        var apiProjectPath = envResult.RepositoryRoot != null
            ? Path.Combine(envResult.RepositoryRoot, "src", "SporticoApp.Api")
            : Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            // Optional: appsettings.json only carries blank placeholders; the real values come from
            // the environment. A missing file must not stop migration tooling from running.
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:Default is not configured. Set ConnectionStrings__Default in " +
                "<repository-root>/.env or in the process environment.");
        }

        // Describes what CONFIGURATION points at. `dotnet ef --connection <value>` bypasses this
        // factory's connection string entirely, so the line below does not reflect that override.
        Console.WriteLine($"[ef] Configured database target: {DescribeTarget(connectionString)}");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Describes the target in a form that is safe to print: the hosting platform and whether it is
    /// a direct or pooled endpoint. Never returns the host, database, username or password.
    /// </summary>
    private static string DescribeTarget(string connectionString)
    {
        var host = string.Empty;
        foreach (var part in connectionString.Split(';'))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && pair[0].Trim().Equals("Host", StringComparison.OrdinalIgnoreCase))
            {
                host = pair[1].Trim();
                break;
            }
        }

        if (host.EndsWith("pooler.supabase.com", StringComparison.OrdinalIgnoreCase))
        {
            return "Supabase (session pooler)";
        }

        if (host.EndsWith("supabase.co", StringComparison.OrdinalIgnoreCase))
        {
            return "Supabase (direct)";
        }

        if (host is "localhost" or "127.0.0.1" or "::1")
        {
            return "local";
        }

        return string.IsNullOrEmpty(host) ? "unknown" : "other (non-Supabase)";
    }
}
