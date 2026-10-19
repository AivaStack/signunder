using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Signunder.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "published_by",
                schema: "registry",
                table: "asset_record",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tenant_identity",
                schema: "registry",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    domain_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_name = table.Column<string>(type: "text", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cert_chain_pem = table.Column<string>(type: "text", nullable: true),
                    key_ref = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_identity", x => x.tenant_id);
                    table.ForeignKey(
                        name: "fk_tenant_identity_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "registry",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_identity",
                schema: "registry");

            migrationBuilder.DropColumn(
                name: "published_by",
                schema: "registry",
                table: "asset_record");
        }
    }
}
