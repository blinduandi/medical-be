using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;

namespace medical_be.Extensions;

public static class DatabaseExtensions
{
    public static async Task<IApplicationBuilder> SeedDataAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<Role>>();

            await SeedRolesAsync(roleManager);
            await SeedPermissionsAsync(context);
            await SeedDefaultUsersAsync(userManager);
            await AssignRolePermissionsAsync(context, roleManager);
            await SeedFileTypesAsync(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database");
        }

        return app;
    }

    private static async Task SeedRolesAsync(RoleManager<Role> roleManager)
    {
        var roles = new[]
        {
            new Role { Name = "Admin", Description = "System Administrator with full access" },
            new Role { Name = "Doctor", Description = "Medical professional with patient management access" },
            new Role { Name = "Nurse", Description = "Healthcare professional with limited patient access" },
            new Role { Name = "Patient", Description = "Patient with access to own medical records" },
            new Role { Name = "Receptionist", Description = "Front desk staff with appointment management access" }
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role.Name!))
            {
                await roleManager.CreateAsync(role);
            }
        }
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (context.Permissions.Any()) return;

        var permissions = new List<Permission>
        {
            // User Management
            new() { Name = "CreateUser", Description = "Create new users", Module = "Users", Action = "Create" },
            new() { Name = "ReadUser", Description = "View user information", Module = "Users", Action = "Read" },
            new() { Name = "UpdateUser", Description = "Update user information", Module = "Users", Action = "Update" },
            new() { Name = "DeleteUser", Description = "Delete users", Module = "Users", Action = "Delete" },

            // Appointment Management
            new() { Name = "CreateAppointment", Description = "Schedule appointments", Module = "Appointments", Action = "Create" },
            new() { Name = "ReadAppointment", Description = "View appointments", Module = "Appointments", Action = "Read" },
            new() { Name = "UpdateAppointment", Description = "Modify appointments", Module = "Appointments", Action = "Update" },
            new() { Name = "DeleteAppointment", Description = "Cancel appointments", Module = "Appointments", Action = "Delete" },

            // Medical Records
            new() { Name = "CreateMedicalRecord", Description = "Create medical records", Module = "MedicalRecords", Action = "Create" },
            new() { Name = "ReadMedicalRecord", Description = "View medical records", Module = "MedicalRecords", Action = "Read" },
            new() { Name = "UpdateMedicalRecord", Description = "Update medical records", Module = "MedicalRecords", Action = "Update" },
            new() { Name = "DeleteMedicalRecord", Description = "Delete medical records", Module = "MedicalRecords", Action = "Delete" },

            // System Administration
            new() { Name = "ManageRoles", Description = "Manage user roles", Module = "System", Action = "ManageRoles" },
            new() { Name = "ManagePermissions", Description = "Manage permissions", Module = "System", Action = "ManagePermissions" },
            new() { Name = "ViewReports", Description = "View system reports", Module = "System", Action = "ViewReports" }
        };

        context.Permissions.AddRange(permissions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDefaultUsersAsync(UserManager<User> userManager)
    {
        // Create admin user
        if (await userManager.FindByEmailAsync("admin@medical.com") == null)
        {
            var admin = new User
            {
                UserName = "admin@medical.com",
                Email = "admin@medical.com",
                FirstName = "System",
                LastName = "Administrator",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = Gender.Other,
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(admin, "Admin123!");
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Create sample doctor
        if (await userManager.FindByEmailAsync("doctor@medical.com") == null)
        {
            var doctor = new User
            {
                UserName = "doctor@medical.com",
                Email = "doctor@medical.com",
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(1980, 5, 15),
                Gender = Gender.Male,
                EmailConfirmed = true,
                IsActive = true
            };

            await userManager.CreateAsync(doctor, "Doctor123!");
            await userManager.AddToRoleAsync(doctor, "Doctor");
        }
    }

    private static async Task AssignRolePermissionsAsync(ApplicationDbContext context, RoleManager<Role> roleManager)
    {
        if (context.RolePermissions.Any()) return;

        var permissions = await context.Permissions.ToListAsync();
        var adminRole = await roleManager.FindByNameAsync("Admin");
        var doctorRole = await roleManager.FindByNameAsync("Doctor");
        var nurseRole = await roleManager.FindByNameAsync("Nurse");
        var patientRole = await roleManager.FindByNameAsync("Patient");
        var receptionistRole = await roleManager.FindByNameAsync("Receptionist");

        var rolePermissions = new List<RolePermission>();

        // Admin gets all permissions
        if (adminRole != null)
        {
            rolePermissions.AddRange(permissions.Select(p => new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = p.Id
            }));
        }

        // Doctor permissions
        if (doctorRole != null)
        {
            var doctorPermissionNames = new[]
            {
                "ReadUser", "UpdateUser", 
                "CreateAppointment", "ReadAppointment", "UpdateAppointment",
                "CreateMedicalRecord", "ReadMedicalRecord", "UpdateMedicalRecord"
            };

            rolePermissions.AddRange(
                permissions.Where(p => doctorPermissionNames.Contains(p.Name))
                          .Select(p => new RolePermission
                          {
                              RoleId = doctorRole.Id,
                              PermissionId = p.Id
                          }));
        }

        // Patient permissions
        if (patientRole != null)
        {
            var patientPermissionNames = new[] { "ReadAppointment", "ReadMedicalRecord" };

            rolePermissions.AddRange(
                permissions.Where(p => patientPermissionNames.Contains(p.Name))
                          .Select(p => new RolePermission
                          {
                              RoleId = patientRole.Id,
                              PermissionId = p.Id
                          }));
        }

        context.RolePermissions.AddRange(rolePermissions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedFileTypesAsync(ApplicationDbContext context)
    {
        if (await context.FileTypes.AnyAsync())
            return;

        var fileTypes = new[]
        {
            new FileType
            {
                Name = "Profile Photo",
                Description = "User profile photos",
                Category = "ProfilePhoto",
                AllowedExtensions = "[\"jpg\",\"jpeg\",\"png\",\"webp\"]",
                MaxSizeBytes = 5 * 1024 * 1024 // 5MB
            },
            new FileType
            {
                Name = "X-Ray",
                Description = "X-Ray medical images",
                Category = "XRay",
                AllowedExtensions = "[\"jpg\",\"jpeg\",\"png\",\"pdf\",\"dcm\"]",
                MaxSizeBytes = 50 * 1024 * 1024 // 50MB
            },
            new FileType
            {
                Name = "MRI Scan",
                Description = "MRI medical images",
                Category = "MRI",
                AllowedExtensions = "[\"jpg\",\"jpeg\",\"png\",\"pdf\",\"dcm\"]",
                MaxSizeBytes = 100 * 1024 * 1024 // 100MB
            },
            new FileType
            {
                Name = "CT Scan",
                Description = "CT scan medical images",
                Category = "CTScan",
                AllowedExtensions = "[\"jpg\",\"jpeg\",\"png\",\"pdf\",\"dcm\"]",
                MaxSizeBytes = 100 * 1024 * 1024 // 100MB
            },
            new FileType
            {
                Name = "Ultrasound",
                Description = "Ultrasound medical images",
                Category = "Ultrasound",
                AllowedExtensions = "[\"jpg\",\"jpeg\",\"png\",\"pdf\"]",
                MaxSizeBytes = 20 * 1024 * 1024 // 20MB
            },
            new FileType
            {
                Name = "Lab Result",
                Description = "Laboratory test results",
                Category = "LabResult",
                AllowedExtensions = "[\"pdf\",\"jpg\",\"jpeg\",\"png\"]",
                MaxSizeBytes = 10 * 1024 * 1024 // 10MB
            },
            new FileType
            {
                Name = "Prescription",
                Description = "Medical prescriptions",
                Category = "Prescription",
                AllowedExtensions = "[\"pdf\",\"jpg\",\"jpeg\",\"png\"]",
                MaxSizeBytes = 10 * 1024 * 1024 // 10MB
            },
            new FileType
            {
                Name = "Medical Document",
                Description = "General medical documents",
                Category = "Document",
                AllowedExtensions = "[\"pdf\",\"doc\",\"docx\",\"jpg\",\"jpeg\",\"png\"]",
                MaxSizeBytes = 25 * 1024 * 1024 // 25MB
            },
            new FileType
            {
                Name = "Medical Scan",
                Description = "General medical scans and images",
                Category = "MedicalScan",
                AllowedExtensions = "[\"jpg\",\"jpeg\",\"png\",\"pdf\",\"dcm\"]",
                MaxSizeBytes = 50 * 1024 * 1024 // 50MB
            }
        };

        context.FileTypes.AddRange(fileTypes);
        await context.SaveChangesAsync();
    }
}
