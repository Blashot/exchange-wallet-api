using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddWalletPersistence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_wallet_balances_wallet_wallet_id",
            schema: "public",
            table: "wallet_balances");

        migrationBuilder.AddForeignKey(
            name: "fk_wallet_balances_wallets_wallet_id",
            schema: "public",
            table: "wallet_balances",
            column: "wallet_id",
            principalSchema: "public",
            principalTable: "wallets",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_wallet_balances_wallets_wallet_id",
            schema: "public",
            table: "wallet_balances");

        migrationBuilder.AddForeignKey(
            name: "fk_wallet_balances_wallet_wallet_id",
            schema: "public",
            table: "wallet_balances",
            column: "wallet_id",
            principalSchema: "public",
            principalTable: "wallets",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }
}
