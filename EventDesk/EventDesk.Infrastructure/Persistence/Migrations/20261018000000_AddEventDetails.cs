using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventDesk.Infrastructure.Persistence.Migrations;

public partial class AddEventDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "CalendarEvents",
            type: "TEXT",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ColorHex",
            table: "CalendarEvents",
            type: "TEXT",
            maxLength: 7,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Description", table: "CalendarEvents");
        migrationBuilder.DropColumn(name: "ColorHex", table: "CalendarEvents");
    }
}
