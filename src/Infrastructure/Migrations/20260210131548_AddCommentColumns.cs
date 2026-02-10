using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListItems_Items_ItemId",
                table: "ListItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ListItems_ShoppingLists_ListId",
                table: "ListItems");

            migrationBuilder.DropIndex(
                name: "IX_Users_PwdHash",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "ListItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ListItems_Items_ItemId",
                table: "ListItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ListItems_ShoppingLists_ListId",
                table: "ListItems",
                column: "ListId",
                principalTable: "ShoppingLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ListItems_Items_ItemId",
                table: "ListItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ListItems_ShoppingLists_ListId",
                table: "ListItems");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "ListItems");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PwdHash",
                table: "Users",
                column: "PwdHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ListItems_Items_ItemId",
                table: "ListItems",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ListItems_ShoppingLists_ListId",
                table: "ListItems",
                column: "ListId",
                principalTable: "ShoppingLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
