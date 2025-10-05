# Milestone 6: Deployment & Production Readiness

**Goal:** Deploy ASP.NET Core application to AWS Elastic Beanstalk with RDS PostgreSQL

---

## Deliverables

### 1. Application Preparation

**Install Required NuGet Packages:**

Add PostgreSQL support to your project:

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
```

**Update Program.cs for Production:**

Modify your database configuration to support both development (in-memory) and production (PostgreSQL):

```csharp
// Configure database based on environment
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("LibraryDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
```

**Update database initialization:**

Replace `EnsureCreatedAsync()` with `MigrateAsync()`:

```csharp
// Apply database migrations in production
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    if (!app.Environment.IsDevelopment())
    {
        await context.Database.MigrateAsync();
    }
}
```

---

### 2. Create Entity Framework Migrations

**Install EF Core Tools:**

```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"
```

**Create Initial Migration:**

```bash
# Delete old migrations if they exist
rm -rf Migrations/

# Create fresh migration
dotnet ef migrations add InitialCreate

# Verify migration files were created
ls -la Migrations/
```

**Important:** Use static DateTime values in seed data to avoid migration issues:

```csharp
// Correct - static date
CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)

// Incorrect - causes migration warnings
CreatedAt = DateTime.UtcNow
```

---

### 3. Build Production Package

```bash
# Clean previous builds
dotnet clean
rm -rf publish/
rm -f LibraryManagementApi.zip

# Build and publish
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../LibraryManagementApi.zip .
cd ..

# Verify migrations are included
unzip -l LibraryManagementApi.zip | grep -i migration
```

---

### 4. Setting up RDS PostgreSQL Database

Navigate to **AWS Console → RDS → Create database**

**Engine Options:**
- Engine type: PostgreSQL
- Version: PostgreSQL 15.x
- Templates: Free tier (development) or Production

**Availability and Durability:**
- Deployment option: Single DB instance

**Database Settings:**
- DB instance identifier: `library-app-database`
- Master username: `postgres`
- Credentials management: Self managed
- Master password: Create and save a strong password securely

**Instance Configuration:**
- DB instance class: Burstable classes
- Select: `db.t4g.micro` (free tier eligible)

**Storage Configuration:**
- Storage type: General Purpose SSD (gp2)
- Allocated storage: `20` GiB
- Storage autoscaling: Optional

**Connectivity:**
- Compute resource: Don't connect to an EC2 compute resource
- Network type: IPv4
- Virtual Private Cloud (VPC): Default VPC
- DB subnet group: default
- Public access: No
- VPC security group: Choose existing → default
- Availability Zone: No preference

**Additional Configuration:**
- Initial database name: `librarydb`
- DB parameter group: default.postgres15
- Backup retention: 7 days (production) or 1 day (development)
- Encryption: Enable encryption at rest

**After Creation:**
- Wait 5-10 minutes for database to become "Available"
- Navigate to your database in RDS console
- Copy the **Endpoint** from Connectivity & security tab
- Format: `library-app-database.xxxxx.us-east-1.rds.amazonaws.com`
- Note the **Port**: 5432

---

### 5. Setting up Elastic Beanstalk Application

Navigate to **AWS Console → Elastic Beanstalk → Create application**

**Environment Tier:**
- Select: Web server environment

**Application Information:**
- Application name: `library-management-api`
- Application tags: (Optional)

**Environment Information:**
- Environment name: `library-api-env`
- Domain: Leave blank (AWS will generate URL)
- Description: Library Management System REST API

**Platform Configuration:**
- Platform: .NET Core on Linux
- Platform branch: .NET 8 or .NET 9 running on 64bit Amazon Linux 2023
- Platform version: Latest recommended version

**Application Code:**
- Select: Upload your code
- Version label: `v1.0.0`
- Source code origin: Local file
- Choose file: Select `LibraryManagementApi.zip`

**Presets:**
- Configuration presets: Single instance (free tier)

Click **"Next"** to configure more options

---

### 6. Service Access Configuration

**IAM Roles:**
- Service role: `aws-elasticbeanstalk-service-role`
- EC2 instance profile: `aws-elasticbeanstalk-ec2-role`

AWS will automatically create these roles if they don't exist.

**EC2 Key Pair:**
- Leave as default (not required)

---

### 7. Networking Configuration

**VPC Configuration:**
- VPC: Select default VPC (must match your RDS VPC)
- Instance settings:
    - Public IP address: Leave unchecked
- Instance subnets: Select at least one subnet:
    - us-east-1a (subnet-xxxxxxxxx)
    - us-east-1b (subnet-xxxxxxxxx)

**Database:**
- Enable database: Leave **UNCHECKED** (using existing RDS)

**Tags:**
- Optional: Add tags for resource management

---

### 8. Environment Properties Configuration

Configure these environment variables:

| Name | Value | Description |
|------|-------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Activates production configuration |
| `ConnectionStrings__DefaultConnection` | `Host=[RDS-ENDPOINT];Port=5432;Database=librarydb;Username=postgres;Password=[YOUR-PASSWORD]` | PostgreSQL connection string |
| `Jwt__Secret` | `[generate-secure-secret]` | Generate with: `openssl rand -base64 32` |
| `Jwt__Issuer` | `LibraryManagementApi` | JWT token issuer |
| `Jwt__Audience` | `LibraryManagementApiUsers` | JWT token audience |

**To get your RDS endpoint:**
1. Go to RDS console
2. Click on `library-app-database`
3. Find endpoint in Connectivity & security section
4. Copy the full endpoint URL

**To generate JWT secret:**
```bash
openssl rand -base64 32
```

**Note:** The double underscore `__` in `ConnectionStrings__DefaultConnection` is how .NET reads nested configuration from environment variables.

---

### 9. Review and Create

- Review all configuration settings
- Click **"Submit"** to create the environment
- Wait 5-10 minutes for environment creation

---

### 10. Security Group Configuration

After both RDS and Elastic Beanstalk are running:

**Update RDS Security Group:**
1. Navigate to **EC2 → Security Groups**
2. Find your RDS security group (check RDS instance details)
3. Click **Edit inbound rules**
4. Add inbound rule:
    - Type: PostgreSQL
    - Port: 5432
    - Source: Custom → Select Elastic Beanstalk security group
    - Description: "Allow EB to connect to RDS"
5. Save rules

---

## Post-Deployment Verification

### 1. Check Environment Health

- Go to Elastic Beanstalk console
- Environment health should show **green "Ok"** status
- If red, check logs for errors

### 2. Test API Endpoints

**Access Swagger UI:**
```
http://[your-eb-url]/swagger
```

**Register a user:**
```bash
curl -X POST http://[your-eb-url]/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#",
    "firstName": "Test",
    "lastName": "User",
    "phoneNumber": "+1-555-0123"
  }'
