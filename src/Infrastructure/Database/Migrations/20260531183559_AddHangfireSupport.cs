using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddHangfireSupport : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_exchange_rates_exchange_rate_table_exchange_rate_table_id",
            schema: "public",
            table: "exchange_rates");

        migrationBuilder.AddForeignKey(
            name: "fk_exchange_rates_exchange_rate_tables_exchange_rate_table_id",
            schema: "public",
            table: "exchange_rates",
            column: "exchange_rate_table_id",
            principalSchema: "public",
            principalTable: "exchange_rate_tables",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_exchange_rates_exchange_rate_tables_exchange_rate_table_id",
            schema: "public",
            table: "exchange_rates");

        migrationBuilder.AddForeignKey(
            name: "fk_exchange_rates_exchange_rate_table_exchange_rate_table_id",
            schema: "public",
            table: "exchange_rates",
            column: "exchange_rate_table_id",
            principalSchema: "public",
            principalTable: "exchange_rate_tables",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }
}
