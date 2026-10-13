using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Signunder.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Marking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "fingerprint",
                table: "asset_record",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fingerprint_quality",
                table: "asset_record",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "wm_payload",
                table: "asset_record",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_record_wm_payload",
                table: "asset_record",
                column: "wm_payload",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asset_record_wm_payload",
                table: "asset_record");

            migrationBuilder.DropColumn(
                name: "fingerprint",
                table: "asset_record");

            migrationBuilder.DropColumn(
                name: "fingerprint_quality",
                table: "asset_record");

            migrationBuilder.DropColumn(
                name: "wm_payload",
                table: "asset_record");
        }
    }
}
