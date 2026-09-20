using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence;

public sealed class FaultDiagnosisDbContextFactory : IDesignTimeDbContextFactory<FaultDiagnosisDbContext>
{
    public FaultDiagnosisDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=127.0.0.1;Port=5432;Database=iiot_fault_lab;Username=iiot";

        var optionsBuilder = new DbContextOptionsBuilder<FaultDiagnosisDbContext>();
        optionsBuilder.UseNpgsql(connectionString, options => options.MigrationsAssembly(typeof(FaultDiagnosisDbContext).Assembly.FullName));
        return new FaultDiagnosisDbContext(optionsBuilder.Options);
    }
}
