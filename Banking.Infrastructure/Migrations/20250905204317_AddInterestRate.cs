using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Banking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInterestRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InterestRate",
                table: "BankAccounts",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterestRate",
                table: "BankAccounts");
        }
    }
}
