using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace medical_be.Migrations
{
    /// <inheritdoc />
    public partial class PendingChangesCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "ModelType",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "NanoId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "FailedNotificationsCount",
                table: "notification_campaigns");

            migrationBuilder.DropColumn(
                name: "HardcodedFilters",
                table: "notification_campaigns");

            migrationBuilder.DropColumn(
                name: "NotificationData",
                table: "notification_campaigns");

            migrationBuilder.DropColumn(
                name: "OpenedNotificationsCount",
                table: "notification_campaigns");

            migrationBuilder.DropColumn(
                name: "PendingNotificationsCount",
                table: "notification_campaigns");

            migrationBuilder.DropColumn(
                name: "SelectedEntitiesCount",
                table: "notification_campaigns");

            migrationBuilder.DropColumn(
                name: "SuccessNotificationsCount",
                table: "notification_campaigns");

            migrationBuilder.DropColumn(
                name: "TotalNotificationsCount",
                table: "notification_campaigns");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ModelId",
                table: "notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelType",
                table: "notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NanoId",
                table: "notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedNotificationsCount",
                table: "notification_campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HardcodedFilters",
                table: "notification_campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationData",
                table: "notification_campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OpenedNotificationsCount",
                table: "notification_campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingNotificationsCount",
                table: "notification_campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SelectedEntitiesCount",
                table: "notification_campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SuccessNotificationsCount",
                table: "notification_campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalNotificationsCount",
                table: "notification_campaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
