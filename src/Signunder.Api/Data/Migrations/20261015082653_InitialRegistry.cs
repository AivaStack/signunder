using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Signunder.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "registry");

            migrationBuilder.CreateTable(
                name: "tenant",
                schema: "registry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    region = table.Column<string>(type: "text", nullable: false),
                    tier = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_key",
                schema: "registry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prefix = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_key", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_key_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "registry",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proxy_request",
                schema: "registry",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    upstream_id = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proxy_request", x => new { x.tenant_id, x.provider, x.upstream_id });
                    table.ForeignKey(
                        name: "fk_proxy_request_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "registry",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proxy_result",
                schema: "registry",
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
                        principalSchema: "registry",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenant_key",
                schema: "registry",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kms_key_arn = table.Column<string>(type: "text", nullable: false),
                    algorithm = table.Column<string>(type: "text", nullable: false),
                    cert_chain_pem = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_key", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_key_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "registry",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_record",
                schema: "registry",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_key_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modality = table.Column<string>(type: "text", nullable: false),
                    mime = table.Column<string>(type: "text", nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: false),
                    bytes_sha256 = table.Column<string>(type: "text", nullable: false),
                    manifest_hash = table.Column<string>(type: "text", nullable: false),
                    manifest_label = table.Column<string>(type: "text", nullable: true),
                    validation_state = table.Column<string>(type: "text", nullable: false),
                    actions = table.Column<string>(type: "jsonb", nullable: false),
                    layers = table.Column<string>(type: "jsonb", nullable: false),
                    validation_report = table.Column<string>(type: "jsonb", nullable: false),
                    wm_payload = table.Column<long>(type: "bigint", nullable: true),
                    fingerprint = table.Column<byte[]>(type: "bytea", nullable: true),
                    fingerprint_quality = table.Column<int>(type: "integer", nullable: true),
                    provider = table.Column<string>(type: "text", nullable: true),
                    model_name = table.Column<string>(type: "text", nullable: true),
                    retained_media_key = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_asset_record_tenant_key_tenant_key_id",
                        column: x => x.tenant_key_id,
                        principalSchema: "registry",
                        principalTable: "tenant_key",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asset_record_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "registry",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_key_hash",
                schema: "registry",
                table: "api_key",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_key_tenant_id",
                schema: "registry",
                table: "api_key",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_record_manifest_hash",
                schema: "registry",
                table: "asset_record",
                column: "manifest_hash");

            migrationBuilder.CreateIndex(
                name: "ix_asset_record_tenant_id_created_at",
                schema: "registry",
                table: "asset_record",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_record_tenant_key_id",
                schema: "registry",
                table: "asset_record",
                column: "tenant_key_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_record_wm_payload",
                schema: "registry",
                table: "asset_record",
                column: "wm_payload",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_slug",
                schema: "registry",
                table: "tenant",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_key_tenant_id",
                schema: "registry",
                table: "tenant_key",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_key",
                schema: "registry");

            migrationBuilder.DropTable(
                name: "asset_record",
                schema: "registry");

            migrationBuilder.DropTable(
                name: "proxy_request",
                schema: "registry");

            migrationBuilder.DropTable(
                name: "proxy_result",
                schema: "registry");

            migrationBuilder.DropTable(
                name: "tenant_key",
                schema: "registry");

            migrationBuilder.DropTable(
                name: "tenant",
                schema: "registry");
        }
    }
}
