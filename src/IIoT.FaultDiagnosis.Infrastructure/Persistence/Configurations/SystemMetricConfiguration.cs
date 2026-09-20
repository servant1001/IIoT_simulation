using IIoT.FaultDiagnosis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence.Configurations;

internal sealed class SystemMetricConfiguration : IEntityTypeConfiguration<SystemMetric>
{
    public void Configure(EntityTypeBuilder<SystemMetric> builder)
    {
        builder.ToTable("system_metrics");
        builder.HasKey(metric => metric.Id);
        builder.Property(metric => metric.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(metric => metric.ExperimentRunId).HasColumnName("experiment_run_id");
        builder.Property(metric => metric.Timestamp).HasColumnName("timestamp").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(metric => metric.CpuPercent).HasColumnName("cpu_percent").HasPrecision(5, 2).IsRequired();
        builder.Property(metric => metric.MemoryMb).HasColumnName("memory_mb").HasPrecision(12, 2).IsRequired();
        builder.Property(metric => metric.ThreadCount).HasColumnName("thread_count");
        builder.Property(metric => metric.GcHeapMb).HasColumnName("gc_heap_mb").HasPrecision(12, 2);
        builder.Property(metric => metric.HandleCount).HasColumnName("handle_count");
        builder.HasIndex(metric => new { metric.ExperimentRunId, metric.Timestamp }).HasDatabaseName("ix_system_metrics_run_timestamp");
        builder.HasOne(metric => metric.ExperimentRun)
            .WithMany(run => run.SystemMetrics)
            .HasForeignKey(metric => metric.ExperimentRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
