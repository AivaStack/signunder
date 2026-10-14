using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Signunder.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProxyMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "model_name",
                table: "asset_record",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider",
                table: "asset_record",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "retained_media_key",
                table: "asset_record",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "proxy_result",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    upstream_id = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proxy_result", x => new { x.tenant_id, x.provider, x.upstream_id });
                    table.ForeignKey(
                        name: "fk_proxy_result_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "proxy_result");

            migrationBuilder.DropColumn(
                name: "model_name",
                table: "asset_record");

            migrationBuilder.DropColumn(
                name: "provider",
                table: "asset_record");

            migrationBuilder.DropColumn(
                name: "retained_media_key",
                table: "asset_record");
        }
    }
}
