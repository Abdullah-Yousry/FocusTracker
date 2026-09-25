using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSessions3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalTimeInMinutes",
                table: "Sessions",
                newName: "TotalPausedInMinutes");

            migrationBuilder.RenameColumn(
                name: "MaxPauseMinutes",
                table: "Sessions",
                newName: "MaxAllowedPauseInMinutes");

            migrationBuilder.RenameColumn(
                name: "CreateAt",
                table: "Sessions",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAt",
                table: "Sessions",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FocusTimeInMinutes",
                table: "Sessions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "FocusTimeInMinutes",
                table: "Sessions");

            migrationBuilder.RenameColumn(
                name: "TotalPausedInMinutes",
                table: "Sessions",
                newName: "TotalTimeInMinutes");

            migrationBuilder.RenameColumn(
                name: "MaxAllowedPauseInMinutes",
                table: "Sessions",
                newName: "MaxPauseMinutes");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Sessions",
                newName: "CreateAt");
        }
    }
}
