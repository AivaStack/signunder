using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Signunder.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class TenantMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_member",
                schema: "registry",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_member", x => new { x.tenant_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_tenant_member_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "registry",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_tenant_member_user_id",
                schema: "registry",
                table: "tenant_member",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_member",
                schema: "registry");
        }
    }
}
