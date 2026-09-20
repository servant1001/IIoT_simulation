using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIoT.FaultDiagnosis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    protocol_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "experiments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    protocol_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    actual_fault_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    device_count = table.Column<int>(type: "integer", nullable: false),
                    tag_count = table.Column<int>(type: "integer", nullable: false),
                    polling_interval_ms = table.Column<int>(type: "integer", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    repeat_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "experiment_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experiment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_number = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    success_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failure_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    average_response_time_ms = table.Column<double>(type: "double precision", nullable: true),
                    max_response_time_ms = table.Column<double>(type: "double precision", nullable: true),
                    min_response_time_ms = table.Column<double>(type: "double precision", nullable: true),
                    cpu_average = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    memory_average_mb = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiment_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_experiment_runs_experiments_experiment_id",
                        column: x => x.experiment_id,
                        principalTable: "experiments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "communication_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experiment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experiment_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protocol_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    response_time_ms = table.Column<double>(type: "double precision", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    fault_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    raw_request = table.Column<string>(type: "text", nullable: true),
                    raw_response = table.Column<string>(type: "text", nullable: true),
                    parsed_value = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communication_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_communication_records_devices_device_id",
                        column: x => x.device_id,
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_communication_records_experiment_runs_experiment_run_id",
                        column: x => x.experiment_run_id,
                        principalTable: "experiment_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_communication_records_experiments_experiment_id",
                        column: x => x.experiment_id,
                        principalTable: "experiments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "system_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    experiment_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cpu_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    memory_mb = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    thread_count = table.Column<int>(type: "integer", nullable: true),
                    gc_heap_mb = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    handle_count = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_metrics", x => x.id);
                    table.ForeignKey(
                        name: "FK_system_metrics_experiment_runs_experiment_run_id",
                        column: x => x.experiment_run_id,
                        principalTable: "experiment_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "diagnosis_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    communication_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnosis_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    predicted_fault_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    possible_causes = table.Column<string>(type: "jsonb", nullable: true),
                    suggested_actions = table.Column<string>(type: "jsonb", nullable: true),
                    diagnosis_duration_ms = table.Column<double>(type: "double precision", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_diagnosis_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_diagnosis_results_communication_records_communication_recor~",
                        column: x => x.communication_record_id,
                        principalTable: "communication_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_communication_records_device_started_at",
                table: "communication_records",
                columns: new[] { "device_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "IX_communication_records_experiment_id",
                table: "communication_records",
                column: "experiment_id");

            migrationBuilder.CreateIndex(
                name: "ix_communication_records_run_created_at",
                table: "communication_records",
                columns: new[] { "experiment_run_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_devices_enabled_protocol",
                table: "devices",
                columns: new[] { "is_enabled", "protocol_type" });

            migrationBuilder.CreateIndex(
                name: "ix_diagnosis_results_record_id",
                table: "diagnosis_results",
                column: "communication_record_id");

            migrationBuilder.CreateIndex(
                name: "ux_experiment_runs_experiment_run_number",
                table: "experiment_runs",
                columns: new[] { "experiment_id", "run_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_experiments_created_at",
                table: "experiments",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_system_metrics_run_timestamp",
                table: "system_metrics",
                columns: new[] { "experiment_run_id", "timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diagnosis_results");

            migrationBuilder.DropTable(
                name: "system_metrics");

            migrationBuilder.DropTable(
                name: "communication_records");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "experiment_runs");

            migrationBuilder.DropTable(
                name: "experiments");
        }
    }
}
