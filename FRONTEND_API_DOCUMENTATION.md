# Medical API Frontend Documentation

## Overview
This documentation provides comprehensive guidance for frontend developers working with the Medical API backend. It includes all endpoints, usage examples, authentication requirements, and implementation guidelines for AI-assisted development with Claude AI.

## Base Configuration

### API Base URL
```typescript
const API_BASE_URL = 'https://localhost:7152/api' // Development
// const API_BASE_URL = 'https://your-production-domain.com/api' // Production
```

### Common Headers
```typescript
const commonHeaders = {
  'Content-Type': 'application/json',
  'Accept': 'application/json'
}

// For authenticated requests
const authHeaders = {
  ...commonHeaders,
  'Authorization': `Bearer ${localStorage.getItem('authToken')}`
}
```

## Authentication & Authorization

### 1. User Registration
**Endpoint:** `POST /api/auth/register`
**Purpose:** Register new users (patients)
**Authentication:** None required

```typescript
interface RegisterDto {
  email: string
  password: string
  confirmPassword: string
  firstName: string
  lastName: string
  phoneNumber: string
  dateOfBirth: string // ISO date format
  gender: number // 0 = Female, 1 = Male, 2 = Other
  address: string
}

// Usage Example
const registerUser = async (userData: RegisterDto) => {
  const response = await fetch(`${API_BASE_URL}/auth/register`, {
    method: 'POST',
    headers: commonHeaders,
    body: JSON.stringify(userData)
  })
  return await response.json()
}
```

**Frontend Implementation:**
- Use for patient registration forms
- Implement password confirmation validation
- Show email verification prompt after successful registration
- Handle validation errors for duplicate emails, weak passwords

### 2. User Login
**Endpoint:** `POST /api/auth/login`
**Purpose:** Authenticate users and get JWT token
**Authentication:** None required

```typescript
interface LoginDto {
  email: string
  password: string
}

interface AuthResponse {
  token: string
  refreshToken: string
  user: {
    id: string
    email: string
    firstName: string
    lastName: string
    role: string
    isEmailVerified: boolean
    isMfaEnabled: boolean
  }
}

// Usage Example
const loginUser = async (credentials: LoginDto) => {
  const response = await fetch(`${API_BASE_URL}/auth/login`, {
    method: 'POST',
    headers: commonHeaders,
    body: JSON.stringify(credentials)
  })
  const data = await response.json()
  
  if (data.success) {
    localStorage.setItem('authToken', data.data.token)
    localStorage.setItem('refreshToken', data.data.refreshToken)
  }
  return data
}
```

**Frontend Implementation:**
- Store JWT token securely
- Redirect based on user role (Admin, Doctor, Patient)
- Handle MFA if enabled
- Show email verification reminder if not verified

### 3. Get Current User
**Endpoint:** `GET /api/auth/me`
**Purpose:** Get authenticated user's profile
**Authentication:** Required (JWT)

```typescript
const getCurrentUser = async () => {
  const response = await fetch(`${API_BASE_URL}/auth/me`, {
    headers: authHeaders
  })
  return await response.json()
}
```

**Frontend Implementation:**
- Use for profile pages
- Check authentication status on app initialization
- Update user context/state

### 4. Update Profile
**Endpoint:** `PUT /api/auth/me`
**Purpose:** Update user profile information
**Authentication:** Required (JWT)

```typescript
interface UpdateUserDto {
  firstName?: string
  lastName?: string
  phoneNumber?: string
  dateOfBirth?: string
  gender?: number
  address?: string
  profilePicture?: string // Base64 or file upload
}

const updateProfile = async (updateData: UpdateUserDto) => {
  const response = await fetch(`${API_BASE_URL}/auth/me`, {
    method: 'PUT',
    headers: authHeaders,
    body: JSON.stringify(updateData)
  })
  return await response.json()
}
```

### 5. Change Password
**Endpoint:** `POST /api/auth/change-password`
**Purpose:** Change user password
**Authentication:** Required (JWT)

