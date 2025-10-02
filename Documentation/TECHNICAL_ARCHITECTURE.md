# Arhitectura Tehnică - Diagramele Sistemului
## Medical Management API System

---

## 1. ARHITECTURA DE ANSAMBLU

### 1.1 Layered Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        PRESENTATION LAYER                       │
├─────────────────────────────────────────────────────────────────┤
│  Controllers/                                                   │
│  ├── AuthController.cs          ├── PatientController.cs        │
│  ├── DoctorController.cs        ├── AdminController.cs          │
│  ├── AppointmentController.cs   └── DocumentController.cs       │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                      BUSINESS LOGIC LAYER                       │
├─────────────────────────────────────────────────────────────────┤
│  Services/                                                      │
│  ├── AuthService.cs             ├── PatientAccessLogService.cs  │
│  ├── AuditService.cs            ├── NotificationService.cs      │
│  ├── FileService.cs             └── PatternDetectionService.cs  │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                      DATA ACCESS LAYER                          │
├─────────────────────────────────────────────────────────────────┤
│  Data/                                                          │
│  ├── ApplicationDbContext.cs                                   │
│  └── Entity Framework Core Repositories                        │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                       DATABASE LAYER                            │
├─────────────────────────────────────────────────────────────────┤
│                        SQL Server                               │
│  ├── Users & Roles Tables                                      │
│  ├── Medical Data Tables                                       │
│  └── Audit & Logging Tables                                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. DATABASE SCHEMA DESIGN

### 2.1 Core Entities Relationship Diagram

```
┌─────────────────┐         ┌─────────────────┐
│      Users      │         │      Roles      │
├─────────────────┤         ├─────────────────┤
│ + Id (PK)       │    ┌────│ + Id (PK)       │
│ + Email         │    │    │ + Name          │
│ + FirstName     │    │    │ + Description   │
│ + LastName      │    │    └─────────────────┘
│ + IDNP          │    │              │
│ + DateOfBirth   │    │              │
│ + Gender        │    │              │ 1
│ + IsActive      │    │              │
└─────────────────┘    │              │
         │              │              │
         │ 1            │ M            │ M
         │              │              │
         │              │    ┌─────────▼─────────┐
         │              └────│    UserRoles      │
         │                   ├───────────────────┤
         │ M                 │ + UserId (FK)     │
         │                   │ + RoleId (FK)     │
         │                   └───────────────────┘
         │
         ├─────────────────────────────────────────────────┐
         │                                                 │
         ▼ M                                               ▼ M
┌─────────────────┐                               ┌─────────────────┐
│  VisitRecords   │                               │   Appointments  │
├─────────────────┤                               ├─────────────────┤
│ + Id (PK)       │                               │ + Id (PK)       │
│ + PatientId(FK) │                               │ + PatientId(FK) │
│ + DoctorId (FK) │                               │ + DoctorId (FK) │
│ + VisitDate     │                               │ + AppointmentDate│
│ + Diagnosis     │                               │ + Status        │
│ + Treatment     │                               │ + Notes         │
└─────────────────┘                               └─────────────────┘
         │
         │
         ▼ M
┌─────────────────┐
│   Allergies     │
├─────────────────┤
│ + Id (PK)       │
│ + PatientId(FK) │
│ + AllergenName  │
│ + Severity      │
│ + DiagnosedDate │
└─────────────────┘
```

### 2.2 Patient-Doctor Relationship Schema

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│   Users         │         │  PatientDoctor  │         │     Users       │
│   (Patient)     │         │  (Junction)     │         │   (Doctor)      │
├─────────────────┤         ├─────────────────┤         ├─────────────────┤
│ + Id (PK)       │◄───────►│ + Id (PK)       │◄───────►│ + Id (PK)       │
│ + FirstName     │ 1     M │ + PatientId(FK) │ M     1 │ + FirstName     │
│ + LastName      │         │ + DoctorId (FK) │         │ + LastName      │
│ + IDNP          │         │ + AssignedDate  │         │ + Specialty     │
│ + DateOfBirth   │         │ + IsActive      │         │ + Experience    │
│ + BloodType     │         │ + AssignedBy    │         │ + ClinicId      │
└─────────────────┘         │ + Notes         │         └─────────────────┘
                           └─────────────────┘
                                     │
                                     │ 1
                                     │
                                     ▼ M
                           ┌─────────────────┐
                           │PatientAccessLog │
                           ├─────────────────┤
                           │ + Id (PK)       │
                           │ + PatientId(FK) │
                           │ + DoctorId (FK) │
                           │ + AccessedAt    │
                           │ + AccessType    │
                           │ + IpAddress     │
                           │ + UserAgent     │
                           └─────────────────┘
