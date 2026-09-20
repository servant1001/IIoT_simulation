using IIoT.FaultDiagnosis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence.Configurations;

internal sealed class ExperimentConfiguration : IEntityTypeConfiguration<Experiment>
{
    public void Configure(EntityTypeBuilder<Experiment> builder)
    {
        builder.ToTable("experiments");
        builder.HasKey(experiment => experiment.Id);
        builder.Property(experiment => experiment.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(experiment => experiment.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(experiment => experiment.ProtocolType).HasColumnName("protocol_type").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(experiment => experiment.ActualFaultType).HasColumnName("actual_fault_type").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(experiment => experiment.DeviceCount).HasColumnName("device_count").IsRequired();
        builder.Property(experiment => experiment.TagCount).HasColumnName("tag_count").IsRequired();
        builder.Property(experiment => experiment.PollingIntervalMs).HasColumnName("polling_interval_ms").IsRequired();
        builder.Property(experiment => experiment.DurationSeconds).HasColumnName("duration_seconds").IsRequired();
        builder.Property(experiment => experiment.RepeatCount).HasColumnName("repeat_count").IsRequired();
        builder.Property(experiment => experiment.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(experiment => experiment.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(experiment => experiment.CreatedAt).HasDatabaseName("ix_experiments_created_at");
    }
}
