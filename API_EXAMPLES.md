## Test API Endpoints

Here are some example requests you can test with the Medical API:

### 1. Register a new user
```http
POST http://localhost:5152/api/auth/register
Content-Type: application/json

{
  "email": "patient@test.com",
  "password": "Patient123!",
  "confirmPassword": "Patient123!",
  "firstName": "John",
  "lastName": "Smith",
  "phoneNumber": "+1234567890",
  "dateOfBirth": "1990-01-15",
  "gender": 1,
  "address": "123 Main St, City, State"
}
```

### 2. Login
```http
POST http://localhost:5152/api/auth/login
Content-Type: application/json

{
  "email": "admin@medical.com",
  "password": "Admin123!"
}
```

### 3. Get current user info (requires JWT token)
```http
GET http://localhost:5152/api/auth/me
Authorization: Bearer YOUR_JWT_TOKEN_HERE
```

### 4. Health check
```http
GET http://localhost:5152/health
```

### 5. Assign role (Admin only)
```http
POST http://localhost:5152/api/auth/assign-role/USER_ID_HERE
Authorization: Bearer ADMIN_JWT_TOKEN_HERE
Content-Type: application/json

"Doctor"
```

### 6. Change password (authenticated user)
```http
POST http://localhost:5152/api/auth/change-password
Authorization: Bearer YOUR_JWT_TOKEN_HERE
Content-Type: application/json

{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!",
  "confirmNewPassword": "NewPassword123!"
}
```

### Default Users for Testing:
- **Admin**: admin@medical.com / Admin123!
- **Doctor**: doctor@medical.com / Doctor123!

### Notes:
- Replace `YOUR_JWT_TOKEN_HERE` with the actual token received from login
- Replace `USER_ID_HERE` with an actual user ID from the database
- The API is running on port 5152 (check your console output for the exact port)
- All endpoints return JSON responses
- Authentication endpoints return JWT tokens that expire in 60 minutes (configurable)
