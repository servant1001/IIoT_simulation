using IIoT.FaultDiagnosis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence.Configurations;

internal sealed class ExperimentRunConfiguration : IEntityTypeConfiguration<ExperimentRun>
{
    public void Configure(EntityTypeBuilder<ExperimentRun> builder)
    {
        builder.ToTable("experiment_runs");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(run => run.ExperimentId).HasColumnName("experiment_id").IsRequired();
        builder.Property(run => run.RunNumber).HasColumnName("run_number").IsRequired();
        builder.Property(run => run.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone");
        builder.Property(run => run.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
        builder.Property(run => run.SuccessCount).HasColumnName("success_count").HasDefaultValue(0).IsRequired();
        builder.Property(run => run.FailureCount).HasColumnName("failure_count").HasDefaultValue(0).IsRequired();
        builder.Property(run => run.AverageResponseTimeMs).HasColumnName("average_response_time_ms");
        builder.Property(run => run.MaxResponseTimeMs).HasColumnName("max_response_time_ms");
        builder.Property(run => run.MinResponseTimeMs).HasColumnName("min_response_time_ms");
        builder.Property(run => run.CpuAverage).HasColumnName("cpu_average").HasPrecision(5, 2);
        builder.Property(run => run.MemoryAverageMb).HasColumnName("memory_average_mb").HasPrecision(12, 2);
        builder.Property(run => run.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(run => new { run.ExperimentId, run.RunNumber }).IsUnique().HasDatabaseName("ux_experiment_runs_experiment_run_number");
        builder.HasOne(run => run.Experiment)
            .WithMany(experiment => experiment.Runs)
            .HasForeignKey(run => run.ExperimentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