```

---

## 3. DESIGN PATTERNS IMPLEMENTATION

### 3.1 Dependency Injection Pattern Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                      Program.cs (DI Container)                  │
├─────────────────────────────────────────────────────────────────┤
│  builder.Services.AddScoped<IAuthService, AuthService>();      │
│  builder.Services.AddScoped<IAuditService, AuditService>();    │
│  builder.Services.AddScoped<IPatientAccessLogService,          │
│                             PatientAccessLogService>();        │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼ Injects Dependencies
┌─────────────────────────────────────────────────────────────────┐
│                    Controller Constructor                        │
├─────────────────────────────────────────────────────────────────┤
│  public PatientController(                                      │
│      IPatientAccessLogService patientAccessLogService,         │
│      IAuditService auditService)                               │
│  {                                                              │
│      _patientAccessLogService = patientAccessLogService;       │
│      _auditService = auditService;                             │
│  }                                                              │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼ Uses Injected Services
┌─────────────────────────────────────────────────────────────────┐
│                    Controller Actions                           │
├─────────────────────────────────────────────────────────────────┤
│  public async Task<IActionResult> GetPatientDetails(...)       │
│  {                                                              │
│      await _patientAccessLogService.LogPatientAccessAsync(...);│
│      await _auditService.LogAuditAsync(...);                   │
│      return SuccessResponse(data);                             │
│  }                                                              │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Repository Pattern Implementation

```
┌─────────────────────────────────────────────────────────────────┐
│                    Generic Repository                           │
├─────────────────────────────────────────────────────────────────┤
│  ApplicationDbContext : IdentityDbContext<User>                │
│                                                                 │
│  + DbSet<PatientDoctor> PatientDoctors { get; set; }          │
│  + DbSet<PatientAccessLog> PatientAccessLogs { get; set; }    │
│  + DbSet<VisitRecord> VisitRecords { get; set; }              │
│                                                                 │
│  + SaveChangesAsync() : Task<int>                              │
│  + Set<TEntity>() : DbSet<TEntity>                             │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼ Inherited by Services
┌─────────────────────────────────────────────────────────────────┐
│                    Service Layer                                │
├─────────────────────────────────────────────────────────────────┤
│  public class PatientAccessLogService                          │
│  {                                                              │
│      private readonly ApplicationDbContext _context;           │
│                                                                 │
│      public async Task LogPatientAccessAsync(...)              │
│      {                                                          │
│          var accessLog = new PatientAccessLog { ... };        │
│          _context.PatientAccessLogs.Add(accessLog);           │
│          await _context.SaveChangesAsync();                    │
│      }                                                          │
│  }                                                              │
└─────────────────────────────────────────────────────────────────┘
```

---

## 4. SECURITY ARCHITECTURE

### 4.1 Authentication & Authorization Flow

```
┌─────────────────┐    1. Login Request     ┌─────────────────┐
│   Client App    │────────────────────────►│  AuthController │
│                 │                         │                 │
└─────────────────┘                         └─────────────────┘
         ▲                                           │
         │                                           │ 2. Validate Credentials
         │                                           ▼
         │                                  ┌─────────────────┐
         │                                  │   AuthService   │
         │                                  │                 │
         │                                  └─────────────────┘
         │                                           │
         │                                           │ 3. Generate JWT
         │                                           ▼
         │                                  ┌─────────────────┐
         │                                  │   JWT Token     │
         │                                  │                 │
         │  4. Return Token                 └─────────────────┘
         │◄─────────────────────────────────────────┘
         │
         │ 5. Subsequent Requests with Bearer Token
         ▼
┌─────────────────┐    Authorization Header  ┌─────────────────┐
│   API Request   │────────────────────────►│  [Authorize]    │
│                 │                         │   Attribute     │
└─────────────────┘                         └─────────────────┘
                                                     │
                                                     │ 6. Check Roles
                                                     ▼
                                            ┌─────────────────┐
                                            │ Role Validation │
                                            │ Patient/Doctor/ │
                                            │     Admin       │
                                            └─────────────────┘
