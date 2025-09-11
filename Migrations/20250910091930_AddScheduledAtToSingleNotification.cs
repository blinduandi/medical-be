using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace medical_be.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledAtToSingleNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledAt",
                table: "notifications",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledAt",
                table: "notifications");
        }
    }
}
