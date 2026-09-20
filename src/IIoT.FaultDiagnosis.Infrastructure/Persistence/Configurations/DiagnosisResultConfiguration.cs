using IIoT.FaultDiagnosis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence.Configurations;

internal sealed class DiagnosisResultConfiguration : IEntityTypeConfiguration<DiagnosisResult>
{
    public void Configure(EntityTypeBuilder<DiagnosisResult> builder)
    {
        builder.ToTable("diagnosis_results");
        builder.HasKey(result => result.Id);
        builder.Property(result => result.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(result => result.CommunicationRecordId).HasColumnName("communication_record_id").IsRequired();
        builder.Property(result => result.DiagnosisMethod).HasColumnName("diagnosis_method").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(result => result.PredictedFaultType).HasColumnName("predicted_fault_type").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(result => result.Confidence).HasColumnName("confidence").HasPrecision(5, 4);
        builder.Property(result => result.PossibleCauses).HasColumnName("possible_causes").HasColumnType("jsonb");
        builder.Property(result => result.SuggestedActions).HasColumnName("suggested_actions").HasColumnType("jsonb");
        builder.Property(result => result.DiagnosisDurationMs).HasColumnName("diagnosis_duration_ms").IsRequired();
        builder.Property(result => result.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(result => result.CommunicationRecordId).HasDatabaseName("ix_diagnosis_results_record_id");
        builder.HasOne(result => result.CommunicationRecord)
            .WithMany(record => record.DiagnosisResults)
            .HasForeignKey(result => result.CommunicationRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
