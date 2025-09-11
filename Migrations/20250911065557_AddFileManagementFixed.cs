using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace medical_be.Migrations
{
    /// <inheritdoc />
    public partial class AddFileManagementFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AllowedExtensions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicalFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModelType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModelId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DeletedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsTemporary = table.Column<bool>(type: "bit", nullable: false),
                    BlurHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalFiles_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalFiles_AspNetUsers_DeletedById",
                        column: x => x.DeletedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalFiles_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalFiles_FileTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "FileTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileTypes_Category",
                table: "FileTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_FileTypes_IsActive",
                table: "FileTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalFiles_CreatedAt",
                table: "MedicalFiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalFiles_CreatedById",
                table: "MedicalFiles",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalFiles_DeletedAt",
                table: "MedicalFiles",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalFiles_DeletedById",
                table: "MedicalFiles",
                column: "DeletedById");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalFiles_IsTemporary",
                table: "MedicalFiles",
                column: "IsTemporary");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalFiles_ModelType_ModelId",
                table: "MedicalFiles",
                columns: new[] { "ModelType", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalFiles_TypeId",
                table: "MedicalFiles",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalFiles_UpdatedById",
                table: "MedicalFiles",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalFiles");

            migrationBuilder.DropTable(
                name: "FileTypes");
        }
    }
}