```typescript
interface ChangePasswordDto {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}
```

## Multi-Factor Authentication (MFA)

### 1. Setup MFA
**Endpoint:** `POST /api/mfa/setup`
**Purpose:** Generate QR code for MFA setup
**Authentication:** Required (JWT)

```typescript
const setupMFA = async () => {
  const response = await fetch(`${API_BASE_URL}/mfa/setup`, {
    method: 'POST',
    headers: authHeaders
  })
  return await response.json() // Returns QR code and secret
}
```

### 2. Verify MFA Login
**Endpoint:** `POST /api/auth/verify-mfa`
**Purpose:** Complete login with MFA code
**Authentication:** Required (temporary token from login)

```typescript
interface VerifyMfaLoginDto {
  tempToken: string
  code: string
}
```

## Appointment Management

### 1. Get Appointments
**Endpoint:** `GET /api/appointment`
**Purpose:** Get user's appointments (filtered by role)
**Authentication:** Required (JWT)

```typescript
const getAppointments = async (filters?: {
  status?: string
  dateFrom?: string
  dateTo?: string
  doctorId?: string
}) => {
  const queryParams = new URLSearchParams(filters as any).toString()
  const response = await fetch(`${API_BASE_URL}/appointment?${queryParams}`, {
    headers: authHeaders
  })
  return await response.json()
}
```

**Frontend Implementation:**
- Use for appointment lists in patient/doctor dashboards
- Implement filtering by date, status, doctor
- Show different views for patients vs doctors

### 2. Create Appointment
**Endpoint:** `POST /api/appointment`
**Purpose:** Book new appointment
**Authentication:** Required (JWT)

```typescript
interface CreateAppointmentDto {
  doctorId: string
  appointmentDate: string // ISO datetime
  reasonForVisit: string
  appointmentType: string // "Consultation" | "Follow-up" | "Emergency"
  notes?: string
}

const createAppointment = async (appointmentData: CreateAppointmentDto) => {
  const response = await fetch(`${API_BASE_URL}/appointment`, {
    method: 'POST',
    headers: authHeaders,
    body: JSON.stringify(appointmentData)
  })
  return await response.json()
}
```

**Frontend Implementation:**
- Use in appointment booking forms
- Validate appointment dates (future dates only)
- Show doctor availability calendar
- Send confirmation notifications

### 3. Update Appointment
**Endpoint:** `PUT /api/appointment/{id}`
**Purpose:** Update appointment details
**Authentication:** Required (JWT, role-based access)

### 4. Update Appointment Status
**Endpoint:** `PATCH /api/appointment/{id}/status`
**Purpose:** Change appointment status (doctors only)
**Authentication:** Required (JWT, Doctor role)

```typescript
interface UpdateAppointmentStatusDto {
  status: 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled' | 'NoShow'
  notes?: string
}

const updateAppointmentStatus = async (id: number, statusData: UpdateAppointmentStatusDto) => {
  const response = await fetch(`${API_BASE_URL}/appointment/${id}/status`, {
    method: 'PATCH',
    headers: authHeaders,
    body: JSON.stringify(statusData)
  })
  return await response.json()
}
```

### 5. Get Doctor's Schedule
**Endpoint:** `GET /api/appointment/doctor/schedule`
**Purpose:** Get doctor's schedule and availability
**Authentication:** Required (JWT, Doctor role)

### 6. Get My Appointments
**Endpoint:** `GET /api/appointment/my`
**Purpose:** Get current user's appointments
**Authentication:** Required (JWT)

## Doctor Management

### 1. Get Doctors
**Endpoint:** `GET /api/doctor`
**Purpose:** Get list of available doctors
**Authentication:** Required (JWT)

```typescript
const getDoctors = async (filters?: {
  specialization?: string
  isAvailable?: boolean
  search?: string
}) => {
  const queryParams = new URLSearchParams(filters as any).toString()
  const response = await fetch(`${API_BASE_URL}/doctor?${queryParams}`, {
    headers: authHeaders
  })
  return await response.json()
}
```

