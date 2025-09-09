using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using medical_be.Models;

namespace medical_be.Data;

public class ApplicationDbContext : IdentityDbContext<User, Role, string, 
    Microsoft.AspNetCore.Identity.IdentityUserClaim<string>,
    UserRole,
    Microsoft.AspNetCore.Identity.IdentityUserLogin<string>,
    Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>,
    Microsoft.AspNetCore.Identity.IdentityUserToken<string>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<MedicalRecord> MedicalRecords { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    
    // New Health Models
    public DbSet<VisitRecord> VisitRecords { get; set; }
    public DbSet<Vaccination> Vaccinations { get; set; }
    public DbSet<Allergy> Allergies { get; set; }
    public DbSet<MedicalDocument> MedicalDocuments { get; set; }
    public DbSet<NotificationCampaign> NotificationCampaigns { get; set; }
    public DbSet<SingleNotification> Notifications { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure UserRole
        builder.Entity<UserRole>(userRole =>
        {
            userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

            userRole.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            userRole.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();
        });

        // Configure RolePermission
        builder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(rp => rp.Id);

            entity.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
        });

        // Configure Permission
        builder.Entity<Permission>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => new { p.Module, p.Action }).IsUnique();
        });

        // Configure Appointment
        builder.Entity<Appointment>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.HasOne(a => a.Patient)
                .WithMany(u => u.PatientAppointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Doctor)
                .WithMany(u => u.DoctorAppointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => new { a.DoctorId, a.AppointmentDate });
        });

        // Configure MedicalRecord
        builder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(mr => mr.Id);

            entity.HasOne(mr => mr.Patient)
                .WithMany(u => u.MedicalRecords)
                .HasForeignKey(mr => mr.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(mr => mr.Doctor)
                .WithMany()
                .HasForeignKey(mr => mr.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(mr => mr.Appointment)
                .WithMany()
                .HasForeignKey(mr => mr.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(mr => mr.PatientId);
            entity.HasIndex(mr => mr.RecordDate);
        });

        // Configure User
        builder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // Configure VisitRecord relationships
        builder.Entity<VisitRecord>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.HasOne(v => v.Patient)
                .WithMany(u => u.PatientVisitRecords)
                .HasForeignKey(v => v.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Doctor)
                .WithMany(u => u.DoctorVisitRecords)
                .HasForeignKey(v => v.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Vaccination relationships
        builder.Entity<Vaccination>(entity =>
        {
            entity.HasKey(v => v.Id);

            entity.HasOne(v => v.Patient)
                .WithMany(u => u.PatientVaccinations)
                .HasForeignKey(v => v.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.AdministeredBy)
                .WithMany(u => u.AdministeredVaccinations)
                .HasForeignKey(v => v.AdministeredById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Allergy relationships
        builder.Entity<Allergy>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.HasOne(a => a.Patient)
                .WithMany(u => u.PatientAllergies)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.RecordedBy)
                .WithMany(u => u.RecordedAllergies)
                .HasForeignKey(a => a.RecordedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure AuditLog
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
            entity.Property(a => a.UserEmail).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityType).HasMaxLength(50);
            entity.Property(a => a.Details).HasMaxLength(2000);
            entity.Property(a => a.IpAddress).HasMaxLength(45);
            entity.Property(a => a.UserAgent).HasMaxLength(500);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.CreatedAt);
        });

        // Configure NotificationLog
        builder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.RecipientEmail).IsRequired().HasMaxLength(100);
            entity.Property(n => n.RecipientPhone).HasMaxLength(15);
            entity.Property(n => n.Subject).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Content).IsRequired();
            entity.Property(n => n.ErrorMessage).HasMaxLength(1000);

            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.CreatedAt);
            entity.HasIndex(n => n.Status);
        });

        // Configure MedicalDocument relationships
        builder.Entity<MedicalDocument>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.HasOne(d => d.Patient)
                .WithMany()
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.VisitRecord)
                .WithMany()
                .HasForeignKey(d => d.VisitRecordId)
                .OnDelete(DeleteBehavior.SetNull);
        });


        // Configure NotificationCampaign
        builder.Entity<NotificationCampaign>(entity =>
        {
            entity.ToTable("notification_campaigns");
            entity.Property(e => e.DeliveryStatus).HasDefaultValue("paused");
            entity.Property(e => e.Type).HasDefaultValue("email");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Configure Notification
        builder.Entity<SingleNotification>(entity =>
        {
            entity.ToTable("notifications");
            entity.Property(e => e.Status).HasDefaultValue("waiting_for_sending");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(n => n.Campaign)
                .WithMany(c => c.Notifications)
                .HasForeignKey(n => n.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);
        });

    }
}
