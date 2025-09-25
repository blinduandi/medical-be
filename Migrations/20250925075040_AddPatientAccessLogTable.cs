using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace medical_be.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientAccessLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientAccessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccessType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccessReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAccessLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAccessLogs_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAccessLogs_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessLogs_AccessedAt",
                table: "PatientAccessLogs",
                column: "AccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessLogs_DoctorId",
                table: "PatientAccessLogs",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessLogs_DoctorId_AccessedAt",
                table: "PatientAccessLogs",
                columns: new[] { "DoctorId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessLogs_PatientId",
                table: "PatientAccessLogs",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessLogs_PatientId_AccessedAt",
                table: "PatientAccessLogs",
                columns: new[] { "PatientId", "AccessedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientAccessLogs");
        }
    }
}
