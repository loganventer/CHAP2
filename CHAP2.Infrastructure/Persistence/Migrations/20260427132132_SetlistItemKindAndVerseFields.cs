using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CHAP2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetlistItemKindAndVerseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ChorusId",
                table: "SetlistItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "BookId",
                table: "SetlistItems",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookName",
                table: "SetlistItems",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Chapter",
                table: "SetlistItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "SetlistItems",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Verse",
                table: "SetlistItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerseRef",
                table: "SetlistItems",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerseText",
                table: "SetlistItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookId",
                table: "SetlistItems");

            migrationBuilder.DropColumn(
                name: "BookName",
                table: "SetlistItems");

            migrationBuilder.DropColumn(
                name: "Chapter",
                table: "SetlistItems");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "SetlistItems");

            migrationBuilder.DropColumn(
                name: "Verse",
                table: "SetlistItems");

            migrationBuilder.DropColumn(
                name: "VerseRef",
                table: "SetlistItems");

            migrationBuilder.DropColumn(
                name: "VerseText",
                table: "SetlistItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "ChorusId",
                table: "SetlistItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
