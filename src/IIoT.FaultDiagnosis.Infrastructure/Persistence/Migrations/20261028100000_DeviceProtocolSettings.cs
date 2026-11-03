using IIoT.FaultDiagnosis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FaultDiagnosisDbContext))]
[Migration("20261028100000_DeviceProtocolSettings")]
public partial class DeviceProtocolSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "protocol_settings_json",
            table: "devices",
            type: "text",
            nullable: false,
            defaultValue: "{}");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "protocol_settings_json", table: "devices");
    }
}
