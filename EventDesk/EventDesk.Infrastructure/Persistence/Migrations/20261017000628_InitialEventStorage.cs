using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventDesk.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialEventStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    StartsAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    EndsAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeZoneId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReminderRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeadTimeTicks = table.Column<long>(type: "INTEGER", nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RepeatIntervalTicks = table.Column<long>(type: "INTEGER", nullable: true),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReminderRules_CalendarEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "CalendarEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_IsDeleted_EndsAtUtcTicks",
                table: "CalendarEvents",
                columns: new[] { "IsDeleted", "EndsAtUtcTicks" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_IsDeleted_StartsAtUtcTicks",
                table: "CalendarEvents",
                columns: new[] { "IsDeleted", "StartsAtUtcTicks" });

            migrationBuilder.CreateIndex(
                name: "IX_ReminderRules_EventId",
                table: "ReminderRules",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReminderRules");

            migrationBuilder.DropTable(
                name: "CalendarEvents");
        }
    }
}
