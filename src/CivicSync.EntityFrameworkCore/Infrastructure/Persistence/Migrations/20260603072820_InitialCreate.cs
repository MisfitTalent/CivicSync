using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicSync.Node.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChangeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAtNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CitizenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CitizenReplicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CitizenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastLedgerSequenceApplied = table.Column<long>(type: "bigint", nullable: false),
                    SyncStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenReplicas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Citizens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NationalIdNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citizens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginatingNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PayloadProofHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PreviousProofHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CurrentProofHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NodeSyncReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SyncOutboxEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeSyncReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncInboxEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedFromNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncInboxEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncOutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncOutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovingNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentApprovals_ChangeRequests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "ChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FieldChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldChanges_ChangeRequests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "ChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnownPeerNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeerDepartmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PeerBaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LastSyncedSequence = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownPeerNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnownPeerNodes_DepartmentNodes_DepartmentNodeId",
                        column: x => x.DepartmentNodeId,
                        principalTable: "DepartmentNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CitizenReplicas_DepartmentNodeId_CitizenId",
                table: "CitizenReplicas",
                columns: new[] { "DepartmentNodeId", "CitizenId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citizens_DepartmentNodeId_NationalIdNumber",
                table: "Citizens",
                columns: new[] { "DepartmentNodeId", "NationalIdNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentApprovals_ChangeRequestId_ApprovingNodeId",
                table: "DepartmentApprovals",
                columns: new[] { "ChangeRequestId", "ApprovingNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentNodes_DepartmentCode",
                table: "DepartmentNodes",
                column: "DepartmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FieldChanges_ChangeRequestId",
                table: "FieldChanges",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_KnownPeerNodes_DepartmentNodeId_PeerDepartmentCode",
                table: "KnownPeerNodes",
                columns: new[] { "DepartmentNodeId", "PeerDepartmentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_OriginatingNodeId_SequenceNumber",
                table: "LedgerEntries",
                columns: new[] { "OriginatingNodeId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeSyncReceipts_SyncOutboxEventId_TargetNodeId",
                table: "NodeSyncReceipts",
                columns: new[] { "SyncOutboxEventId", "TargetNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncInboxEntries_DepartmentNodeId_LedgerEntryId",
                table: "SyncInboxEntries",
                columns: new[] { "DepartmentNodeId", "LedgerEntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncOutboxEvents_DepartmentNodeId_LedgerEntryId",
                table: "SyncOutboxEvents",
                columns: new[] { "DepartmentNodeId", "LedgerEntryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CitizenReplicas");

            migrationBuilder.DropTable(
                name: "Citizens");

            migrationBuilder.DropTable(
                name: "DepartmentApprovals");

            migrationBuilder.DropTable(
                name: "FieldChanges");

            migrationBuilder.DropTable(
                name: "KnownPeerNodes");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "NodeSyncReceipts");

            migrationBuilder.DropTable(
                name: "SyncInboxEntries");

            migrationBuilder.DropTable(
                name: "SyncOutboxEvents");

            migrationBuilder.DropTable(
                name: "ChangeRequests");

            migrationBuilder.DropTable(
                name: "DepartmentNodes");
        }
    }
}
