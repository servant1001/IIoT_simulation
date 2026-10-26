using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IIoT.FaultDiagnosis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10RagDiagnosisTrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rag_diagnosis_traces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnosis_result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_fault_types = table.Column<string>(type: "jsonb", nullable: false),
                    source_ids = table.Column<string>(type: "jsonb", nullable: false),
                    historical_case_ids = table.Column<string>(type: "jsonb", nullable: false),
                    provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rag_diagnosis_traces", x => x.id);
                    table.ForeignKey(
                        name: "FK_rag_diagnosis_traces_diagnosis_results_diagnosis_result_id",
                        column: x => x.diagnosis_result_id,
                        principalTable: "diagnosis_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_rag_diagnosis_traces_result_id",
                table: "rag_diagnosis_traces",
                column: "diagnosis_result_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rag_diagnosis_traces");
        }
    }
}
