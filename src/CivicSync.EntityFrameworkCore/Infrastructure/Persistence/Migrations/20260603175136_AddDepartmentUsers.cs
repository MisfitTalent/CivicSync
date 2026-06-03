using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartmentUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentUsers_DepartmentNodes_DepartmentNodeId",
                        column: x => x.DepartmentNodeId,
                        principalTable: "DepartmentNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentUsers_DepartmentNodeId_EmailAddress",
                table: "DepartmentUsers",
                columns: new[] { "DepartmentNodeId", "EmailAddress" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentUsers");
        }
    }
}
