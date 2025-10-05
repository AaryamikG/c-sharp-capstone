# Digital Library Management System API (.NET)

## Business Context

### Overview

The Digital Library Management System is a modern backend API solution designed to digitize and streamline library
operations for public and institutional libraries. As libraries transition from manual record-keeping to digital
platforms, this system provides a comprehensive solution for managing book catalogs, user memberships, and borrowing
workflows.

### Business Problem

Traditional libraries face several operational challenges:

- Manual tracking of book availability and reservations leads to errors and inefficiency
- Limited visibility into borrowing patterns and inventory usage
- Poor user experience with no self-service capabilities for browsing or reserving books
- Difficulty managing overdue books and calculating late fees
- Time-consuming checkout and return processes at the library desk

### Solution

Our Digital Library Management System provides:

- **Self-service portal** for users to browse, search, and reserve books online
- **Automated reservation management** with 7-day pickup windows
- **Real-time availability tracking** to reduce operational overhead
- **Librarian tools** for efficient checkout and return processing
- **Borrowing history** for patrons to track their reading activity
- **Scalable architecture** ready for cloud deployment

### Target Users

1. **Library Patrons**: Browse catalog, reserve books, view borrowing history
2. **Librarians**: Process checkouts and returns, manage reservations

---

## Project Documentation

### Getting Started

1. [Development Environment Setup](docs/dev-environment-setup.md) - Set up in-memory database and local development
   environment
2. [Milestone 1: Data Modeling](docs/milestone-1-data-modeling-guide.md) - Create entity classes and database schema

### Core Features Implementation

3. [Milestone 2: User Service & Authentication](docs/milestone-2-user-service-authentication.md) - Implement
   registration, login, and JWT authentication
4. [Milestone 3: Catalog Service](docs/milestone-3-catalog-service.md) - Build book browsing and search functionality
5. [Milestone 4: Reservation Service](docs/milestone-4-reservation-service-core-functionality.md) - Implement
   reservation lifecycle management

### Quality & Deployment

6. [Milestone 5: Deployment & Production Readiness](docs/milestone-6-deployment-production-readiness.md) - Deploy to AWS
   Elastic Beanstalk with RDS

### Reference Documentation

- [User Stories](docs/user-stories.md) - 11 user stories covering all features
- [API Contracts](docs/api-contracts.md) - Complete API documentation for all 10 endpoints

---

## API Endpoints (10 Total)

### Authentication & User Management (3 endpoints)

- `POST /api/auth/register` - Create new user account
- `POST /api/auth/login` - Authenticate and receive JWT token
- `GET /api/users/profile` - View user profile with statistics

### Catalog Management (2 endpoints)

- `GET /api/catalog/books` - Browse and search books with pagination
- `GET /api/catalog/books/{bookId}` - View detailed book information

### Reservation Management (5 endpoints)

- `POST /api/reservations` - Reserve an available book
- `GET /api/reservations` - View active reservations
- `POST /api/reservations/{reservationId}/checkout` - Checkout book (Librarian only)
- `POST /api/reservations/{reservationId}/return` - Return book with late fee calculation (Librarian only)
- `GET /api/reservations/history` - View complete borrowing history

---

## Technical Stack Summary

### Core Technologies

- **.NET**: 8.0 (LTS)
- **ASP.NET Core**: 8.x
- **ASP.NET Core Identity**: 8.x (User management and authentication)
- **Entity Framework Core**: 8.x
- **Database**: In-Memory (Development), PostgreSQL 15+ (Production)

### Additional Libraries

- **JWT**: Microsoft.AspNetCore.Authentication.JwtBearer
- **Validation**: FluentValidation.AspNetCore
- **OpenAPI**: Swashbuckle.AspNetCore (Swagger)
- **Testing**: xUnit, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing

### AWS Deployment

- **AWS Elastic Beanstalk**: .NET application hosting
- **AWS RDS**: PostgreSQL database
- **Environment Variables**: Configuration via Elastic Beanstalk environment properties
- **Amazon VPC**: Network security and isolation

---

## Success Criteria

> Capstones will be graded using the following success criteria on a Pass/Fail basis.<br>
> You will receive a score out of 20, along with instructor feedback.

### Functional Requirements

- ✅ All 11 user stories fully implemented
- ✅ 10 API endpoints documented and functional
- ✅ Complete reservation lifecycle working end-to-end (reserve → checkout → return)
- ✅ Role-based access control enforced (Patron vs Librarian)
- ✅ JWT authentication with 24-hour token expiration

### Technical Requirements

- ✅ OpenAPI documentation complete and accessible
- ✅ Deployed to AWS Elastic Beanstalk with RDS PostgreSQL integration

### Quality Standards

- ✅ Clean code principles followed
- ✅ SOLID principles applied
- ✅ Async/await pattern used throughout
- ✅ Dependency injection configured properly
- ✅ Comprehensive error handling (400, 401, 403, 404, 500)
- ✅ Proper logging throughout application
- ✅ Production-ready configuration for AWS deployment

### Business Rules Implemented

- ✅ Maximum 5 active reservations per user
- ✅ 7-day reservation expiry period
- ✅ 14-day checkout period
- ✅ $1.00 per day late fee calculation
- ✅ Real-time book availability tracking

For detailed implementation guides, refer to the milestone documents in the `docs/` directory.