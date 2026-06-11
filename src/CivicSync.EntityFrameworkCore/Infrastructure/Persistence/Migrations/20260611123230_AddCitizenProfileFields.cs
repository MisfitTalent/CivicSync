using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankingAndAssets",
                table: "Citizens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BiometricReference",
                table: "Citizens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DateOfBirth",
                table: "Citizens",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmploymentHistory",
                table: "Citizens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IncomeAndInvestmentProfile",
                table: "Citizens",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MunicipalServiceStatus",
                table: "Citizens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassportNumber",
                table: "Citizens",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RatesAccount",
                table: "Citizens",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RelationshipStatus",
                table: "Citizens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResidentialAddress",
                table: "Citizens",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Citizens",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankingAndAssets",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "BiometricReference",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "EmploymentHistory",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "IncomeAndInvestmentProfile",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "MunicipalServiceStatus",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "PassportNumber",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "RatesAccount",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "RelationshipStatus",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "ResidentialAddress",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "Citizens");
        }
    }
}
