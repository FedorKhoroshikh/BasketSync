using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDiscountCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CardIdentifiers_Value",
                table: "CardIdentifiers");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "DiscountCards");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "DiscountCards",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "CardIdentifiers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "DiscountCards");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "CardIdentifiers");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "DiscountCards",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_CardIdentifiers_Value",
                table: "CardIdentifiers",
                column: "Value",
                unique: true);
        }
    }
}
