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
            AddColumnIfMissing(migrationBuilder, "BankingAndAssets", "nvarchar(500)");
            AddColumnIfMissing(migrationBuilder, "BiometricReference", "nvarchar(200)");
            AddColumnIfMissing(migrationBuilder, "DateOfBirth", "nvarchar(60)");
            AddColumnIfMissing(migrationBuilder, "EmploymentHistory", "nvarchar(500)");
            AddColumnIfMissing(migrationBuilder, "IncomeAndInvestmentProfile", "nvarchar(500)");
            AddColumnIfMissing(migrationBuilder, "MunicipalServiceStatus", "nvarchar(100)");
            AddColumnIfMissing(migrationBuilder, "PassportNumber", "nvarchar(30)");
            AddColumnIfMissing(migrationBuilder, "RatesAccount", "nvarchar(50)");
            AddColumnIfMissing(migrationBuilder, "RelationshipStatus", "nvarchar(200)");
            AddColumnIfMissing(migrationBuilder, "ResidentialAddress", "nvarchar(300)");
            AddColumnIfMissing(migrationBuilder, "TaxNumber", "nvarchar(30)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropColumnIfExists(migrationBuilder, "BankingAndAssets");
            DropColumnIfExists(migrationBuilder, "BiometricReference");
            DropColumnIfExists(migrationBuilder, "DateOfBirth");
            DropColumnIfExists(migrationBuilder, "EmploymentHistory");
            DropColumnIfExists(migrationBuilder, "IncomeAndInvestmentProfile");
            DropColumnIfExists(migrationBuilder, "MunicipalServiceStatus");
            DropColumnIfExists(migrationBuilder, "PassportNumber");
            DropColumnIfExists(migrationBuilder, "RatesAccount");
            DropColumnIfExists(migrationBuilder, "RelationshipStatus");
            DropColumnIfExists(migrationBuilder, "ResidentialAddress");
            DropColumnIfExists(migrationBuilder, "TaxNumber");
        }

        private static void AddColumnIfMissing(MigrationBuilder migrationBuilder, string columnName, string sqlType)
        {
            migrationBuilder.Sql($@"
IF COL_LENGTH('Citizens', '{columnName}') IS NULL
BEGIN
    ALTER TABLE [Citizens] ADD [{columnName}] {sqlType} NOT NULL CONSTRAINT [DF_Citizens_{columnName}] DEFAULT N''
END");
        }

        private static void DropColumnIfExists(MigrationBuilder migrationBuilder, string columnName)
        {
            migrationBuilder.Sql($@"
IF COL_LENGTH('Citizens', '{columnName}') IS NOT NULL
BEGIN
    ALTER TABLE [Citizens] DROP COLUMN [{columnName}]
END");
        }
    }
}
