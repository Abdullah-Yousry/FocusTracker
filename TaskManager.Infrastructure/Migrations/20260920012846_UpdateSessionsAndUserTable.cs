using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSessionsAndUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "TotalTimeInMin",
                table: "Sessions",
                newName: "TotalTimeInMinutes");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Sessions",
                newName: "StartedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastStatusChangedAt",
                table: "Sessions",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "MaxPauseMinutes",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastStatusChangedAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "MaxPauseMinutes",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "TotalTimeInMinutes",
                table: "Sessions",
                newName: "TotalTimeInMin");

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "Sessions",
                newName: "StartTime");

            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Sessions",
                type: "datetime(6)",
                nullable: true);
        }
    }
}
