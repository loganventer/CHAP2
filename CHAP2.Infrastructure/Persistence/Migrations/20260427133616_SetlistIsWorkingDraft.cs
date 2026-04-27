using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CHAP2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetlistIsWorkingDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWorkingDraft",
                table: "Setlists",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Setlists_OwnerId_IsWorkingDraft",
                table: "Setlists",
                columns: new[] { "OwnerId", "IsWorkingDraft" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Setlists_OwnerId_IsWorkingDraft",
                table: "Setlists");

            migrationBuilder.DropColumn(
                name: "IsWorkingDraft",
                table: "Setlists");
        }
    }
}
