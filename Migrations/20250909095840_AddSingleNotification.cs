using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace medical_be.Migrations
{
    /// <inheritdoc />
    public partial class AddSingleNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_campaigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryStatus = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "paused"),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "email"),
                    NotificationTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotificationBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotificationData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HardcodedFilters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    SelectedEntitiesCount = table.Column<int>(type: "int", nullable: false),
                    TotalNotificationsCount = table.Column<int>(type: "int", nullable: false),
                    PendingNotificationsCount = table.Column<int>(type: "int", nullable: false),
                    FailedNotificationsCount = table.Column<int>(type: "int", nullable: false),
                    SuccessNotificationsCount = table.Column<int>(type: "int", nullable: false),
                    OpenedNotificationsCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelId = table.Column<long>(type: "bigint", nullable: true),
                    ModelType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "waiting_for_sending"),
                    NanoId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampaignId = table.Column<long>(type: "bigint", nullable: true),
                    ToEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MainCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_notification_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "notification_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_CampaignId",
                table: "notifications",
                column: "CampaignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "notification_campaigns");
        }
    }
}