**Frontend Implementation:**
- Use for doctor selection in appointment booking
- Implement search by name or specialization
- Show doctor profiles with ratings and availability

### 2. Get Doctor Profile
**Endpoint:** `GET /api/doctor/{id}`
**Purpose:** Get detailed doctor information
**Authentication:** Required (JWT)

```typescript
const getDoctorProfile = async (doctorId: string) => {
  const response = await fetch(`${API_BASE_URL}/doctor/${doctorId}`, {
    headers: authHeaders
  })
  return await response.json()
}
```

## Patient Management

### 1. Get Patient Profile
**Endpoint:** `GET /api/patient/{id}`
**Purpose:** Get patient details (doctors/admin only)
**Authentication:** Required (JWT, Doctor/Admin role)

### 2. Update Patient Information
**Endpoint:** `PUT /api/patient/{id}`
**Purpose:** Update patient medical information
**Authentication:** Required (JWT, Doctor role)

## Medical Document Management

### 1. Upload Document (Patient Self-Upload)
**Endpoint:** `POST /api/document/upload/my`
**Purpose:** Allow patients to upload their own medical documents
**Authentication:** Required (JWT, Patient role)

```typescript
const uploadMyDocument = async (file: File, documentType: string, description?: string) => {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('documentType', documentType)
  if (description) {
    formData.append('description', description)
  }

  const response = await fetch(`${API_BASE_URL}/document/upload/my`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${localStorage.getItem('authToken')}`
      // Don't set Content-Type for FormData
    },
    body: formData
  })
  return await response.json()
}

// Document Types enum (must match backend)
type DocumentType = 'MedicalCertificate' | 'LabResults' | 'XRay' | 'MRI' | 'Prescription' | 'Referral' | 'Other'
```

**Frontend Implementation:**
- Use for patient document self-upload
- Support drag-and-drop file uploads
- Show upload progress
- Validate file types: PDF, JPEG, PNG, TIFF, DICOM
- Maximum file size: 10MB
- Always provide a valid DocumentType from the enum
- Handle validation errors gracefully

**Error Handling:**
```typescript
try {
  const result = await uploadMyDocument(file, 'LabResults', 'Blood test results')
  console.log('Upload successful:', result)
} catch (error) {
  if (error.message.includes('Document type is required')) {
    // Handle missing document type
  } else if (error.message.includes('Invalid document type')) {
    // Handle invalid document type - show valid options
  } else if (error.message.includes('File size exceeds')) {
    // Handle file too large
  } else if (error.message.includes('Invalid file type')) {
    // Handle unsupported file type
  }
}
```

### 2. Upload Document (Doctor/Admin)
**Endpoint:** `POST /api/document/upload`
**Purpose:** Allow doctors/admins to upload documents for any patient
**Authentication:** Required (JWT, Doctor/Admin role)

```typescript
interface UploadDocumentDto {
  patientId: string
  file: File
  documentType: string
  description?: string
}

