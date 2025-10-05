# Data Modeling Guide

### Milestone 1: Data Modeling

**Goal:** Create entity classes and establish database schema for the Library Management System

#### Deliverables:

1. **Entity-Relationship Overview**
   - Understand the three core entities: User, Book, Reservation
   - Identify relationships between entities
   - Understand foreign key constraints

2. **Entity Classes**

   **User Entity:**
   - UserId (Guid, Primary Key)
   - Email (string, unique, not null, max length: 255)
   - PasswordHash (string, BCrypt hashed, not null)
   - FirstName (string, not null, max length: 100)
   - LastName (string, not null, max length: 100)
   - PhoneNumber (string, not null, max length: 20)
   - Role (Enum: Patron, Librarian)
   - MembershipStatus (Enum: Active, Suspended)
   - MemberSince (DateTime?, nullable)
   - CreatedAt, UpdatedAt (Audit fields)

   **Book Entity:**
   - BookId (Guid, Primary Key)
   - Isbn (string, unique, not null, max length: 20)
   - Title (string, not null, max length: 255)
   - Author (string, not null, max length: 255)
   - Genre (string, not null, max length: 100)
   - PublicationYear (int?, nullable)
   - Description (string, TEXT type, nullable)
   - Publisher (string, nullable, max length: 255)
   - PageCount (int?, nullable)
   - Language (string, nullable, max length: 50)
   - TotalCopies (int, not null, default: 0)
   - AvailableCopies (int, not null, default: 0)
   - CreatedAt, UpdatedAt (Audit fields)
   - **Note:** Status property (Available/CheckedOut) is calculated, not stored

   **Reservation Entity:**
   - ReservationId (Guid, Primary Key)
   - BookId (Guid, Foreign Key to Book)
   - UserId (Guid, Foreign Key to User)
   - Status (Enum: Reserved, CheckedOut, Returned, Cancelled)
   - ReservedAt (DateTime, not null)
   - ExpiresAt (DateTime?, nullable)
   - CheckedOutAt (DateTime?, nullable)
   - DueDate (DateTime?, nullable)
   - ReturnedAt (DateTime?, nullable)
   - RenewalCount (int, default: 0)
   - LateDays (int?, nullable)
   - LateFee (decimal?, nullable)
   - Condition (Enum: Good, Fair, Poor, Damaged, nullable)
   - Notes (string, TEXT type, nullable)
   - CreatedAt, UpdatedAt (Audit fields)

3. **Enum Types**

   Create the following enums in your `Models` folder:

   **Role Enum:**
   ```csharp
   public enum Role
   {
       Patron,      // Regular library user
       Librarian    // Staff member who can checkout/return books
   }
   ```

   **MembershipStatus Enum:**
   ```csharp
   public enum MembershipStatus
   {
       Active,      // User can use the system
       Suspended    // User is blocked from making reservations
   }
   ```

   **ReservationStatus Enum:**
   ```csharp
   public enum ReservationStatus
   {
       Reserved,    // Book is reserved but not picked up
       CheckedOut,  // Book has been checked out
       Returned,    // Book has been returned
       Cancelled    // Reservation was cancelled
   }
   ```

   **BookCondition Enum:**
   ```csharp
   public enum BookCondition
   {
       Good,        // Book is in good condition
       Fair,        // Book shows some wear
       Poor,        // Book is damaged but usable
       Damaged      // Book has significant damage
   }
   ```

4. **DbContext and Configuration**
   - Create `ApplicationDbContext` class inheriting from `DbContext`
   - Configure `DbSet<User>`, `DbSet<Book>`, `DbSet<Reservation>`
   - Use Fluent API in `OnModelCreating` for entity configurations
   - Configure relationships, constraints, and indexes

#### Acceptance Criteria:

- [ ] ASP.NET Core application starts successfully on ports 5000/5001
- [ ] All three entity classes created with proper data annotations and configurations
- [ ] All base entities (User, Book, Reservation) can be persisted to in-memory database
- [ ] ApplicationDbContext configured with all three DbSets
- [ ] Project compiles with zero warnings
- [ ] All entity relationships (User ↔ Reservation ↔ Book) are properly configured
- [ ] All four enum types are defined (Role, MembershipStatus, ReservationStatus, BookCondition)
- [ ] Fluent API configurations implemented in `OnModelCreating`
- [ ] Audit fields (CreatedAt/UpdatedAt) auto-populate using appropriate approach
- [ ] Can test entity operations using in-memory database

#### Technical Specifications:

- .NET version: 8.0 or 9.0
- Database: In-Memory (Development), PostgreSQL (Production via AWS RDS)
- ORM: Entity Framework Core
- Schema management: EF Core model validation (Development), Migrations (Production)
- Use `[Key]` attribute or Fluent API for primary keys
- Use `[Required]`, `[MaxLength]`, `[EmailAddress]` data annotations where appropriate
- Configure enums to store as strings in database
- Implement `IEntityTypeConfiguration<T>` for complex configurations (optional but recommended)
- Use DateTime (or DateTimeOffset) for all timestamp fields
- Configure cascade delete behavior for relationships