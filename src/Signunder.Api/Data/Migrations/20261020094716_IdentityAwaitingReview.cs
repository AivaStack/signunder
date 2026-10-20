﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Signunder.Api.Data.Migrations
{
    /// <summary>
    /// The companies whose domain is verified but whose name is not approved, or no longer
    /// matches their name: TenantIdentity.Status "awaiting_review" (ADR 0014). Read daily by
    /// .github/workflows/identity-reviews.yml through a role that may read only this view.
    /// </summary>
    public partial class IdentityAwaitingReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE VIEW registry.identity_awaiting_review AS
                SELECT t.slug, t.legal_name, i.domain, i.domain_verified_at, i.approved_name
                FROM registry.tenant t
                JOIN registry.tenant_identity i ON i.tenant_id = t.id
                WHERE i.domain_verified_at IS NOT NULL
                  AND (i.approved_name IS DISTINCT FROM t.legal_name
                       OR coalesce(i.cert_chain_pem, '') = ''
                       OR coalesce(i.key_ref, '') = '');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS registry.identity_awaiting_review;");
        }
    }
}
