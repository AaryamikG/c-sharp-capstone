### Milestone 3: Catalog Service
**Goal:** Build book catalog browsing and search system

#### Deliverables:

1. **Book Browsing**
   - Paginated book listing endpoint
   - Default pagination: page=0, size=20
   - Sort options: title, author, publicationYear
   - Sort order: asc (ascending) or desc (descending)
   - Default sort: title ascending

2. **Search & Filtering**
   - Full-text search on title and author fields using `query` parameter
   - Filter by genre (exact match)
   - Filter by ISBN (exact match)
   - Filter by availableOnly (boolean, shows only books with availableCopies > 0)
   - Combined filtering (all parameters can be used together)
   - Pagination support for search results

3. **Book Details**
   - Retrieve complete book information by bookId
   - Display all metadata: isbn, title, author, genre, publicationYear, description, publisher, pageCount, language
   - Show availability: totalCopies, availableCopies, status
   - Include audit timestamps: createdAt, updatedAt
   - Return 404 if book not found

4. **Inventory Status Management**
   - Status calculation: Available (availableCopies > 0), CheckedOut (availableCopies = 0)
   - totalCopies tracking
   - availableCopies automatic updates on reservation/checkout/return
   - Status property is read-only (calculated from availableCopies)

#### Acceptance Criteria:
- [ ] GET /api/catalog/books returns paginated results with default settings
- [ ] Pagination includes: items array, page, pageSize, totalCount, totalPages
- [ ] Search with query parameter performs search on title and author
- [ ] Multiple filters can be combined (query + genre + availableOnly)
- [ ] Sorting works correctly for title, author, and publicationYear
- [ ] GET /api/catalog/books/{bookId} returns complete book details
- [ ] Book not found returns 404 with proper error response
- [ ] Status property correctly reflects availability (Available vs CheckedOut)
- [ ] All catalog endpoints have 80%+ test coverage

#### API Endpoints Completed:

**1. GET /api/catalog/books**
- Public endpoint (no authentication required)
- Query parameters: page, pageSize, sortBy, sortOrder, query, genre, isbn, availableOnly
- Response 200: Paginated book list with metadata
- Supports combined search and filtering
- Default: page=0, pageSize=20, sortBy=title, sortOrder=asc

**2. GET /api/catalog/books/{bookId}**
- Public endpoint (no authentication required)
- Path parameter: bookId (Guid)
- Response 200: Complete book details with all metadata fields
- Error 404: Book not found

#### Testing Requirements:
- Unit tests for BookService (search logic, filtering, pagination)
- Integration tests for GET /api/catalog/books with various parameter combinations
- Integration tests for GET /api/catalog/books/{bookId}
- Validation tests for query parameters (page, pageSize, sortBy, sortOrder)
- Edge case tests (empty results, invalid bookId, pagination boundaries)
- Status calculation tests (Available vs CheckedOut based on availableCopies)

#### Technical Specifications:
- Use LINQ queries for filtering and sorting
- Implement search using `Contains()` for title and author fields
- Return `PagedResult<BookDto>` with pagination metadata
- Create DTOs for API responses (separate from entity models)
- Use `async/await` pattern for all database operations
- Implement proper null checking and validation