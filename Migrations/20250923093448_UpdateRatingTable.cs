using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace medical_be.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRatingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PatientId",
                table: "Ratings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DoctorId",
                table: "Ratings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_DoctorId_PatientId",
                table: "Ratings",
                columns: new[] { "DoctorId", "PatientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_PatientId",
                table: "Ratings",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_AspNetUsers_DoctorId",
                table: "Ratings",
                column: "DoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_AspNetUsers_PatientId",
                table: "Ratings",
                column: "PatientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_AspNetUsers_DoctorId",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_AspNetUsers_PatientId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_DoctorId_PatientId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_PatientId",
                table: "Ratings");

            migrationBuilder.AlterColumn<string>(
                name: "PatientId",
                table: "Ratings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DoctorId",
                table: "Ratings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
