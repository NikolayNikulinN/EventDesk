using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventDesk.Infrastructure.Persistence.Migrations;

public partial class AddEventRecurrence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "RecurrenceKind", table: "CalendarEvents",
            type: "INTEGER", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(name: "RecurrenceMonth", table: "CalendarEvents",
            type: "INTEGER", nullable: true);
        migrationBuilder.AddColumn<int>(name: "RecurrenceDay", table: "CalendarEvents",
            type: "INTEGER", nullable: true);
        migrationBuilder.AddColumn<int>(name: "NotificationIntervalMinutes", table: "CalendarEvents",
            type: "INTEGER", nullable: true);

        migrationBuilder.CreateIndex(name: "IX_CalendarEvents_IsDeleted_RecurrenceKind",
            table: "CalendarEvents", columns: new[] { "IsDeleted", "RecurrenceKind" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_CalendarEvents_IsDeleted_RecurrenceKind", table: "CalendarEvents");
        migrationBuilder.DropColumn(name: "NotificationIntervalMinutes", table: "CalendarEvents");
        migrationBuilder.DropColumn(name: "RecurrenceDay", table: "CalendarEvents");
        migrationBuilder.DropColumn(name: "RecurrenceMonth", table: "CalendarEvents");
        migrationBuilder.DropColumn(name: "RecurrenceKind", table: "CalendarEvents");
    }
}
