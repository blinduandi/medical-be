using Quartz;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;

namespace medical_be.Jobs;

/// <summary>
/// Background job that automatically marks past appointments as completed
/// Runs every 5 minutes to update appointment statuses
/// </summary>
public class AppointmentStatusJob : IJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppointmentStatusJob> _logger;

    public AppointmentStatusJob(ApplicationDbContext context, ILogger<AppointmentStatusJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var now = DateTime.UtcNow;
            
            // Find appointments that have passed their end time but are not marked as completed
            // Fetch to memory first to avoid DateTime.Add translation issues
            var pastAppointments = await _context.Appointments
                .Where(a => 
                    (a.Status == AppointmentStatus.Scheduled || 
                     a.Status == AppointmentStatus.Confirmed || 
                     a.Status == AppointmentStatus.InProgress) &&
                    a.AppointmentDate < now)
                .ToListAsync();

            // Filter in memory to check if appointment end time has passed
            var appointmentsToComplete = pastAppointments
                .Where(a => a.AppointmentDate.Add(a.Duration) < now)
                .ToList();

            if (appointmentsToComplete.Any())
            {
                foreach (var appointment in appointmentsToComplete)
                {
                    appointment.Status = AppointmentStatus.Completed;
                    appointment.UpdatedAt = now;
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "Marked {Count} past appointments as completed at {Time}", 
                    appointmentsToComplete.Count, 
                    now);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating past appointment statuses");
        }
    }
}