const uploadDocument = async (uploadData: UploadDocumentDto) => {
  const formData = new FormData()
  formData.append('file', uploadData.file)
  formData.append('patientId', uploadData.patientId)
  formData.append('documentType', uploadData.documentType)
  if (uploadData.description) {
    formData.append('description', uploadData.description)
  }

  const response = await fetch(`${API_BASE_URL}/document/upload`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${localStorage.getItem('authToken')}`
    },
    body: formData
  })
  return await response.json()
}
```

### 3. Get My Documents (Patient)
**Endpoint:** `GET /api/document/my`
**Purpose:** Get current patient's medical documents
**Authentication:** Required (JWT, Patient role)

```typescript
interface MedicalDocumentDto {
  id: string
  patientId: string
  fileName: string
  documentType: string
  description?: string
  uploadedById: string
  uploadedByName: string
  fileSizeBytes: number
  mimeType: string
  createdAt: string
}

const getMyDocuments = async (): Promise<MedicalDocumentDto[]> => {
  const response = await fetch(`${API_BASE_URL}/document/my`, {
    headers: authHeaders
  })
  const data = await response.json()
  return data.data
}
```

**Frontend Implementation:**
- Use in patient dashboard to show personal documents
- Display document type, upload date, file size
- Allow sorting and filtering
- Show who uploaded each document

### 4. Get Patient Documents (Doctor/Admin)
**Endpoint:** `GET /api/document/patient/{patientId}`
**Purpose:** Get documents for a specific patient
**Authentication:** Required (JWT, Doctor/Admin role)

```typescript
const getPatientDocuments = async (patientId: string): Promise<MedicalDocumentDto[]> => {
  const response = await fetch(`${API_BASE_URL}/document/patient/${patientId}`, {
    headers: authHeaders
  })
  const data = await response.json()
  return data.data
}
```

### 5. Download Document
**Endpoint:** `GET /api/document/{documentId}/download`
**Purpose:** Download medical document
**Authentication:** Required (JWT, patients can only download their own)

```typescript
const downloadDocument = async (documentId: string) => {
  const response = await fetch(`${API_BASE_URL}/document/${documentId}/download`, {
    headers: authHeaders
  })
  
  if (response.ok) {
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    
    // Get filename from Content-Disposition header or use a default
    const contentDisposition = response.headers.get('Content-Disposition')
    const filename = contentDisposition
      ? contentDisposition.split('filename=')[1]?.replace(/['"]/g, '')
      : 'document'
    
    a.download = filename
    a.click()
    window.URL.revokeObjectURL(url)
  } else {
    throw new Error('Failed to download document')
  }
}
```

**Frontend Implementation:**
- Use for document viewing/download in medical records
- Handle different file types appropriately
- Show download progress for large files
- Implement proper error handling

## Rating System

### 1. Submit Rating
**Endpoint:** `POST /api/rating`
**Purpose:** Rate doctor after appointment
**Authentication:** Required (JWT, Patient role)

```typescript
interface CreateRatingDto {
  appointmentId: number
  rating: number // 1-5
  comment?: string
}

const submitRating = async (ratingData: CreateRatingDto) => {
  const response = await fetch(`${API_BASE_URL}/rating`, {
    method: 'POST',
    headers: authHeaders,
    body: JSON.stringify(ratingData)
  })
  return await response.json()
}
```

### 2. Get Doctor Ratings
**Endpoint:** `GET /api/rating/doctor/{doctorId}`
**Purpose:** Get ratings for a specific doctor
**Authentication:** Required (JWT)

## Notifications

### 1. Get Notifications
**Endpoint:** `GET /api/singlenotification`
**Purpose:** Get user notifications
**Authentication:** Required (JWT)

```typescript
const getNotifications = async () => {
  const response = await fetch(`${API_BASE_URL}/singlenotification`, {
    headers: authHeaders
  })
  return await response.json()
}
```

### 2. Mark Notification as Read
**Endpoint:** `PUT /api/singlenotification/{id}`
**Purpose:** Mark notification as read
**Authentication:** Required (JWT)

## Admin Functions

### 1. Get All Users
**Endpoint:** `GET /api/admin/users`
**Purpose:** Get list of all users (admin only)
**Authentication:** Required (JWT, Admin role)

```typescript
const getAllUsers = async (filters?: {
  role?: string
  isVerified?: boolean
  search?: string
  page?: number
  pageSize?: number
}) => {
  const queryParams = new URLSearchParams(filters as any).toString()
  const response = await fetch(`${API_BASE_URL}/admin/users?${queryParams}`, {
    headers: authHeaders
  })
  return await response.json()
}
```

### 2. Create User
**Endpoint:** `POST /api/admin/users`
**Purpose:** Create new user (admin only)
**Authentication:** Required (JWT, Admin role)

### 3. Update User
**Endpoint:** `PUT /api/admin/users/{userId}`
**Purpose:** Update any user (admin only)
**Authentication:** Required (JWT, Admin role)

### 4. Delete User
**Endpoint:** `DELETE /api/admin/users/{userId}`
**Purpose:** Delete user (admin only)
**Authentication:** Required (JWT, Admin role)

### 5. Assign Role
**Endpoint:** `POST /api/admin/users/{userId}/roles`
**Purpose:** Assign role to user (admin only)
**Authentication:** Required (JWT, Admin role)

### 6. Remove Role
**Endpoint:** `DELETE /api/admin/users/{userId}/roles/{roleName}`
**Purpose:** Remove role from user (admin only)
**Authentication:** Required (JWT, Admin role)

## Error Handling

### Standard Error Response Format
```typescript
interface ErrorResponse {
  success: false
  message: string
  errors?: { [key: string]: string[] }
  statusCode: number
}
```

### Common HTTP Status Codes
- `200` - Success
- `201` - Created
- `400` - Bad Request (validation errors)
- `401` - Unauthorized (missing/invalid token)
- `403` - Forbidden (insufficient permissions)
- `404` - Not Found
- `409` - Conflict (duplicate resources)
- `500` - Internal Server Error

### Error Handling Implementation
```typescript
const handleApiResponse = async <T>(response: Response): Promise<T> => {
  const data = await response.json()
  
  if (!response.ok) {
    if (response.status === 401) {
      // Redirect to login or refresh token
      localStorage.removeItem('authToken')
      window.location.href = '/login'
    }
    throw new Error(data.message || 'An error occurred')
  }
  
  return data
}
```

## Authentication State Management

### React Context Example
```typescript
interface AuthContextType {
  user: User | null
  token: string | null
  login: (credentials: LoginDto) => Promise<void>
  logout: () => void
  isAuthenticated: boolean
  isLoading: boolean
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}
```

## Role-Based Access Control

### Protected Route Component
```typescript
interface ProtectedRouteProps {
  children: React.ReactNode
  requiredRole?: 'Admin' | 'Doctor' | 'Patient'
  redirectTo?: string
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredRole,
  redirectTo = '/login'
}) => {
  const { user, isAuthenticated } = useAuth()
  
  if (!isAuthenticated) {
    return <Navigate to={redirectTo} />
  }
  
  if (requiredRole && user?.role !== requiredRole) {
    return <Navigate to="/unauthorized" />
  }
  
  return <>{children}</>
}
```

## Real-time Features

### WebSocket Connection (if implemented)
```typescript
const connectWebSocket = () => {
  const token = localStorage.getItem('authToken')
  const ws = new WebSocket(`wss://localhost:7152/notifications?token=${token}`)
  
  ws.onmessage = (event) => {
    const notification = JSON.parse(event.data)
    // Handle real-time notifications
  }
  
  return ws
}
```

## Data Validation

### Form Validation Examples
```typescript
// Password validation
const validatePassword = (password: string) => {
  const minLength = password.length >= 8
  const hasUpperCase = /[A-Z]/.test(password)
  const hasLowerCase = /[a-z]/.test(password)
  const hasNumbers = /\d/.test(password)
  const hasNonalphas = /\W/.test(password)
  
  return {
    isValid: minLength && hasUpperCase && hasLowerCase && hasNumbers && hasNonalphas,
    errors: {
      minLength: !minLength ? 'Password must be at least 8 characters' : '',
      hasUpperCase: !hasUpperCase ? 'Password must contain uppercase letter' : '',
      hasLowerCase: !hasLowerCase ? 'Password must contain lowercase letter' : '',
      hasNumbers: !hasNumbers ? 'Password must contain number' : '',
      hasNonalphas: !hasNonalphas ? 'Password must contain special character' : ''
    }
  }
}

// Email validation
const validateEmail = (email: string) => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}
```

## Frontend Architecture Recommendations

### 1. State Management
- Use React Context for authentication state
- Consider Redux Toolkit for complex state management
- Use React Query/TanStack Query for server state

### 2. HTTP Client Setup
```typescript
// api.ts
import axios from 'axios'

