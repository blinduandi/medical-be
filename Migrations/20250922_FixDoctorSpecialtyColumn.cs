using Microsoft.EntityFrameworkCore.Migrations;

namespace medical_be.Migrations
{
    public partial class FixDoctorSpecialtyColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update invalid or null specialties to valid enum names
            migrationBuilder.Sql("UPDATE AspNetUsers SET Specialty = 'Surgery' WHERE Specialty = 'chirurg';");
            migrationBuilder.Sql("UPDATE AspNetUsers SET Specialty = 'Cardiology' WHERE Specialty = 'cardiolog';");
            migrationBuilder.Sql("UPDATE AspNetUsers SET Specialty = 'GeneralPractice' WHERE Specialty IS NULL OR Specialty = '';");
            // Add more mappings as needed for your DoctorSpecialty enum
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Optionally, revert changes if needed (not required for data cleanup)
        }
    }
}
