# Medical API - Quick Reference Guide

## Authentication Endpoints
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| POST | `/api/auth/register` | Register new user | No |
| POST | `/api/auth/login` | User login | No |
| POST | `/api/auth/verify-mfa` | Verify MFA code | Temp Token |
| GET | `/api/auth/me` | Get current user | Yes |
| PUT | `/api/auth/me` | Update profile | Yes |
| POST | `/api/auth/change-password` | Change password | Yes |
| POST | `/api/auth/assign-role/{userId}` | Assign role (Admin) | Admin |
| DELETE | `/api/auth/remove-role/{userId}` | Remove role (Admin) | Admin |
| POST | `/api/auth/verification-code` | Get verification code | No |
| POST | `/api/auth/verify-code` | Verify email code | No |

## MFA Endpoints
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| POST | `/api/mfa/setup` | Setup MFA | Yes |
| POST | `/api/mfa/verify` | Verify MFA setup | Yes |
| DELETE | `/api/mfa/disable` | Disable MFA | Yes |

## Appointment Endpoints
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| GET | `/api/appointment` | Get appointments | Yes |
| POST | `/api/appointment` | Create appointment | Yes |
| GET | `/api/appointment/{id}` | Get appointment by ID | Yes |
| PUT | `/api/appointment/{id}` | Update appointment | Yes |
| DELETE | `/api/appointment/{id}` | Delete appointment | Yes |
| PATCH | `/api/appointment/{id}/status` | Update status (Doctor) | Doctor |
| GET | `/api/appointment/my` | Get my appointments | Yes |
| GET | `/api/appointment/doctor/schedule` | Get doctor schedule | Doctor |
| GET | `/api/appointment/doctor/all` | Get all doctor appointments | Doctor |

## Doctor Endpoints
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| GET | `/api/doctor` | Get all doctors | Yes |
| GET | `/api/doctor/{id}` | Get doctor profile | Yes |
| PUT | `/api/doctor/{id}` | Update doctor profile | Doctor/Admin |
| GET | `/api/doctor/{id}/availability` | Get doctor availability | Yes |
| POST | `/api/doctor/{id}/schedule` | Set doctor schedule | Doctor |

## Patient Endpoints
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| GET | `/api/patient/{id}` | Get patient profile | Doctor/Admin |
| PUT | `/api/patient/{id}` | Update patient info | Doctor/Admin |
| GET | `/api/patient/{id}/medical-history` | Get medical history | Doctor/Admin |
| POST | `/api/patient/{id}/medical-record` | Add medical record | Doctor |

## File Management Endpoints
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| POST | `/api/files/upload` | Upload file | Yes |
| GET | `/api/files/{id}` | Download file | Yes |
| DELETE | `/api/files/{id}` | Delete file | Yes |
| GET | `/api/files/user/{userId}` | Get user files | Yes |

## Rating Endpoints
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| POST | `/api/rating` | Submit rating | Patient |
| GET | `/api/rating/doctor/{doctorId}` | Get doctor ratings | Yes |
| GET | `/api/rating/appointment/{appointmentId}` | Get appointment rating | Yes |

## Notification Endpoints
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| GET | `/api/singlenotification` | Get notifications | Yes |
| GET | `/api/singlenotification/{id}` | Get notification by ID | Yes |
| POST | `/api/singlenotification/appointment` | Create appointment notification | Doctor/Admin |
| POST | `/api/singlenotification/visit-record` | Create visit notification | Doctor |
| POST | `/api/singlenotification/registration` | Create registration notification | Admin |

## Admin Endpoints
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| GET | `/api/admin/test/users-count` | Get users count | Admin |
| GET | `/api/admin/users` | Get all users | Admin |
| POST | `/api/admin/users` | Create user | Admin |
| PUT | `/api/admin/users/{userId}` | Update user | Admin |
| DELETE | `/api/admin/users/{userId}` | Delete user | Admin |
| POST | `/api/admin/users/{userId}/roles` | Assign role | Admin |
| DELETE | `/api/admin/users/{userId}/roles/{roleName}` | Remove role | Admin |

## Document Management Endpoints
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| POST | `/api/document/upload` | Upload document (Doctor/Admin for any patient) | Yes |
| POST | `/api/document/upload/my` | Upload document (Patient for self) | Patient |
| GET | `/api/document/my` | Get my documents (Patient) | Patient |
| GET | `/api/document/{id}/download` | Download document | Yes |
| DELETE | `/api/document/{id}` | Delete document | Yes |
| GET | `/api/document/patient/{patientId}` | Get patient documents | Doctor/Admin |

## Health Check
| Method | Endpoint | Purpose | Auth Required |
|--------|----------|---------|---------------|
| GET | `/health` | API health check | No |

## Response Format
All endpoints return responses in this format:
```json
{
  "success": true/false,
  "message": "Description of the result",
  "data": {}, // Response data (on success)
  "errors": {}, // Validation errors (on failure)
  "statusCode": 200
}
```

## Authentication
- Use Bearer token in Authorization header: `Bearer {your_jwt_token}`
- Tokens expire and need refresh or re-login
- Role-based access control enforced

## Common Status Codes
- **200**: Success
- **201**: Created
- **400**: Bad Request (validation errors)
- **401**: Unauthorized (login required)
- **403**: Forbidden (insufficient permissions)
- **404**: Not Found
- **409**: Conflict (duplicate data)
- **500**: Server Error

## File Upload Guidelines
- Maximum file size: 10MB
- Supported formats: PDF, JPEG, PNG, TIFF, DICOM
- Use multipart/form-data for file uploads
- Include metadata in form fields
- **Required DocumentType values:** MedicalCertificate, LabResults, XRay, MRI, Prescription, Referral, Other

## Date Formats
- Use ISO 8601 format: `YYYY-MM-DDTHH:mm:ss.sssZ`
- Example: `2025-10-01T14:30:00.000Z`

## Pagination
Some endpoints support pagination with query parameters:
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 10, max: 100)
- `sortBy`: Sort field
- `sortOrder`: `asc` or `desc`

## Error Handling Best Practices
1. Always check the `success` field in responses
2. Handle network errors and timeouts
3. Display user-friendly error messages
4. Implement retry logic for transient errors
5. Log errors for debugging

## Security Notes
- All sensitive operations require authentication
- File uploads are scanned for malware
- Rate limiting is applied to prevent abuse
- HTTPS required in production
- Input validation on all endpoints