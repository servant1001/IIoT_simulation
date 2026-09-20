using IIoT.FaultDiagnosis.Application.Devices;
using IIoT.FaultDiagnosis.Application.Communication;
using IIoT.FaultDiagnosis.Application.Metrics;
using IIoT.FaultDiagnosis.Application.Experiments;
using IIoT.FaultDiagnosis.Application.Diagnosis;
using IIoT.FaultDiagnosis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.FaultDiagnosis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");

        services.AddDbContext<FaultDiagnosisDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(FaultDiagnosisDbContext).Assembly.FullName)));
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<ICommunicationRecordRepository, CommunicationRecordRepository>();
        services.AddScoped<IMetricRepository, MetricRepository>();
        services.AddScoped<IExperimentRepository, ExperimentRepository>();
        services.AddScoped<IDiagnosisRepository, DiagnosisRepository>();
        return services;
    }
}