```

**Login:**
```bash
curl -X POST http://[your-eb-url]/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#"
  }'
```

**Browse catalog (no auth required):**
```bash
curl http://[your-eb-url]/api/catalog/books
```

### 3. Verify Database Connection

- Check Elastic Beanstalk logs for successful database migration
- Should see: "Applied migration 'InitialCreate'"
- Database tables should be created automatically

---

## Troubleshooting

### Environment Health is Red

**Check:**
- Application logs in Elastic Beanstalk console
- Verify `ASPNETCORE_ENVIRONMENT=Production` is set
- Confirm all environment variables are configured correctly
- Look for startup errors in logs

### Database Connection Failed

**Check:**
- Connection string format is correct with double underscores
- RDS endpoint matches exactly
- Database name is `librarydb` (as configured in RDS)
- RDS security group allows inbound from EB security group
- Both RDS and EB are in the same VPC
- Database credentials are correct
- RDS instance status is "Available"

### Migration Not Applied

**Check:**
- Migrations folder is included in ZIP file: `unzip -l LibraryManagementApi.zip | grep -i migration`
- `MigrateAsync()` is called in Program.cs
- Application has permission to create tables
- Check logs for migration errors

### DateTime/Timestamp Errors

**Error:** `timestamp with time zone literal cannot be generated`

**Solution:** Always use `DateTimeKind.Utc` for DateTime values:
```csharp
new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
```

### 404 on All Endpoints

**Check:**
- Application started successfully (check logs)
- Controllers are properly configured with routes
- Swagger UI accessible (indicates app is running)

---

## Acceptance Criteria

- [ ] NuGet packages installed (Npgsql, EF Core Design)
- [ ] Migrations created successfully
- [ ] Deployment package (ZIP) built with migrations included
- [ ] RDS PostgreSQL database created with `librarydb` database
- [ ] RDS endpoint obtained and documented
- [ ] Elastic Beanstalk environment created successfully
- [ ] Environment health shows green "Ok" status
- [ ] All environment variables configured correctly
- [ ] Security groups configured (EB can connect to RDS)
- [ ] Swagger UI accessible
- [ ] Can register user successfully
- [ ] Can login and receive JWT token
- [ ] Can browse catalog without authentication
- [ ] Can create reservation with authentication
- [ ] Librarian can checkout and return books
- [ ] Database tables created automatically by migrations

---

## Technical Specifications

- Platform: Elastic Beanstalk .NET Core on Linux
- .NET Version: 8.0 or 9.0
- Database: RDS PostgreSQL 15.x
- Instance: db.t4g.micro (free tier eligible)
- Storage: 20GB RDS
- Region: US East (us-east-1)
- Schema management: EF Core migrations