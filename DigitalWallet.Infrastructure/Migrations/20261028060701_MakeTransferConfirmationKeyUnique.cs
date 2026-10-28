using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeTransferConfirmationKeyUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransferConfirmation_UserId_IdempotencyKey",
                table: "TransferConfirmation");

            migrationBuilder.CreateIndex(
                name: "IX_TransferConfirmation_UserId_IdempotencyKey",
                table: "TransferConfirmation",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransferConfirmation_UserId_IdempotencyKey",
                table: "TransferConfirmation");

            migrationBuilder.CreateIndex(
                name: "IX_TransferConfirmation_UserId_IdempotencyKey",
                table: "TransferConfirmation",
                columns: new[] { "UserId", "IdempotencyKey" });
        }
    }
}
