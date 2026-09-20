using IIoT.FaultDiagnosis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence;

public sealed class FaultDiagnosisDbContext(DbContextOptions<FaultDiagnosisDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Experiment> Experiments => Set<Experiment>();
    public DbSet<ExperimentRun> ExperimentRuns => Set<ExperimentRun>();
    public DbSet<CommunicationRecord> CommunicationRecords => Set<CommunicationRecord>();
    public DbSet<SystemMetric> SystemMetrics => Set<SystemMetric>();
    public DbSet<DiagnosisResult> DiagnosisResults => Set<DiagnosisResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FaultDiagnosisDbContext).Assembly);
    }
}
