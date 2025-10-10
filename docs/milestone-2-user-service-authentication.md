# Milestone 3: Catalog Service

**Goal:** Build Catalog Service for book inventory and search management

**Related User Stories:** US-004 (Browse Book Catalog), US-005 (Search and Filter Books), US-006 (View Book Details)

---

## Business Requirements

### Book Browsing (US-004)
- Library patrons must be able to browse the complete book catalog
- Results must be paginated for performance
  - Default: 20 books per page, starting at page 0
- Results must be sortable by:
  - Title
  - Author
  - Publication year
- Sort direction can be ascending or descending (default: ascending)
- Each book displays: bookId, isbn, title, author, genre, publicationYear, description, totalCopies, availableCopies, status
- Book status is determined by availability:
  - Available: availableCopies > 0
  - CheckedOut: availableCopies = 0
- No authentication required (public access)

### Search and Filtering (US-005)
- Patrons must be able to search books by title and/or author
- Patrons can filter by:
  - Genre (exact match)
  - ISBN (exact match)
  - Availability (show only books with copies available)
- Multiple filters can be combined simultaneously
- Search results maintain pagination and sorting capabilities
- Empty searches return empty results (not an error)
- No authentication required (public access)

### Book Details (US-006)
- Patrons must be able to view complete information about a specific book
- Details include all metadata: isbn, title, author, genre, publicationYear, description, publisher, pageCount, language
- Details show availability: totalCopies, availableCopies, status
- Details include audit information: createdAt, updatedAt
- Returns 404 error if book not found
- No authentication required (public access)

### Inventory Management
- Book status is calculated dynamically (not stored)
- availableCopies updates when:
  - Reservation Service decrements (book reserved)
  - Reservation Service increments (book returned)
- totalCopies represents physical inventory
- Catalog Service provides internal endpoint for Reservation Service to update availability

---

## General Technical Requirements

**Technology Stack:**
- ASP.NET Core 8.0 or 9.0
- Entity Framework Core 8.0+
- PostgreSQL 15+ (or in-memory for development)

**Performance Requirements:**
- Search and filter operations must complete within reasonable time for catalogs up to 1000+ books
- Efficient querying and indexing strategy required

**Data Requirements:**
- Support pagination for large result sets
- Return pagination metadata (page number, size, total elements, total pages, last page flag)
- Support dynamic filtering and sorting combinations

**Service Communication:**
- Catalog Service is called by Reservation Service to check book availability
- Catalog Service provides internal endpoint to update available copies count
- All inter-service calls use HTTP/REST

**Public Access:**
- All public catalog endpoints are accessible without authentication
- Internal availability update endpoint should be secured or restricted to internal service calls

---

## Deliverables

### 1. Paginated Book Listing
Implement endpoint that:
- Returns books in paginated format
- Accepts pagination parameters (page number, page size)
- Accepts sort parameters (field, direction)
- Applies default pagination and sorting when not specified
- Returns metadata about pagination state

### 2. Search and Filter Functionality
Implement search/filter capabilities that:
- Searches across title and author fields
- Filters by genre
- Filters by ISBN
- Filters by availability (books with available copies)
- Supports combining multiple filters
- Maintains pagination for filtered results
- Performs efficiently on large datasets

### 3. Book Details Retrieval
Implement endpoint that:
- Retrieves complete book information by unique identifier
- Returns all book metadata fields
- Calculates and includes current availability status
- Returns appropriate error for non-existent books

### 4. Status Calculation
Implement logic that:
- Dynamically determines book status based on availableCopies
- Does not store status as a database field
- Consistently calculates status across all endpoints

### 5. Availability Management (Internal)
Implement internal endpoint that:
- Allows Reservation Service to update availableCopies
- Supports increment (book returned) and decrement (book reserved) operations
- Validates book exists before updating
- Returns updated book availability information

---

## API Endpoints to Implement

Based on `api-contracts.md` and microservices architecture, implement these endpoints:

