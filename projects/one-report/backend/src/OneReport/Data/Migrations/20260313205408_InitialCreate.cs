using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneReport.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    connection_string = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    data_source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data_source_config = table.Column<string>(type: "jsonb", nullable: true),
                    query_template = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_columns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    data_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    format = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    aggregation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_columns", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_columns_report_definitions_report_definition_id",
                        column: x => x.report_definition_id,
                        principalTable: "report_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_export_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    export_format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    record_count = table.Column<long>(type: "bigint", nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    parameters = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_export_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_export_histories_report_definitions_report_definitio",
                        column: x => x.report_definition_id,
                        principalTable: "report_definitions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_data_sources_is_active",
                table: "data_sources",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_data_sources_name",
                table: "data_sources",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_columns_display_order",
                table: "report_columns",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "ix_report_columns_report_definition_id",
                table: "report_columns",
                column: "report_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_definitions_created_at",
                table: "report_definitions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_report_definitions_is_active",
                table: "report_definitions",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_report_definitions_name",
                table: "report_definitions",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_report_export_histories_created_at",
                table: "report_export_histories",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_report_export_histories_report_definition_id",
                table: "report_export_histories",
                column: "report_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_export_histories_status",
                table: "report_export_histories",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_sources");

            migrationBuilder.DropTable(
                name: "report_columns");

            migrationBuilder.DropTable(
                name: "report_export_histories");

            migrationBuilder.DropTable(
                name: "report_definitions");
        }
    }
}
