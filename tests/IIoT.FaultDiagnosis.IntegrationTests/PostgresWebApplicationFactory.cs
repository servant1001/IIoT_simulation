using IIoT.FaultDiagnosis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace IIoT.FaultDiagnosis.IntegrationTests;

public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString = TestConnectionString.Load();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.EnvironmentKey, Environments.Development);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<FaultDiagnosisDbContext>();
            services.RemoveAll<DbContextOptions<FaultDiagnosisDbContext>>();
            services.AddDbContext<FaultDiagnosisDbContext>(options => options.UseNpgsql(_connectionString));
        });
    }

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FaultDiagnosisDbContext>();

        // The integration database is disposable and is never the local experiment database.
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
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

        return $"Host=127.0.0.1;Port=5432;Database={values["POSTGRES_DB"]}_integration_tests;Username={values["POSTGRES_USER"]};Password={values["POSTGRES_PASSWORD"]}";
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