### GET /api/catalog/books
- **Access:** Public (no authentication)
- **Query Parameters:**
  - page (integer, default: 0)
  - size (integer, default: 20)
  - sortBy (string, default: "title") - options: title, author, publicationYear
  - sortOrder (string, default: "asc") - options: asc, desc
  - query (string, optional) - search term for title/author
  - genre (string, optional) - filter by genre
  - isbn (string, optional) - filter by ISBN
  - availableOnly (boolean, default: false)
- **Success (200):** Paginated list with content array and metadata
- **Response includes:** page, size, totalElements, totalPages, last

### GET /api/catalog/books/{bookId}
- **Access:** Public (no authentication)
- **Path Parameter:** bookId (Guid)
- **Success (200):** Complete book details including all metadata
- **Error (404):** Book not found

### PUT /api/catalog/books/{bookId}/availability (Internal)
- **Access:** Internal use by Reservation Service
- **Path Parameter:** bookId (Guid)
- **Request:** operation (increment or decrement), amount (default: 1)
- **Success (200):** bookId, availableCopies, totalCopies, status
- **Error (404):** Book not found
- **Error (400):** Invalid operation or insufficient copies
- **Note:** This endpoint is used by Reservation Service to update book availability

---

## Acceptance Criteria

- [ ] GET /api/catalog/books returns paginated results with defaults (page=0, size=20, sortBy=title, sortOrder=asc)
- [ ] Pagination metadata included: content, page, size, totalElements, totalPages, last
- [ ] Query parameter performs search on title and author fields
- [ ] Genre filter returns only books matching exact genre
- [ ] ISBN filter returns only book matching exact ISBN
- [ ] availableOnly filter returns only books with availableCopies > 0
- [ ] Multiple filters work together (query + genre + availableOnly)
- [ ] Sorting works for title, author, and publicationYear
- [ ] Sort order (asc/desc) works correctly
- [ ] GET /api/catalog/books/{bookId} returns complete book details
- [ ] Invalid bookId returns 404 with appropriate error response
- [ ] Book status correctly calculated (Available vs CheckedOut)
- [ ] Empty search results return empty content array (not error)
- [ ] Search performs efficiently (< 1 second for 1000+ books)
- [ ] Internal availability endpoint updates availableCopies correctly
- [ ] Availability endpoint prevents negative availableCopies
- [ ] Catalog Service runs on port 5002

---

## Suggested Approach

1. Create Catalog Service ASP.NET Core project
2. Configure service to run on port 5002
3. Create Book entity and CatalogServiceContext
4. Design efficient database queries for filtering and sorting
5. Implement pagination mechanism
6. Create book listing endpoint with pagination
7. Add search functionality across title and author
8. Implement individual filter capabilities
9. Combine filters to work together
10. Add sorting functionality
11. Implement book details endpoint
12. Add status calculation logic
13. Implement internal availability update endpoint
14. Optimize query performance with appropriate indexing
15. Set up Swagger documentation
16. Test all catalog operations
17. Test availability updates from Reservation Service

**Note:** You have flexibility in how you implement search and filtering logic, structure your queries, and optimize performance. Consider using appropriate Entity Framework Core features and query techniques to meet performance requirements.

---

## Inter-Service Communication Example

**Availability Update Flow (used by Reservation Service):**
1. User reserves a book in Reservation Service
2. Reservation Service validates book availability
3. Reservation Service calls PUT http://localhost:5002/api/catalog/books/{bookId}/availability
  - Request body: `{ "operation": "decrement", "amount": 1 }`
4. Catalog Service decrements availableCopies by 1
5. Catalog Service returns updated availability
6. Reservation Service completes reservation creation

**Book Return Flow:**
1. Librarian returns a book in Reservation Service
2. Reservation Service calls PUT http://localhost:5002/api/catalog/books/{bookId}/availability
  - Request body: `{ "operation": "increment", "amount": 1 }`
3. Catalog Service increments availableCopies by 1
4. Catalog Service returns updated availability
5. Reservation Service completes return processing

---

## Resources

- Refer to `user-stories.md` for US-004, US-005, US-006 details
- Refer to `api-contracts.md` for exact request/response formats
- Refer to `milestone-1` for microservices architecture and service boundaries
- Entity Framework Core documentation for pagination and query techniques