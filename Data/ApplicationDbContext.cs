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
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<MedicalRecord> MedicalRecords { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    
    // New Health Models
    public DbSet<VisitRecord> VisitRecords { get; set; }
    public DbSet<Vaccination> Vaccinations { get; set; }
    public DbSet<Allergy> Allergies { get; set; }
    public DbSet<MedicalDocument> MedicalDocuments { get; set; }
    
    // Notifications
    public DbSet<NotificationCampaign> NotificationCampaigns { get; set; }
    public DbSet<SingleNotification> Notifications { get; set; }
    
    // File Management
    public DbSet<FileType> FileTypes { get; set; }
    public DbSet<MedicalFile> MedicalFiles { get; set; }
    
    // Machine Learning & Analytics
    public DbSet<MedicalPattern> MedicalPatterns { get; set; }
    public DbSet<PatternMatch> PatternMatches { get; set; }
    public DbSet<MedicalAlert> MedicalAlerts { get; set; }
    public DbSet<LabResult> LabResults { get; set; }
    public DbSet<Diagnosis> Diagnoses { get; set; }
    
    // Patient-Doctor Relationships
    public DbSet<PatientDoctor> PatientDoctors { get; set; }
    
    // Patient Access Logs
    public DbSet<PatientAccessLog> PatientAccessLogs { get; set; }

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

        // Configure Rating
        builder.Entity<Rating>(entity =>
        {
            entity.HasKey(r => r.RatingId);

            entity.HasOne(r => r.Doctor)
                .WithMany(u => u.DoctorRatings)
                .HasForeignKey(r => r.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Patient)
                .WithMany(u => u.PatientRatings)
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new { r.DoctorId, r.PatientId }).IsUnique();
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
            entity.Property(u => u.Specialty)
                .HasConversion(
                    v => v.ToString(),
                    v => (medical_be.Models.DoctorSpecialty)Enum.Parse(typeof(medical_be.Models.DoctorSpecialty), v)
                );
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

        // Configure FileType
        builder.Entity<FileType>(entity =>
        {
            entity.HasKey(ft => ft.Id);
            entity.Property(ft => ft.Name).IsRequired().HasMaxLength(100);
            entity.Property(ft => ft.Description).HasMaxLength(500);
            entity.Property(ft => ft.Category).IsRequired().HasMaxLength(100);
            entity.Property(ft => ft.AllowedExtensions).IsRequired();
            
            entity.HasIndex(ft => ft.Category);
            entity.HasIndex(ft => ft.IsActive);
        });

        // Configure MedicalFile
        builder.Entity<MedicalFile>(entity =>
        {
            entity.HasKey(mf => mf.Id);
            entity.Property(mf => mf.Name).IsRequired().HasMaxLength(255);
            entity.Property(mf => mf.Path).IsRequired().HasMaxLength(500);
            entity.Property(mf => mf.Extension).HasMaxLength(10);
            entity.Property(mf => mf.MimeType).HasMaxLength(100);
            entity.Property(mf => mf.ModelType).HasMaxLength(100);
            entity.Property(mf => mf.Password).HasMaxLength(500);
            entity.Property(mf => mf.Label).HasMaxLength(200);
            entity.Property(mf => mf.BlurHash).HasMaxLength(100);
            entity.Property(mf => mf.Metadata).HasMaxLength(1000);

            // Configure relationships
            entity.HasOne(mf => mf.Type)
                .WithMany(ft => ft.Files)
                .HasForeignKey(mf => mf.TypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(mf => mf.CreatedBy)
                .WithMany()
                .HasForeignKey(mf => mf.CreatedById)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(mf => mf.UpdatedBy)
                .WithMany()
                .HasForeignKey(mf => mf.UpdatedById)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(mf => mf.DeletedBy)
                .WithMany()
                .HasForeignKey(mf => mf.DeletedById)
                .OnDelete(DeleteBehavior.NoAction);

            // Indexes for performance
            entity.HasIndex(mf => mf.TypeId);
            entity.HasIndex(mf => new { mf.ModelType, mf.ModelId });
            entity.HasIndex(mf => mf.CreatedAt);
            entity.HasIndex(mf => mf.DeletedAt);
            entity.HasIndex(mf => mf.IsTemporary);
        });

        // Configure MedicalPattern
        builder.Entity<MedicalPattern>(entity =>
        {
            entity.HasKey(mp => mp.Id);
            entity.Property(mp => mp.Name).IsRequired().HasMaxLength(200);
            entity.Property(mp => mp.Description).HasMaxLength(1000);
            entity.Property(mp => mp.TriggerCondition).IsRequired();
            entity.Property(mp => mp.OutcomeCondition).IsRequired();

            entity.HasOne(mp => mp.Creator)
                .WithMany()
                .HasForeignKey(mp => mp.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(mp => mp.Updater)
                .WithMany()
                .HasForeignKey(mp => mp.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(mp => mp.IsActive);
            entity.HasIndex(mp => mp.CreatedAt);
        });

        // Configure PatternMatch
        builder.Entity<PatternMatch>(entity =>
        {
            entity.HasKey(pm => pm.Id);
            entity.Property(pm => pm.MatchingData).IsRequired();

            entity.HasOne(pm => pm.Pattern)
                .WithMany(p => p.Matches)
                .HasForeignKey(pm => pm.PatternId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pm => pm.Patient)
                .WithMany(u => u.PatternMatches)
                .HasForeignKey(pm => pm.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(pm => pm.DetectedAt);
            entity.HasIndex(pm => pm.ConfidenceScore);
            entity.HasIndex(pm => pm.IsNotified);
        });

        // Configure MedicalAlert
        builder.Entity<MedicalAlert>(entity =>
        {
            entity.HasKey(ma => ma.Id);
            entity.Property(ma => ma.AlertType).IsRequired().HasMaxLength(100);
            entity.Property(ma => ma.Severity).IsRequired().HasMaxLength(50);
            entity.Property(ma => ma.Message).IsRequired();

            entity.HasOne(ma => ma.Patient)
                .WithMany(u => u.PatientAlerts)
                .HasForeignKey(ma => ma.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ma => ma.PatternMatch)
                .WithMany(pm => pm.Alerts)
                .HasForeignKey(ma => ma.PatternMatchId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(ma => ma.Reader)
                .WithMany(u => u.ReadAlerts)
                .HasForeignKey(ma => ma.ReadBy)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(ma => ma.CreatedAt);
            entity.HasIndex(ma => ma.Severity);
            entity.HasIndex(ma => ma.AlertType);
            entity.HasIndex(ma => ma.IsRead);
        });

        // Configure LabResult
        builder.Entity<LabResult>(entity =>
        {
            entity.HasKey(lr => lr.Id);
            entity.Property(lr => lr.TestName).IsRequired().HasMaxLength(100);
            entity.Property(lr => lr.TestCode).HasMaxLength(20);
            entity.Property(lr => lr.Unit).IsRequired().HasMaxLength(20);
            entity.Property(lr => lr.Status).IsRequired().HasMaxLength(20);
            entity.Property(lr => lr.LabName).HasMaxLength(200);
            entity.Property(lr => lr.Notes).HasMaxLength(500);

            entity.HasOne(lr => lr.Patient)
                .WithMany(u => u.LabResults)
                .HasForeignKey(lr => lr.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(lr => lr.TestDate);
            entity.HasIndex(lr => lr.TestName);
            entity.HasIndex(lr => lr.Status);
            entity.HasIndex(lr => lr.CreatedAt);
        });

        // Configure Diagnosis
        builder.Entity<Diagnosis>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.DiagnosisCode).IsRequired().HasMaxLength(20);
            entity.Property(d => d.DiagnosisName).IsRequired().HasMaxLength(200);
            entity.Property(d => d.Description).HasMaxLength(500);
            entity.Property(d => d.Severity).HasMaxLength(50);
            entity.Property(d => d.Category).HasMaxLength(100);
            entity.Property(d => d.Notes).HasMaxLength(500);

            entity.HasOne(d => d.Patient)
                .WithMany(u => u.PatientDiagnoses)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Doctor)
                .WithMany(u => u.DoctorDiagnoses)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(d => d.DiagnosedDate);
            entity.HasIndex(d => d.Category);
            entity.HasIndex(d => d.IsActive);
            entity.HasIndex(d => d.CreatedAt);
        });

        // Configure PatientDoctor relationship
        builder.Entity<PatientDoctor>(entity =>
        {
            entity.HasKey(pd => pd.Id);
            entity.Property(pd => pd.AssignedBy).IsRequired().HasMaxLength(50);
            entity.Property(pd => pd.Notes).HasMaxLength(500);

            entity.HasOne(pd => pd.Patient)
                .WithMany()
                .HasForeignKey(pd => pd.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pd => pd.Doctor)
                .WithMany()
                .HasForeignKey(pd => pd.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create composite index for unique patient-doctor relationships
            entity.HasIndex(pd => new { pd.PatientId, pd.DoctorId, pd.IsActive })
                .HasDatabaseName("IX_PatientDoctor_Unique")
                .IsUnique()
                .HasFilter("[IsActive] = 1");

            entity.HasIndex(pd => pd.AssignedDate);
            entity.HasIndex(pd => pd.IsActive);
        });

        // Configure PatientAccessLog
        builder.Entity<PatientAccessLog>(entity =>
        {
            entity.HasKey(pal => pal.Id);
            entity.Property(pal => pal.AccessType).IsRequired().HasMaxLength(200);
            entity.Property(pal => pal.AccessReason).HasMaxLength(500);
            entity.Property(pal => pal.IpAddress).HasMaxLength(50);
            entity.Property(pal => pal.UserAgent).HasMaxLength(500);
            entity.Property(pal => pal.SessionId).HasMaxLength(100);

            entity.HasOne(pal => pal.Patient)
                .WithMany()
                .HasForeignKey(pal => pal.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pal => pal.Doctor)
                .WithMany()
                .HasForeignKey(pal => pal.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            entity.HasIndex(pal => pal.PatientId);
            entity.HasIndex(pal => pal.DoctorId);
            entity.HasIndex(pal => pal.AccessedAt);
            entity.HasIndex(pal => new { pal.PatientId, pal.AccessedAt });
            entity.HasIndex(pal => new { pal.DoctorId, pal.AccessedAt });
        });
    }
}
