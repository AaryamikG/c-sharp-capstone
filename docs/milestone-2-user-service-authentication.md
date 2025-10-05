### Milestone 2: User Service & Authentication

**Goal:** Implement complete user authentication and authorization system

#### Prerequisites:

Add the following NuGet packages to your `Capstone.csproj`:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.9" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.14.0" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
```

Install them by running:
```bash
dotnet restore
```

#### Deliverables:

1. **User Registration & Profile**
   - User registration endpoint with email, password, firstName, lastName, phoneNumber
   - Email uniqueness validation (return 400 if email exists)
   - Password hashing with BCrypt (minimum 8 characters, uppercase, lowercase, number, special character)
   - Phone number format validation
   - Profile retrieval with activeReservations and borrowingHistory counts
   - Default role assignment (Patron) on registration
   - Default membershipStatus (Active) on registration

2. **JWT Authentication**
   - JWT token generation with 86400 seconds (24 hour) expiration
   - JWT token claims: userId, email, role
   - JWT authentication middleware configuration
   - Token validation using Microsoft.AspNetCore.Authentication.JwtBearer
   - Stateless authentication (no token storage on server)

3. **Role-Based Access Control**
   - Role enumeration (Patron, Librarian)
   - Authorize attribute with role-based policies
   - Custom authorization handlers for 403 responses
   - Public endpoints: POST /api/auth/register, POST /api/auth/login, GET /api/catalog/books, GET /api/catalog/books/{bookId}
   - Protected endpoints: All others require authentication

4. **API Documentation**
   - Swagger/OpenAPI integration (already configured via Swashbuckle)
   - Bearer token authentication scheme in Swagger UI
   - All 3 authentication/user endpoints documented
   - Request/response examples with DTOs
   - Error response documentation (400, 401, 403, 500)

#### Acceptance Criteria:
- [ ] User can register with all required fields (email, password, firstName, lastName, phoneNumber)
- [ ] Registration returns 201 with userId, role=Patron, membershipStatus=Active
- [ ] JWT token issued on successful login with tokenType "Bearer" and expiresIn 86400
- [ ] Login returns 200 with accessToken and user object
- [ ] Token expires after 24 hours (86400 seconds)
- [ ] Protected endpoints return 401 without valid Authorization header
- [ ] Profile endpoint returns activeReservations and borrowingHistory counts
- [ ] Role-based access enforced (Librarian-only endpoints return 403 for Patron)
- [ ] Swagger UI accessible at /swagger with Bearer auth configured
- [ ] All authentication endpoints have 80%+ test coverage

#### API Endpoints Completed:

**1. POST /api/auth/register**
- Public endpoint
- Request: email, password, firstName, lastName, phoneNumber
- Response 201: userId, email, firstName, lastName, role, membershipStatus, createdAt, message
- Error 400: Email already exists

**2. POST /api/auth/login**
- Public endpoint
- Request: email, password
- Response 200: accessToken, tokenType, expiresIn, user object
- Error 401: Invalid email or password

**3. GET /api/users/profile**
- Requires authentication
- Request: Authorization Bearer token in header
- Response 200: Complete user profile with activeReservations and borrowingHistory counts
- activeReservations = count where status IN (Reserved, CheckedOut)
- borrowingHistory = total count of all reservations

#### Testing Requirements:
- Unit tests for UserService (registration, login, profile retrieval)
- Integration tests for complete authentication flow (register → login → access protected endpoint)
- Security tests for unauthorized access (401 responses)
- JWT token validation and expiration tests
- Email uniqueness validation tests
- Password strength validation tests (min 8 chars, uppercase, lowercase, number, special char)
- Profile statistics calculation tests (activeReservations, borrowingHistory)

#### Technical Specifications:
- JWT library: Microsoft.AspNetCore.Authentication.JwtBearer 9.0.9
- Token library: System.IdentityModel.Tokens.Jwt 8.14.0
- Password hashing: BCrypt.Net-Next 4.0.3
- Token signing algorithm: HS256
- Token secret: Configurable via `appsettings.json` (min 256 bits)
- Stateless authentication (no session cookies)
- Use `[Authorize]` attribute for protected endpoints
- Use `[Authorize(Roles = "Librarian")]` for librarian-only endpoints
- Configure JWT in `Program.cs` using `AddAuthentication().AddJwtBearer()`