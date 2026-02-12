using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyIdentifierTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Merge old types Barcode(2) and Image(3) into Screenshot(1)
            migrationBuilder.Sql(
                """UPDATE "CardIdentifiers" SET "Type" = 1 WHERE "Type" IN (2, 3)""");

            // Make Value nullable (was required)
            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "CardIdentifiers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Set empty string for nulls before making column required again
            migrationBuilder.Sql(
                """UPDATE "CardIdentifiers" SET "Value" = '' WHERE "Value" IS NULL""");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "CardIdentifiers",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