```

### 4.2 Role-Based Access Control Matrix

```
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│    Resource     │ │     Patient     │ │     Doctor      │ │      Admin      │
├─────────────────┤ ├─────────────────┤ ├─────────────────┤ ├─────────────────┤
│ Own Profile     │ │       R/W       │ │       R/W       │ │       R/W       │
│ Other Profiles  │ │        -        │ │   R (assigned)  │ │       R/W       │
│ Medical Records │ │    R (own)      │ │   R/W (assigned)│ │       R/W       │
│ Appointments    │ │    R/W (own)    │ │   R/W (assigned)│ │       R/W       │
│ Access Logs     │ │    R (own)      │ │        -        │ │       R/W       │
│ System Config   │ │        -        │ │        -        │ │       R/W       │
└─────────────────┘ └─────────────────┘ └─────────────────┘ └─────────────────┘

Legend: R = Read, W = Write, - = No Access
```

---

## 5. API DESIGN & ENDPOINT STRUCTURE

### 5.1 RESTful API Design

```
API Base: https://localhost:5152/api

Authentication Endpoints:
├── POST   /auth/register           │ User registration
├── POST   /auth/login             │ User authentication  
├── POST   /auth/verify-mfa        │ MFA verification
├── GET    /auth/me                │ Get current user info
└── PUT    /auth/me                │ Update current user

Patient Endpoints:
├── GET    /patient/dashboard      │ Patient dashboard
├── GET    /patient/my-doctors     │ Get assigned doctors
├── POST   /patient/add-doctor     │ Add doctor to patient
├── DELETE /patient/remove-doctor  │ Remove doctor from patient
├── GET    /patient/access-log     │ Get access history
└── GET    /patient/access-log/summary │ Get access summary

Doctor Endpoints:
├── GET    /doctor/my-patients     │ Get assigned patients
├── GET    /doctor/patient/{id}    │ Get patient details (LOGGED)
├── GET    /doctor/search          │ Search doctors
└── PUT    /doctor/profile         │ Update doctor profile

Admin Endpoints:
├── GET    /admin/users            │ Manage all users
├── GET    /admin/patient-access-logs │ Monitor access patterns
├── GET    /admin/statistics       │ System statistics
└── POST   /admin/assign-patient-to-doctor │ Force assignments
```

### 5.2 HTTP Status Code Strategy

```
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│   2xx Success   │ │   4xx Client    │ │   5xx Server    │
│                 │ │     Error       │ │     Error       │
├─────────────────┤ ├─────────────────┤ ├─────────────────┤
│ 200 OK          │ │ 400 Bad Request │ │ 500 Internal    │
│ 201 Created     │ │ 401 Unauthorized│ │ 503 Unavailable │
│ 204 No Content  │ │ 403 Forbidden   │ │                 │
│                 │ │ 404 Not Found   │ │                 │
│                 │ │ 409 Conflict    │ │                 │
└─────────────────┘ └─────────────────┘ └─────────────────┘

Standardized Response Format:
{
  "success": true/false,
  "message": "Human readable message",
  "data": { ... },
  "errors": { ... }
}
```

---

## 6. AUDIT & TRANSPARENCY SYSTEM

### 6.1 Access Logging Flow

```
┌─────────────────┐    1. API Request      ┌─────────────────┐
│     Doctor      │───────────────────────►│  DoctorController│
│   Client App    │                        │                 │
└─────────────────┘                        └─────────────────┘
                                                    │
                                           2. Before Processing
                                                    ▼
                                          ┌─────────────────┐
                                          │PatientAccessLog │
                                          │    Service      │
                                          │                 │
                                          │ LogAccessAsync()│
                                          └─────────────────┘
                                                    │
                                           3. Store Log Entry
                                                    ▼
                                          ┌─────────────────┐
                                          │  Database       │
                                          │ PatientAccessLog│
                                          │                 │
                                          │ + Doctor ID     │
                                          │ + Patient ID    │
                                          │ + Timestamp     │
                                          │ + Access Type   │
                                          │ + IP Address    │
                                          │ + User Agent    │
                                          └─────────────────┘
                                                    │
                                           4. Continue Processing
                                                    ▼
                                          ┌─────────────────┐
                                          │ Return Patient  │
                                          │     Data        │
                                          └─────────────────┘
