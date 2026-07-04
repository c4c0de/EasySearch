using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InstagramAccountId",
                table: "InventoryListings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WhatsAppAccountId",
                table: "InventoryListings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SocialAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialAccounts_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "SocialAccounts",
                columns: new[] { "Id", "DealerId", "IsDefault", "Label", "Type", "Value" },
                values: new object[] { new Guid("66666666-6666-6666-6666-666666666666"), new Guid("11111111-1111-1111-1111-111111111111"), true, "Main WhatsApp", "WhatsApp", "916238744855" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryListings_InstagramAccountId",
                table: "InventoryListings",
                column: "InstagramAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryListings_WhatsAppAccountId",
                table: "InventoryListings",
                column: "WhatsAppAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialAccounts_DealerId",
                table: "SocialAccounts",
                column: "DealerId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryListings_SocialAccounts_InstagramAccountId",
                table: "InventoryListings",
                column: "InstagramAccountId",
                principalTable: "SocialAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryListings_SocialAccounts_WhatsAppAccountId",
                table: "InventoryListings",
                column: "WhatsAppAccountId",
                principalTable: "SocialAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryListings_SocialAccounts_InstagramAccountId",
                table: "InventoryListings");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryListings_SocialAccounts_WhatsAppAccountId",
                table: "InventoryListings");

            migrationBuilder.DropTable(
                name: "SocialAccounts");

            migrationBuilder.DropIndex(
                name: "IX_InventoryListings_InstagramAccountId",
                table: "InventoryListings");

            migrationBuilder.DropIndex(
                name: "IX_InventoryListings_WhatsAppAccountId",
                table: "InventoryListings");

            migrationBuilder.DropColumn(
                name: "InstagramAccountId",
                table: "InventoryListings");

            migrationBuilder.DropColumn(
                name: "WhatsAppAccountId",
                table: "InventoryListings");
        }
    }
}
