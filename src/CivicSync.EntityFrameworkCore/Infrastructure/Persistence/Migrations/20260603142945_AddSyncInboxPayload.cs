using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicSync.Node.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncInboxPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CitizenNationalIdNumber",
                table: "SyncInboxEntries",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FieldChangesJson",
                table: "SyncInboxEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CitizenNationalIdNumber",
                table: "SyncInboxEntries");

            migrationBuilder.DropColumn(
                name: "FieldChangesJson",
                table: "SyncInboxEntries");
        }
    }
}