```

### 6.2 Transparency Dashboard

```
Patient Access Dashboard:
┌─────────────────────────────────────────────────────────────────┐
│  Who accessed your medical data?                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  📊 Summary:                                                    │
│  • Total Accesses: 47                                          │
│  • Unique Doctors: 3                                           │
│  • Last Access: 2025-10-01 14:30 by Dr. Smith                 │
│                                                                 │
│  📋 Recent Activity:                                            │
│  • 2025-10-01 14:30 - Dr. Smith viewed patient details         │
│  • 2025-09-28 09:15 - Dr. Johnson accessed medical records     │
│  • 2025-09-25 16:45 - Dr. Brown reviewed visit history         │
│                                                                 │
│  🔍 Filter by:                                                  │
│  • Doctor: [All Doctors ▼]                                     │
│  • Date Range: [Last 30 days ▼]                                │
│  • Access Type: [All Types ▼]                                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. SCALABILITY & PERFORMANCE

### 7.1 Database Optimization Strategy

```
Performance Optimizations:

┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│    Indexing     │    │   Pagination    │    │    Caching      │
├─────────────────┤    ├─────────────────┤    ├─────────────────┤
│• PatientId      │    │• Page-based     │    │• Memory Cache   │
│• DoctorId       │    │• Skip/Take      │    │• Redis (future) │
│• AccessedAt     │    │• Limit Results  │    │• Response Cache │
│• Composite Keys │    │• Default: 20    │    │• Entity Cache   │
└─────────────────┘    └─────────────────┘    └─────────────────┘

Database Indexes Created:
CREATE INDEX IX_PatientAccessLogs_PatientId ON PatientAccessLogs(PatientId);
CREATE INDEX IX_PatientAccessLogs_DoctorId ON PatientAccessLogs(DoctorId);
CREATE INDEX IX_PatientAccessLogs_AccessedAt ON PatientAccessLogs(AccessedAt);
CREATE INDEX IX_PatientAccessLogs_PatientId_AccessedAt ON PatientAccessLogs(PatientId, AccessedAt);
```

### 7.2 Layered Caching Strategy

```
┌─────────────────────────────────────────────────────────────────┐
│                      Browser Cache                               │
│                   (Static Resources)                            │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                    API Response Cache                            │
│                  (Frequently Accessed Data)                     │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Application Memory Cache                       │
│                    (User Sessions, Tokens)                      │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Database Level                              │
│                   (Query Plan Cache)                            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 8. ERROR HANDLING & LOGGING

### 8.1 Centralized Error Handling

```
┌─────────────────┐    Exception        ┌─────────────────┐
│   Controller    │────────────────────►│  BaseApiCtrl    │
│    Action       │                     │ ErrorResponse() │
└─────────────────┘                     └─────────────────┘
         │                                       │
         │ Try-Catch                            │
         ▼                                       ▼
┌─────────────────┐    Log Error        ┌─────────────────┐
│   ILogger<T>    │◄────────────────────│ Standardized    │
│   (Serilog)     │                     │ Error Format    │
└─────────────────┘                     └─────────────────┘
         │                                       │
         │ Store to File/DB                     │
         ▼                                       ▼
┌─────────────────┐    Return JSON      ┌─────────────────┐
│   Log Files     │                     │   Client        │
│   logs/         │                     │   Response      │
└─────────────────┘                     └─────────────────┘

Error Response Format:
{
  "success": false,
  "message": "An error occurred",
  "errors": {
    "field": ["validation messages"]
  },
  "timestamp": "2025-10-02T10:30:00Z",
  "traceId": "12345-67890-abcde"
}
```

---

## 9. FUTURE EXTENSIBILITY

### 9.1 Microservices Migration Path

```
Current Monolith → Future Microservices

┌─────────────────────────────────────────────────────────────────┐
│                    Current Monolith                             │
├─────────────────────────────────────────────────────────────────┤
│  All Services in One Application                                │
│  • AuthService                                                  │
│  • PatientService                                               │
│  • DoctorService                                                │
│  • AuditService                                                 │
│  • NotificationService                                          │
└─────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼ Migration Path
┌─────────────────────────────────────────────────────────────────┐
│                  Future Microservices                           │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌──────────────┐│
│  │    Auth     │ │   Patient   │ │   Doctor    │ │    Audit     ││
│  │  Service    │ │   Service   │ │   Service   │ │   Service    ││
│  └─────────────┘ └─────────────┘ └─────────────┘ └──────────────┘│
│                                                                 │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌──────────────┐│
│  │Notification │ │    API      │ │  Database   │ │   Message    ││
│  │  Service    │ │  Gateway    │ │   Per Svc   │ │    Queue     ││
│  └─────────────┘ └─────────────┘ └─────────────┘ └──────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

---

**Această arhitectură demonstrează implementarea principiilor moderne de Software Design și oferă o bază solidă pentru scalabilitate și mentenabilitate viitoare.**