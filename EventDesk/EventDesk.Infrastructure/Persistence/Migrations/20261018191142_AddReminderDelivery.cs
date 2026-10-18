using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventDesk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReminderDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReminderDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    DueAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    NextAttemptAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LeaseToken = table.Column<Guid>(type: "TEXT", nullable: true),
                    LeaseUntilUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    DeliveredAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    LastErrorCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderDeliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReminderScheduleCursor",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ThroughUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderScheduleCursor", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReminderAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeliveryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    FinishedAtUtc = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReminderAttempts_ReminderDeliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "ReminderDeliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderAttempts_DeliveryId_Number",
                table: "ReminderAttempts",
                columns: new[] { "DeliveryId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderDeliveries_EventId_Channel_DueAtUtc",
                table: "ReminderDeliveries",
                columns: new[] { "EventId", "Channel", "DueAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderDeliveries_Status_NextAttemptAtUtc",
                table: "ReminderDeliveries",
                columns: new[] { "Status", "NextAttemptAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReminderAttempts");

            migrationBuilder.DropTable(
                name: "ReminderScheduleCursor");

            migrationBuilder.DropTable(
                name: "ReminderDeliveries");
        }
    }
}
