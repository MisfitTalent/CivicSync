using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicSync.EntityFrameworkCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApproverAuditDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApproverDepartmentName",
                table: "DepartmentApprovals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApproverFullName",
                table: "DepartmentApprovals",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApproverRole",
                table: "DepartmentApprovals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ApproverUserId",
                table: "DepartmentApprovals",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApproverDepartmentName",
                table: "DepartmentApprovals");

            migrationBuilder.DropColumn(
                name: "ApproverFullName",
                table: "DepartmentApprovals");

            migrationBuilder.DropColumn(
                name: "ApproverRole",
                table: "DepartmentApprovals");

            migrationBuilder.DropColumn(
                name: "ApproverUserId",
                table: "DepartmentApprovals");
        }
    }
}
