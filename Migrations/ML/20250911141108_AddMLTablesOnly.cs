using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace medical_be.Migrations.ML
{
    /// <inheritdoc />
    public partial class AddMLTablesOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "VisitRecords",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Vaccinations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Allergies",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Diagnoses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DiagnosisCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiagnosisName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiagnosedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diagnoses_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Diagnoses_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Diagnoses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LabResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TestCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReferenceMin = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ReferenceMax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LabName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResults_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalPatterns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TriggerCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutcomeCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinimumCases = table.Column<int>(type: "int", nullable: false),
                    ConfidenceThreshold = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalPatterns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalPatterns_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalPatterns_AspNetUsers_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

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
                    MainCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatternMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatternId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    MatchingData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsNotified = table.Column<bool>(type: "bit", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatternMatches_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatternMatches_MedicalPatterns_PatternId",
                        column: x => x.PatternId,
                        principalTable: "MedicalPatterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "waiting_for_sending"),
                    CampaignId = table.Column<long>(type: "bigint", nullable: true),
                    ToEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MainCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "MedicalAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PatternMatchId = table.Column<int>(type: "int", nullable: true),
                    AlertType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecommendedActions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientCount = table.Column<int>(type: "int", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadBy = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalAlerts_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicalAlerts_AspNetUsers_ReadBy",
                        column: x => x.ReadBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalAlerts_PatternMatches_PatternMatchId",
                        column: x => x.PatternMatchId,
                        principalTable: "PatternMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitRecords_UserId",
                table: "VisitRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vaccinations_UserId",
                table: "Vaccinations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_UserId",
                table: "Allergies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_Category",
                table: "Diagnoses",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_CreatedAt",
                table: "Diagnoses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_DiagnosedDate",
                table: "Diagnoses",
                column: "DiagnosedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_DoctorId",
                table: "Diagnoses",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_IsActive",
                table: "Diagnoses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_PatientId",
                table: "Diagnoses",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_UserId",
                table: "Diagnoses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_CreatedAt",
                table: "LabResults",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_PatientId",
                table: "LabResults",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_Status",
                table: "LabResults",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_TestDate",
                table: "LabResults",
                column: "TestDate");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_TestName",
                table: "LabResults",
                column: "TestName");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalAlerts_AlertType",
                table: "MedicalAlerts",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalAlerts_CreatedAt",
                table: "MedicalAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalAlerts_IsRead",
                table: "MedicalAlerts",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalAlerts_PatientId",
                table: "MedicalAlerts",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalAlerts_PatternMatchId",
                table: "MedicalAlerts",
                column: "PatternMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalAlerts_ReadBy",
                table: "MedicalAlerts",
                column: "ReadBy");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalAlerts_Severity",
                table: "MedicalAlerts",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalPatterns_CreatedAt",
                table: "MedicalPatterns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalPatterns_CreatedBy",
                table: "MedicalPatterns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalPatterns_IsActive",
                table: "MedicalPatterns",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalPatterns_UpdatedBy",
                table: "MedicalPatterns",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_CampaignId",
                table: "notifications",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_PatternMatches_ConfidenceScore",
                table: "PatternMatches",
                column: "ConfidenceScore");

            migrationBuilder.CreateIndex(
                name: "IX_PatternMatches_DetectedAt",
                table: "PatternMatches",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PatternMatches_IsNotified",
                table: "PatternMatches",
                column: "IsNotified");

            migrationBuilder.CreateIndex(
                name: "IX_PatternMatches_PatientId",
                table: "PatternMatches",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatternMatches_PatternId",
                table: "PatternMatches",
                column: "PatternId");

            migrationBuilder.AddForeignKey(
                name: "FK_Allergies_AspNetUsers_UserId",
                table: "Allergies",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vaccinations_AspNetUsers_UserId",
                table: "Vaccinations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VisitRecords_AspNetUsers_UserId",
                table: "VisitRecords",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allergies_AspNetUsers_UserId",
                table: "Allergies");

            migrationBuilder.DropForeignKey(
                name: "FK_Vaccinations_AspNetUsers_UserId",
                table: "Vaccinations");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitRecords_AspNetUsers_UserId",
                table: "VisitRecords");

            migrationBuilder.DropTable(
                name: "Diagnoses");

            migrationBuilder.DropTable(
                name: "LabResults");

            migrationBuilder.DropTable(
                name: "MedicalAlerts");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "PatternMatches");

            migrationBuilder.DropTable(
                name: "notification_campaigns");

            migrationBuilder.DropTable(
                name: "MedicalPatterns");

            migrationBuilder.DropIndex(
                name: "IX_VisitRecords_UserId",
                table: "VisitRecords");

            migrationBuilder.DropIndex(
                name: "IX_Vaccinations_UserId",
                table: "Vaccinations");

            migrationBuilder.DropIndex(
                name: "IX_Allergies_UserId",
                table: "Allergies");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "VisitRecords");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Vaccinations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Allergies");
        }
    }
}
