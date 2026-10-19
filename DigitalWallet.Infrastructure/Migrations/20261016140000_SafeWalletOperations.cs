using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalWallet.Infrastructure.Migrations;

public partial class SafeWalletOperations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion", table: "Wallets", type: "rowversion",
            rowVersion: true, nullable: false);
        migrationBuilder.AddColumn<string>(
            name: "Direction", table: "Transactions", type: "nvarchar(20)",
            maxLength: 20, nullable: false, defaultValue: "Unknown");
        migrationBuilder.AddColumn<string>(
            name: "IdempotencyKey", table: "Transactions", type: "nvarchar(64)",
            maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "RequestHash", table: "Transactions", type: "nvarchar(64)",
            maxLength: 64, nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_Transactions_WalletId_IdempotencyKey", table: "Transactions",
            columns: new[] { "WalletId", "IdempotencyKey" }, unique: true,
            filter: "[IdempotencyKey] IS NOT NULL");

        // Old transfers lack a reliable direction; do not infer it from free text.
        migrationBuilder.Sql("""
            UPDATE Transactions SET Direction = 'Incoming' WHERE Type = 'Deposit';
            UPDATE Transactions SET Direction = 'Outgoing' WHERE Type = 'Withdrawal';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_Transactions_WalletId_IdempotencyKey", "Transactions");
        migrationBuilder.DropColumn("IdempotencyKey", "Transactions");
        migrationBuilder.DropColumn("RequestHash", "Transactions");
        migrationBuilder.DropColumn("Direction", "Transactions");
        migrationBuilder.DropColumn("RowVersion", "Wallets");
    }
}
