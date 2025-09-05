namespace medical_be.Shared.Events;

public abstract class BaseEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
}

// User Events
public class UserCreatedEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    
    public UserCreatedEvent()
    {
        EventType = nameof(UserCreatedEvent);
    }
}

public class UserUpdatedEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    
    public UserUpdatedEvent()
    {
        EventType = nameof(UserUpdatedEvent);
    }
}

public class UserDeletedEvent : BaseEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    
    public UserDeletedEvent()
    {
        EventType = nameof(UserDeletedEvent);
    }
}

// Appointment Events
public class AppointmentCreatedEvent : BaseEvent
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string PatientEmail { get; set; } = string.Empty;
    public string DoctorEmail { get; set; } = string.Empty;
    
    public AppointmentCreatedEvent()
    {
        EventType = nameof(AppointmentCreatedEvent);
    }
}

public class AppointmentCancelledEvent : BaseEvent
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string PatientEmail { get; set; } = string.Empty;
    public string DoctorEmail { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    
    public AppointmentCancelledEvent()
    {
        EventType = nameof(AppointmentCancelledEvent);
    }
}

// Medical Record Events
public class MedicalRecordCreatedEvent : BaseEvent
{
    public Guid RecordId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string PatientEmail { get; set; } = string.Empty;
    
    public MedicalRecordCreatedEvent()
    {
        EventType = nameof(MedicalRecordCreatedEvent);
    }
}
