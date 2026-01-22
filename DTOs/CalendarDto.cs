using medical_be.Models;

namespace medical_be.DTOs;

/// <summary>
/// Calendar event representation of an appointment
/// </summary>
public class CalendarEventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public AppointmentStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public string Color { get; set; } = string.Empty;
    public bool AllDay { get; set; } = false;
}

/// <summary>
/// Calendar view response with grouped appointments
/// </summary>
public class CalendarViewDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalAppointments { get; set; }
    public List<CalendarEventDto> Events { get; set; } = new();
    public CalendarSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Summary statistics for the calendar period
/// </summary>
public class CalendarSummaryDto
{
    public int Scheduled { get; set; }
    public int Confirmed { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int NoShow { get; set; }
}