const api = axios.create({
  baseURL: process.env.REACT_APP_API_BASE_URL,
  timeout: 10000,
})

// Request interceptor for auth token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('authToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Response interceptor for error handling
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export default api
```

### 3. TypeScript Interfaces
Create comprehensive TypeScript interfaces for all DTOs and responses to ensure type safety.

### 4. Environment Configuration
```typescript
// config.ts
export const config = {
  apiBaseUrl: process.env.REACT_APP_API_BASE_URL || 'https://localhost:7152/api',
  wsUrl: process.env.REACT_APP_WS_URL || 'wss://localhost:7152',
  fileUploadMaxSize: 10 * 1024 * 1024, // 10MB
  supportedFileTypes: ['pdf', 'jpg', 'jpeg', 'png', 'doc', 'docx']
}
```

## Testing Guidelines

### 1. API Integration Tests
```typescript
// __tests__/api/auth.test.ts
import { renderHook } from '@testing-library/react'
import { useAuth } from '../contexts/AuthContext'

describe('Authentication API', () => {
  test('should login successfully', async () => {
    const { result } = renderHook(() => useAuth())
    
    await act(async () => {
      await result.current.login({
        email: 'test@example.com',
        password: 'Test123!'
      })
    })
    
    expect(result.current.isAuthenticated).toBe(true)
  })
})
```

### 2. Mock API Responses
```typescript
// __mocks__/api.ts
export const mockApiResponses = {
  login: {
    success: true,
    data: {
      token: 'mock-jwt-token',
      user: {
        id: '1',
        email: 'test@example.com',
        firstName: 'Test',
        lastName: 'User',
        role: 'Patient'
      }
    }
  }
}
```

## Security Best Practices

1. **Token Storage**: Store JWT tokens securely (consider httpOnly cookies for production)
2. **Input Validation**: Validate all user inputs on frontend and backend
3. **HTTPS**: Always use HTTPS in production
4. **Content Security Policy**: Implement CSP headers
5. **Rate Limiting**: Implement rate limiting for sensitive operations
6. **File Upload Security**: Validate file types and scan for malware

## Performance Optimization

1. **Lazy Loading**: Implement lazy loading for routes and components
2. **Caching**: Cache API responses using React Query
3. **Image Optimization**: Optimize and lazy load images
4. **Bundle Splitting**: Split code bundles for better loading performance
5. **Memoization**: Use React.memo, useMemo, and useCallback appropriately

## AI Development Guidelines (Claude AI)

### 1. Prompt Templates for Claude
When working with Claude AI, use these prompt templates:

**For Component Generation:**
```
Create a React component for [specific functionality] that:
- Uses TypeScript with proper interfaces
- Implements error handling and loading states  
- Follows accessibility best practices
- Uses the Medical API endpoints documented above
- Includes proper validation and user feedback
```

**For API Integration:**
```
Generate API integration code for [endpoint name] that:
- Uses the axios instance configured above
- Includes proper TypeScript types
- Implements error handling with user-friendly messages
- Follows the authentication patterns documented
- Includes loading states and retry logic
```

### 2. Code Generation Guidelines
- Always specify TypeScript usage
- Request proper error handling implementation
- Ask for accessibility features
- Specify responsive design requirements
- Request proper testing implementation

### 3. Common Development Patterns
Provide Claude with these patterns when generating code:

```typescript
// Standard API hook pattern
const useAppointments = () => {
  const [appointments, setAppointments] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  
  useEffect(() => {
    const fetchAppointments = async () => {
      try {
        setLoading(true)
        const response = await getAppointments()
        setAppointments(response.data)
      } catch (err) {
        setError(err.message)
      } finally {
        setLoading(false)
      }
    }
    
    fetchAppointments()
  }, [])
  
  return { appointments, loading, error, refetch: fetchAppointments }
}
```

This comprehensive documentation should enable efficient frontend development with AI assistance while ensuring proper integration with the Medical API backend.