using IIoT.FaultDiagnosis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence.Configurations;

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");
        builder.HasKey(device => device.Id);
        builder.Property(device => device.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(device => device.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(device => device.ProtocolType).HasColumnName("protocol_type").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(device => device.Host).HasColumnName("host").HasMaxLength(255).IsRequired();
        builder.Property(device => device.Port).HasColumnName("port").IsRequired();
        builder.Property(device => device.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true).IsRequired();
        builder.Property(device => device.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(device => device.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(device => new { device.IsEnabled, device.ProtocolType }).HasDatabaseName("ix_devices_enabled_protocol");
    }
}
