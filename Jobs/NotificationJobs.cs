using Quartz;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Services;
using medical_be.Shared.Interfaces;

public class NotificationJob : IJob
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationService _notificationService;

    public NotificationJob(ApplicationDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var pendingNotifs = await _context.Notifications
            .Where(n => n.Status == "waiting_for_sending" 
                    && (n.ScheduledAt == null || n.ScheduledAt <= DateTime.UtcNow))
            .ToListAsync();

        foreach (var notif in pendingNotifs)
        {
            try
            {
                await _notificationService.SendEmailAsync(notif.ToEmail, notif.Title, notif.Body);

                notif.Status = "sent";
                notif.UpdatedAt = DateTime.UtcNow;
            }
            catch
            {
                notif.Status = "failed"; // will retry next run
                notif.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

}
