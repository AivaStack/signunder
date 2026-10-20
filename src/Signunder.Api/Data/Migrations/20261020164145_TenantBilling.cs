using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Signunder.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_billing",
                schema: "registry",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_tier = table.Column<string>(type: "text", nullable: true),
                    requested_interval = table.Column<string>(type: "text", nullable: true),
                    requested_currency = table.Column<string>(type: "text", nullable: true),
                    requested_by = table.Column<string>(type: "text", nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    stripe_customer_id = table.Column<string>(type: "text", nullable: true),
                    stripe_subscription_id = table.Column<string>(type: "text", nullable: true),
                    subscription_status = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_billing", x => x.tenant_id);
                    table.ForeignKey(
                        name: "fk_tenant_billing_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "registry",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_billing_stripe_subscription_id",
                schema: "registry",
                table: "tenant_billing",
                column: "stripe_subscription_id",
                unique: true);

            // Upgrade requests waiting for an invoice, read daily by the private repository's
            // billing-requests workflow through the review_watch role (ADR 0015).
            migrationBuilder.Sql("""
                CREATE VIEW registry.upgrade_requests_pending AS
                SELECT t.slug, t.legal_name, t.tier, b.requested_tier, b.requested_interval,
                       b.requested_currency, b.requested_by, b.requested_at
                FROM registry.tenant t
                JOIN registry.tenant_billing b ON b.tenant_id = t.id
                WHERE b.requested_tier IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS registry.upgrade_requests_pending;");
            migrationBuilder.DropTable(
                name: "tenant_billing",
                schema: "registry");
        }
    }
}
