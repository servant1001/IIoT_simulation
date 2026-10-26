using IIoT.FaultDiagnosis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence.Configurations;

internal sealed class RagDiagnosisTraceConfiguration : IEntityTypeConfiguration<RagDiagnosisTrace>
{
    public void Configure(EntityTypeBuilder<RagDiagnosisTrace> builder)
    {
        builder.ToTable("rag_diagnosis_traces");
        builder.HasKey(trace => trace.Id);
        builder.Property(trace => trace.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(trace => trace.DiagnosisResultId).HasColumnName("diagnosis_result_id").IsRequired();
        builder.Property(trace => trace.CandidateFaultTypes).HasColumnName("candidate_fault_types").HasColumnType("jsonb").IsRequired();
        builder.Property(trace => trace.SourceIds).HasColumnName("source_ids").HasColumnType("jsonb").IsRequired();
        builder.Property(trace => trace.HistoricalCaseIds).HasColumnName("historical_case_ids").HasColumnType("jsonb").IsRequired();
        builder.Property(trace => trace.Provider).HasColumnName("provider").HasMaxLength(64).IsRequired();
        builder.Property(trace => trace.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(trace => trace.DiagnosisResultId).IsUnique().HasDatabaseName("ux_rag_diagnosis_traces_result_id");
    }
}
