using IIoT.FaultDiagnosis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence.Configurations;

internal sealed class CommunicationRecordConfiguration : IEntityTypeConfiguration<CommunicationRecord>
{
    public void Configure(EntityTypeBuilder<CommunicationRecord> builder)
    {
        builder.ToTable("communication_records");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(record => record.ExperimentId).HasColumnName("experiment_id");
        builder.Property(record => record.ExperimentRunId).HasColumnName("experiment_run_id");
        builder.Property(record => record.DeviceId).HasColumnName("device_id").IsRequired();
        builder.Property(record => record.ProtocolType).HasColumnName("protocol_type").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(record => record.StartedAt).HasColumnName("started_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(record => record.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamp with time zone");
        builder.Property(record => record.ResponseTimeMs).HasColumnName("response_time_ms");
        builder.Property(record => record.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(record => record.FaultType).HasColumnName("fault_type").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(record => record.ErrorCode).HasColumnName("error_code").HasMaxLength(128);
        builder.Property(record => record.ErrorMessage).HasColumnName("error_message");
        builder.Property(record => record.RetryCount).HasColumnName("retry_count").HasDefaultValue(0).IsRequired();
        builder.Property(record => record.RawRequest).HasColumnName("raw_request");
        builder.Property(record => record.RawResponse).HasColumnName("raw_response");
        builder.Property(record => record.ParsedValue).HasColumnName("parsed_value");
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(record => new { record.ExperimentRunId, record.CreatedAt }).HasDatabaseName("ix_communication_records_run_created_at");
        builder.HasIndex(record => new { record.DeviceId, record.StartedAt }).HasDatabaseName("ix_communication_records_device_started_at");
        builder.HasOne(record => record.Experiment)
            .WithMany(experiment => experiment.CommunicationRecords)
            .HasForeignKey(record => record.ExperimentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(record => record.ExperimentRun)
            .WithMany(run => run.CommunicationRecords)
            .HasForeignKey(record => record.ExperimentRunId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(record => record.Device)
            .WithMany()
            .HasForeignKey(record => record.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
