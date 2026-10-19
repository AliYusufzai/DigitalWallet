using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Beneficiaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    WalletNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficiaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beneficiaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_UserId_WalletNumber",
                table: "Beneficiaries",
                columns: new[] { "UserId", "WalletNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Beneficiaries");
        }
    }
}
