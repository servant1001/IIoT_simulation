using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3AllowStandaloneSystemMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "experiment_run_id",
                table: "system_metrics",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "experiment_run_id",
                table: "system_metrics",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
