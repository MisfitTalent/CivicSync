using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenRecordVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RecordVersion",
                table: "Citizens",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<long>(
                name: "CommittedCitizenVersion",
                table: "ChangeRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExpectedCitizenVersion",
                table: "ChangeRequests",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordVersion",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "CommittedCitizenVersion",
                table: "ChangeRequests");

            migrationBuilder.DropColumn(
                name: "ExpectedCitizenVersion",
                table: "ChangeRequests");
        }
    }
}
