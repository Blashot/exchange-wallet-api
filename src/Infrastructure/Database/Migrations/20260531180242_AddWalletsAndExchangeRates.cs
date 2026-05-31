using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddWalletsAndExchangeRates : Migration
{
    private static readonly string[] columns = new[] { "exchange_rate_table_id", "currency_code" };
    private static readonly string[] columnsArray = new[] { "wallet_id", "currency_code" };
    private static readonly string[] columnsArray0 = new[] { "wallet_id", "occurred_at" };

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "exchange_rate_tables",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                table_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                publication_date = table.Column<DateOnly>(type: "date", nullable: false),
                imported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_exchange_rate_tables", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "wallets",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                version = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_wallets", x => x.id);
                table.ForeignKey(
                    name: "fk_wallets_users_user_id",
                    column: x => x.user_id,
                    principalSchema: "public",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "exchange_rates",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                exchange_rate_table_id = table.Column<Guid>(type: "uuid", nullable: false),
                currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                currency_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                mid_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_exchange_rates", x => x.id);
                table.ForeignKey(
                    name: "fk_exchange_rates_exchange_rate_table_exchange_rate_table_id",
                    column: x => x.exchange_rate_table_id,
                    principalSchema: "public",
                    principalTable: "exchange_rate_tables",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "wallet_balances",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                amount = table.Column<decimal>(type: "numeric(28,8)", precision: 28, scale: 8, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_wallet_balances", x => x.id);
                table.ForeignKey(
                    name: "fk_wallet_balances_wallet_wallet_id",
                    column: x => x.wallet_id,
                    principalSchema: "public",
                    principalTable: "wallets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "wallet_transactions",
            schema: "public",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                amount = table.Column<decimal>(type: "numeric(28,8)", precision: 28, scale: 8, nullable: false),
                occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_wallet_transactions", x => x.id);
                table.ForeignKey(
                    name: "fk_wallet_transactions_wallets_wallet_id",
                    column: x => x.wallet_id,
                    principalSchema: "public",
                    principalTable: "wallets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_exchange_rate_tables_publication_date",
            schema: "public",
            table: "exchange_rate_tables",
            column: "publication_date",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_exchange_rate_tables_table_number",
            schema: "public",
            table: "exchange_rate_tables",
            column: "table_number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_exchange_rates_table_id_currency_code",
            schema: "public",
            table: "exchange_rates",
            columns: columns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_wallet_balances_wallet_id_currency_code",
            schema: "public",
            table: "wallet_balances",
            columns: columnsArray,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_wallet_transactions_wallet_id_occurred_at",
            schema: "public",
            table: "wallet_transactions",
            columns: columnsArray0);

        migrationBuilder.CreateIndex(
            name: "ix_wallets_user_id",
            schema: "public",
            table: "wallets",
            column: "user_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "exchange_rates",
            schema: "public");

        migrationBuilder.DropTable(
            name: "wallet_balances",
            schema: "public");

        migrationBuilder.DropTable(
            name: "wallet_transactions",
            schema: "public");

        migrationBuilder.DropTable(
            name: "exchange_rate_tables",
            schema: "public");

        migrationBuilder.DropTable(
            name: "wallets",
            schema: "public");
    }
}
