using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UniqueReferenceNumberPerWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_ReferenceNumber",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ReferenceNumber_WalletId",
                table: "Transactions",
                columns: new[] { "ReferenceNumber", "WalletId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_ReferenceNumber_WalletId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ReferenceNumber",
                table: "Transactions",
                column: "ReferenceNumber",
                unique: true);
        }
    }
}
