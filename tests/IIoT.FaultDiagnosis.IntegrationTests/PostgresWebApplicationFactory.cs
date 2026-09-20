using IIoT.FaultDiagnosis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IIoT.FaultDiagnosis.IntegrationTests;

public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.EnvironmentKey, Environments.Development);
        var connectionString = TestConnectionString.Load();
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<FaultDiagnosisDbContext>();
            services.RemoveAll<DbContextOptions<FaultDiagnosisDbContext>>();
            services.AddDbContext<FaultDiagnosisDbContext>(options => options.UseNpgsql(connectionString));
        });
    }
}

internal static class TestConnectionString
{
    public static string Load()
    {
        var values = File.ReadLines(FindEnvironmentFile())
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#'))
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);

        return $"Host=127.0.0.1;Port=5432;Database={values["POSTGRES_DB"]};Username={values["POSTGRES_USER"]};Password={values["POSTGRES_PASSWORD"]}";
    }

    private static string FindEnvironmentFile()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("A local .env file is required to run PostgreSQL integration tests.");
    }
}
